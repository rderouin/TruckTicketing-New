namespace SE.TruckTicketing.Client.Components.GridFilters;

public class FilterComponentChangeArgs<TValue>
{
    public TValue Value { get; set; }

    public FilterComponent Ref { get; set; }
}
