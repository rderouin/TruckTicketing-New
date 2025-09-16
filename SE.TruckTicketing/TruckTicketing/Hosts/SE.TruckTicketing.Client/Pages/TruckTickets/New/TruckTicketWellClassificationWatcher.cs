using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Contracts.Enums;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public class WellClassificationWatcher : ComponentBase, ITruckTicketWorkflow
{
    private string[] _dependencies = Array.Empty<string>();

    [Inject]
    private IServiceProxyBase<TruckTicketWellClassification, Guid> WellClassificationService { get; set; }

    public ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        _dependencies = GetDependencies(viewModel);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        var dependencies = GetDependencies(viewModel);
        var shouldRun = truckTicket.FacilityId != Guid.Empty &&
                        truckTicket.SourceLocationId != Guid.Empty &&
                        truckTicket.WellClassification == WellClassifications.Undefined &&
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
            OrderBy = nameof(TruckTicketWellClassification.Date),
            Filters = new()
            {
                { nameof(TruckTicketWellClassification.FacilityId), truckTicket.FacilityId },
                { nameof(TruckTicketWellClassification.SourceLocationId), truckTicket.SourceLocationId },
            },
        };

        var search = await WellClassificationService.Search(criteria);
        var index = search?.Results?.FirstOrDefault();

        if (index is null)
        {
            return false;
        }

        viewModel.TruckTicket.WellClassification = index.WellClassification;
        return true;
    }

    private string[] GetDependencies(TruckTicketExperienceViewModel viewModel)
    {
        var truckTicket = viewModel.TruckTicket;
        return new[] { truckTicket.FacilityId.ToString(), truckTicket.SourceLocationId.ToString() };
    }
}
