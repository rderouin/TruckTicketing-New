using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class AdditionalServicesGrid : BaseRazorComponent
{
    private SearchResultsModel<TruckTicketAdditionalService, SearchCriteriaModel> _additionalServices = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<TruckTicketAdditionalService>(),
    };

    private EditContext _editContext;

    [Parameter]
    public TruckTicket model { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAdditionalService> NewAdditionalServiceAdded { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAdditionalService> OnAdditionalServiceChange { get; set; }

    //Events
    private EventCallback HandleCancel => new(this, () => DialogService.Close());

    private EventCallback<TruckTicketAdditionalService> AddAdditionalServiceHandler =>
        new(this, (Func<TruckTicketAdditionalService, Task>)(async model =>
                                                             {
                                                                 DialogService.Close();
                                                                 await NewAdditionalServiceAdded.InvokeAsync(model);
                                                             }));

    private EventCallback<TruckTicketAdditionalService> UpdateAdditionalServiceHandler =>
        new(this, (Func<TruckTicketAdditionalService, Task>)(async model =>
                                                             {
                                                                 DialogService.Close();
                                                                 await OnAdditionalServiceChange.InvokeAsync(model);
                                                             }));

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _editContext = new(model);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        await LoadAdditionalServices();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadAdditionalServices();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }

    private Task LoadAdditionalServices()
    {
        _additionalServices = new(model.AdditionalServices);
        return Task.CompletedTask;
    }

    private async Task AddAdditionalService()
    {
        await OpenEditDialog(new(), true);
    }

    private async Task OpenEditDialog(TruckTicketAdditionalService truckTicketAdditionalServiceModel, bool isNew)
    {
        await DialogService.OpenAsync<AdditionalServiceEdit>("Additional Service",
                                                             new()
                                                             {
                                                                 { "TruckTicketAdditionalService", truckTicketAdditionalServiceModel },
                                                                 { "IsNewRecord", isNew },
                                                                 { "AdditionalServices", model.AdditionalServices },
                                                                 { "Model", model },
                                                                 { nameof(AdditionalServiceEdit.AddAdditionalService), AddAdditionalServiceHandler },
                                                                 { nameof(AdditionalServiceEdit.UpdateAdditionalService), UpdateAdditionalServiceHandler },
                                                                 { nameof(AdditionalServiceEdit.OnCancel), HandleCancel },
                                                             });
    }
}
