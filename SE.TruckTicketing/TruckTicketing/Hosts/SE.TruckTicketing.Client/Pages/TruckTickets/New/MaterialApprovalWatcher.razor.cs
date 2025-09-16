using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components.UserControls;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public partial class MaterialApprovalWatcher : BaseRazorComponent, ITruckTicketWorkflow
{
    private string[] _dependencies = Array.Empty<string>();

    private MaterialApprovalOperatorNotesAcknowledgmentDialog _materialApprovalAcknowledgment;

    [Inject]
    private IServiceProxyBase<Contracts.Models.Operations.MaterialApproval, Guid> MaterialApprovalService { get; set; }

    private TruckTicketExperienceViewModel ViewModel { get; set; }

    public async ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = Array.Empty<string>();
        if (await ShouldRun(viewModel))
        {
            viewModel.MaterialApprovals = await LoadMaterialApprovals(viewModel);
        }

        ViewModel = viewModel;
        StateHasChanged();
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var dependencies = GetDependencies(viewModel);
        var shouldRun = truckTicket.FacilityId != Guid.Empty &&
                        truckTicket.SourceLocationId != Guid.Empty &&
                        viewModel.ShowMaterialApproval && (!_dependencies.SequenceEqual(dependencies) || viewModel.IsRefreshMaterialApproval);

        _dependencies = dependencies;
        return ValueTask.FromResult(shouldRun);
    }

    public async ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel)
    {
        ViewModel.IsRefreshMaterialApproval = false;
        var materialApprovals = await LoadMaterialApprovals(viewModel);
        viewModel.MaterialApprovals = materialApprovals;

        if (materialApprovals.Count == 1)
        {
            var materialApproval = materialApprovals.First();
            var triggerStateChange = viewModel.TruckTicket.MaterialApprovalId != materialApproval.Id;
            await viewModel.SetMaterialApproval(await HandleAnalyticalExpiration(viewModel, materialApproval));
            await _materialApprovalAcknowledgment.OpenDialog(materialApproval);
            ViewModel.HasMaterialApprovalErrors = (materialApproval != null && materialApproval.HazardousNonhazardous == Contracts.Lookups.HazardousClassification.Undefined) ? true : false;
            return triggerStateChange;
        }
        else
        {
            var materialApproval = materialApprovals.FirstOrDefault(ma => ma.Id == viewModel.TruckTicket.MaterialApprovalId);
            var triggerStateChange = viewModel.TruckTicket.MaterialApprovalId.GetValueOrDefault() != (materialApproval?.Id ?? Guid.Empty);
            await viewModel.SetMaterialApproval(await HandleAnalyticalExpiration(viewModel, materialApproval));
            await _materialApprovalAcknowledgment.OpenDialog(materialApproval);
            return triggerStateChange;
        }
    }

    private async Task<Contracts.Models.Operations.MaterialApproval> HandleAnalyticalExpiration(TruckTicketExperienceViewModel viewModel, Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        if (materialApproval?.AnalyticalExpiryDate != null && materialApproval.AnalyticalExpiryAlertActive)
        {
            var daysToExpiration = materialApproval.AnalyticalExpiryDate.Value.Subtract(DateTimeOffset.UtcNow).Days;

            if (daysToExpiration < 31)
            {
                await ShowMessage("Analytical Expiry Alert",
                                  "<p>The analytical for this Material Approval is nearing expiration or has expired. The analytical needs to be renewed, " +
                                  "as the Material Approval will not be available for use once the analytical expires. If the job is no longer active, " +
                                  "notifications will need to be turned off on the Material Approval.</p>" +
                                  "<br>" +
                                  $"<p>Material Approval ID: {materialApproval.MaterialApprovalNumber}</p>" +
                                  $"<p>Analytical Expiry Date: {materialApproval.AnalyticalExpiryDate.Value:MM/dd/yyyy}</p>" +
                                  $"<p>Source Location: {materialApproval.SourceLocationFormattedIdentifier}</p>" +
                                  $"<p>Generator Name: {materialApproval.GeneratorName}</p>");
            }

            return daysToExpiration < 0 ? null : materialApproval;
        }

        return materialApproval;
    }

    private bool PassedMaterialApprovalAnalyticalExpiryDate(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        var daysToExpiration = materialApproval?.AnalyticalExpiryDate?.Subtract(DateTimeOffset.UtcNow).Days;
        return daysToExpiration < 0;
    }

    private async ValueTask<ICollection<Contracts.Models.Operations.MaterialApproval>> LoadMaterialApprovals(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var criteria = new SearchCriteriaModel
        {
            Filters =
            {
                [nameof(Contracts.Models.Operations.MaterialApproval.AnalyticalFailed)] = false,
                [nameof(Contracts.Models.Operations.MaterialApproval.FacilityId)] = truckTicket.FacilityId,
                [nameof(Contracts.Models.Operations.MaterialApproval.SourceLocationId)] = truckTicket.SourceLocationId,
                [nameof(Contracts.Models.Operations.MaterialApproval.IsActive)] = true,
            },
        };

        var search = await MaterialApprovalService.Search(criteria);
        return search?.Results?.ToArray() ?? Array.Empty<Contracts.Models.Operations.MaterialApproval>();
    }

    private string[] GetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        return new[] { truckTicket.FacilityId.ToString(), truckTicket.SourceLocationId.ToString() };
    }

    protected async Task HandleMaterialApprovalAccept(Contracts.Models.Operations.MaterialApproval materialApproval)
    {
        await ViewModel.SetMaterialApproval(materialApproval);
        ViewModel.HasMaterialApprovalErrors = (materialApproval != null && materialApproval.HazardousNonhazardous == Contracts.Lookups.HazardousClassification.Undefined) ? true : false;
        StateHasChanged();
    }

    protected async Task HandleMaterialApprovalAcknowledgementDecline()
    {
        await ViewModel.SetMaterialApproval(null);
        StateHasChanged();
    }
}
