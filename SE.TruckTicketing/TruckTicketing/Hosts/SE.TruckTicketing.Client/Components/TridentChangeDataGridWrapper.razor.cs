using System;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentChangeDataGridWrapper : BaseTruckTicketingComponent
{
    [Parameter]
    public Guid Id { get; set; }

    [Parameter]
    public string EntityType { get; set; }

    [Parameter]
    public string EntityName { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoading { get; set; }

    [Inject]
    public IBackendService BackendService { get; set; }
}
