using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public interface IBillingConfigurationWatcher : ITruckTicketWorkflow
{
    void ClearDependencies();

    Task LoadBillingConfigurations(TruckTicketExperienceViewModel viewModel, bool isReevaluate = true);

    Task UpdateBillingConfigurations(TruckTicketExperienceViewModel viewModel);

    void SetDependencies(TruckTicketExperienceViewModel viewModel);
}

public class BillingConfigurationWatcher : ComponentBase, IBillingConfigurationWatcher
{
    private string[] _dependencies = Array.Empty<string>();

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    [Inject]
    private IServiceProxyBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    public async ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = Array.Empty<string>();
        var shouldReevaluateBillingConfiguration = !viewModel.TruckTicket.BillingConfigurationId.HasValue || viewModel.TruckTicket.BillingConfigurationId.Value == Guid.Empty;
        if (await ShouldRun(viewModel))
        {
            await LoadBillingConfigurations(viewModel, shouldReevaluateBillingConfiguration);
        }
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var dependencies = GetDependencies(viewModel);
        var shouldRun = truckTicket.FacilityId != Guid.Empty &&
                        truckTicket.SourceLocationId != Guid.Empty &&
                        truckTicket.WellClassification != WellClassifications.Undefined &&
                        truckTicket.FacilityServiceId.GetValueOrDefault() != Guid.Empty &&
                        truckTicket.LoadDate.HasValue &&
                        !_dependencies.SequenceEqual(dependencies);

        _dependencies = dependencies;
        return ValueTask.FromResult(shouldRun);
    }

    public async ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel)
    {
        viewModel.IsLoadingBillingConfigurations = true;
        viewModel.TriggerStateChanged();
        //When called from Run, system would always reevaluate BillingConfiguration
        await LoadBillingConfigurations(viewModel);

        viewModel.IsLoadingBillingConfigurations = false;
        viewModel.TriggerStateChanged();
        //viewModel.BillingConfigurations = billingConfigurations;

        //await UpdateBillingConfigurations(viewModel);

        return true;
    }

    public async Task LoadBillingConfigurations(TruckTicketExperienceViewModel viewModel, bool reevaluateBillingConfiguration = true)
    {
        var loadedBillingConfigurations = new List<BillingConfiguration>();
        if (viewModel.TruckTicket.Status is TruckTicketStatus.New or TruckTicketStatus.Open or TruckTicketStatus.Stub or TruckTicketStatus.Hold)
        {
            var billingConfigs = await TruckTicketService.GetMatchingBillingConfiguration(viewModel.TruckTicket);
            loadedBillingConfigurations.AddRange(billingConfigs);
        }

        //If reevaluate & refresh -> update billing config
        //If reevaluate & not refresh -> update billing config
        //If not reevaluate & refresh -> don't update billing config with new selection; but still update data references for existing billing config
        //If no reevaluate & not refresh -> do nothing (load existing )
        if (reevaluateBillingConfiguration)
        {
            viewModel.BillingConfigurations = loadedBillingConfigurations.OrderByDescending(config => config.IncludeForAutomation).ThenBy(config => config.Name).ToList();
            await UpdateBillingConfigurations(viewModel);
        }
        else
        {
            var associatedBillingConfiguration = await BillingConfigurationService.GetById(viewModel.TruckTicket.BillingConfigurationId.GetValueOrDefault(), !viewModel.IsRefresh);
            if (loadedBillingConfigurations.All(x => x.Id != associatedBillingConfiguration.Id))
            {
                loadedBillingConfigurations.Add(associatedBillingConfiguration);
            }

            if (viewModel.IsRefresh)
            {
                await viewModel.SetBillingConfiguration(associatedBillingConfiguration);
            }

            viewModel.BillingConfigurations = loadedBillingConfigurations.OrderByDescending(config => config.IncludeForAutomation).ThenBy(config => config.Name).ToList();
        }
    }

    public async Task UpdateBillingConfigurations(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var billingConfigurations = viewModel.BillingConfigurations;
        if (billingConfigurations.Count == 0)
        {
            await viewModel.SetBillingConfiguration();
            return;
        }

        if (billingConfigurations.Count == 1)
        {
            var billingConfig = billingConfigurations.Single();
            if (billingConfig.Id != truckTicket.BillingConfigurationId)
            {
                await viewModel.SetBillingConfiguration(billingConfig);
            }

            return;
        }

        var autoSelectedConfig = billingConfigurations.FirstOrDefault(config => config.IncludeForAutomation);
        if (autoSelectedConfig is not null && autoSelectedConfig.Id != truckTicket.BillingConfigurationId)
        {
            await viewModel.SetBillingConfiguration(autoSelectedConfig);
        }
    }

    public void ClearDependencies()
    {
        _dependencies = Array.Empty<string>();
    }

    public void SetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = GetDependencies(viewModel);
    }

    private string[] GetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        return new[]
        {
            truckTicket.FacilityId.ToString(),
            truckTicket.LoadDate?.ToString(),
            truckTicket.WellClassification.ToString(),
            truckTicket.SourceLocationId.ToString(),
            truckTicket.FacilityServiceId?.ToString(),
            truckTicket.GeneratorId.ToString(),
        };
    }
}
