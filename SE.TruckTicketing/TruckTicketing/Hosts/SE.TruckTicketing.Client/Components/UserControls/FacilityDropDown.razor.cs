using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.UserControls;

public partial class FacilityDropDown<TValue> : TridentApiDropDownDataGrid<Facility, TValue>
{
    protected override Task BeforeDataLoad(SearchCriteriaModel criteria)
    {
        criteria.Filters[nameof(Facility.IsActive)] = true;
        return base.BeforeDataLoad(criteria);
    }
}
