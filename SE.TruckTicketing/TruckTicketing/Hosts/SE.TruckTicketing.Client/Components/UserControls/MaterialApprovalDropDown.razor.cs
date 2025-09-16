using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class MaterialApprovalDropDown<TValue> : TridentApiDropDown<MaterialApproval, TValue>
{
    [Parameter]
    public Guid FacilityId { get; set; }

    [Parameter]
    public Guid SourceLocationId { get; set; }

    [Parameter]
    public EventCallback<MaterialApproval> OnExpiredMaterialApprovalLoaded { get; set; }

    [Parameter]
    public EventCallback<MaterialApproval> OnExpiredMaterialApprovalSelected { get; set; }

    protected override async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        await base.BeforeDataLoad(criteria);
        
        criteria.Filters[nameof(MaterialApproval.AnalyticalFailed)] = false;
        criteria.Filters[nameof(MaterialApproval.FacilityId)] = FacilityId;
        criteria.Filters[nameof(MaterialApproval.SourceLocationId)] = SourceLocationId;
    }

    protected override async Task InvokeOnItemSelect(MaterialApproval model)
    {
        await base.InvokeOnItemSelect(model);
        await HandleAnalyticalExpirationMessage(model, OnExpiredMaterialApprovalSelected);
    }

    protected override async Task InvokeOnItemLoad(MaterialApproval model)
    {
        await base.InvokeOnItemLoad(model);
        await HandleAnalyticalExpirationMessage(model, OnExpiredMaterialApprovalLoaded);
    }

    private async Task HandleAnalyticalExpirationMessage(MaterialApproval materialApproval, EventCallback<MaterialApproval>? expiredCallback)
    {
        if (materialApproval.AnalyticalExpiryDate != null)
        {
            var daysToExpiration = materialApproval.AnalyticalExpiryDate.Value.Subtract(DateTimeOffset.UtcNow).Days;

            if (daysToExpiration < 31)
            {
                await ShowMessage("Analytical Expiry Alert",
                                  "The analytical for this Material Approval is nearing expiration or has expired. The analytical needs to be renewed, as the Material Approval will not be available for use once the analytical expires. If the job is no longer active, notifications will need to be turned off on the Material Approval.");
            }

            if (daysToExpiration < 0)
            {
                Value = default;

                if (expiredCallback?.HasDelegate ?? false)
                {
                    await expiredCallback?.InvokeAsync(materialApproval);
                }
            }
        }
    }
}
