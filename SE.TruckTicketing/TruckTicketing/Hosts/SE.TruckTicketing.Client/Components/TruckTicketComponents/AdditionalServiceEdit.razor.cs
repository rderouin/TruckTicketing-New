using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Extensions;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class AdditionalServiceEdit : BaseRazorComponent
{
    [Parameter]
    public TruckTicketAdditionalService TruckTicketAdditionalService { get; set; }

    private TruckTicketAdditionalService TruckTicketAdditionalServiceModel { get; set; }

    [Parameter]
    public bool IsNewRecord { get; set; }

    [Parameter]
    public TruckTicket Model { get; set; }

    [Parameter]
    public List<TruckTicketAdditionalService> AdditionalServices { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAdditionalService> AddAdditionalService { get; set; }

    [Parameter]
    public EventCallback<TruckTicketAdditionalService> UpdateAdditionalService { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        TruckTicketAdditionalServiceModel = TruckTicketAdditionalService.Clone();
    }

    private async Task SaveButton_Clicked()
    {
        if (!IsNewRecord)
        {
            await UpdateAdditionalService.InvokeAsync(TruckTicketAdditionalServiceModel);
        }
        else
        {
            await AddAdditionalService.InvokeAsync(TruckTicketAdditionalServiceModel);
        }
    }

    private void OnProductTypeChange(Product product)
    {
        TruckTicketAdditionalServiceModel.ProductId = product.Id;
        TruckTicketAdditionalServiceModel.AdditionalServiceName = product.Name;
        TruckTicketAdditionalServiceModel.AdditionalServiceNumber = product.Number;
        TruckTicketAdditionalServiceModel.AdditionalServiceQuantity = default;
    }
}
