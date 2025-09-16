using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public class TareWeightWatcher : ComponentBase, ITruckTicketWorkflow
{
    private string[] _dependencies = Array.Empty<string>();

    [Inject]
    private IServiceProxyBase<TruckTicketTareWeight, Guid> TareWeightService { get; set; }

    [Inject]
    public DialogService DialogService { get; set; }

    public ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = GetDependencies(viewModel);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var checkWeight = viewModel.TruckTicketBackup?.TareWeight ?? 0;
        var facility = viewModel.Facility;
        var dependencies = GetDependencies(viewModel);
        var shouldRun = truckTicket.FacilityId != Guid.Empty &&
                        truckTicket.TruckingCompanyId != Guid.Empty &&
                        truckTicket.TruckNumber.HasText() &&
                        checkWeight == 0 &&
                        facility?.Type == FacilityType.Lf &&
                        viewModel.ActivateAutofillTareWeight &&
                        !_dependencies.SequenceEqual(dependencies);

        _dependencies = dependencies;
        return ValueTask.FromResult(shouldRun);
    }

    public async ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var criteria = new SearchCriteriaModel
        {
            PageSize = 1,
            SortOrder = SortOrder.Desc,
            OrderBy = nameof(TruckTicketTareWeight.LoadDate),
            Filters = new()
            {
                { nameof(TruckTicketTareWeight.FacilityId), truckTicket.FacilityId },
                { nameof(TruckTicketTareWeight.TruckingCompanyName), truckTicket.TruckingCompanyName },
                { nameof(TruckTicketTareWeight.TruckNumber), truckTicket.TruckNumber.ToUpper() },
                { nameof(TruckTicketTareWeight.TrailerNumber), truckTicket.TrailerNumber?.ToUpper() ?? string.Empty },
                { nameof(TruckTicketTareWeight.IsActivated), true },
            },
        };

        var search = await TareWeightService.Search(criteria);
        var index = search?.Results?.FirstOrDefault();
        
        if (index is not null)
        {
            var validityDays = viewModel.Facility.TareWeightValidityDays.HasValue ? viewModel.Facility.TareWeightValidityDays : 90;
            if (validityDays <= (DateTimeOffset.Now - index.LoadDate).Days)
            {
                var message = "The tare record for " +
                              "this truck or truck/trailer combo has expired.  Please re-scale truck for new tare weight.";

                await DialogService.OpenAsync<TruckTicketAlertComponent>("Tare Weight Validity",
                                                                         new()
                                                                         {
                                                                             { nameof(TruckTicketAlertComponent.Message), message },
                                                                             { nameof(TruckTicketAlertComponent.ButtonText), "Close" },
                                                                         });
            }
            else
            {
                viewModel.TruckTicket.TareWeight = index.TareWeight;
            }
        }
        else
        {
            viewModel.TruckTicket.TareWeight = 0;
            viewModel.TruckTicket.NetWeight = viewModel.TruckTicket.GrossWeight > 0 ? viewModel.TruckTicket.GrossWeight : 0;
        }

        return true;
    }

    private string[] GetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        return new[] { truckTicket.FacilityId.ToString(), truckTicket.TruckingCompanyId.ToString(), truckTicket.TruckNumber, truckTicket.TrailerNumber };
    }
}
