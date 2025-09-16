using System.Linq;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Pages.SourceLocations;

public partial class OwnershipHistoryIndex : BaseRazorComponent
{
    [Parameter]
    public SourceLocation SourceLocation { get; set; }

    private SearchResultsModel<SourceLocationOwnerHistory, SearchCriteriaModel> Owners
    {
        get
        {
            var owners = SourceLocation?.OwnershipHistory ?? new();
            var model = new SearchResultsModel<SourceLocationOwnerHistory, SearchCriteriaModel>(owners.OrderByDescending(owner => owner.StartDate)) { Info = { TotalRecords = owners.Count } };
            return model;
        }
    }
}
