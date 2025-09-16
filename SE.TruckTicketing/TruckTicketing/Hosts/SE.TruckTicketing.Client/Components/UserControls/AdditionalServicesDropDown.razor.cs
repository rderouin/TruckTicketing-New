using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class AdditionalServicesDropDown<TValue> : TridentApiDropDownDataGrid<Product, TValue>
{
    [Parameter]
    public string FacilityCode { get; set; }

    [Parameter]
    public string ProductCategory { get; set; }

    protected override async Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        if (string.IsNullOrWhiteSpace(FacilityCode) && string.IsNullOrWhiteSpace(ProductCategory))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(FacilityCode))
        {
            criteria.Filters.TryAdd("FacilityCode", FacilityCode);
        }

        if (!string.IsNullOrWhiteSpace(ProductCategory))
        {
            criteria.Filters.TryAdd("ProductCategory", ProductCategory);
        }

        await base.BeforeDataLoad(criteria);
    }
}
