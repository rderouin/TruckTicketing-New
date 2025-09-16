namespace Trident.UI.Blazor.Components.Grid.Filters;

internal class DoubleFilterArgs : FilterEventArgs<DoubleFilter>
{
    public DoubleFilterArgs(DoubleFilter source) : base(source)
    {
    }

    public Bounds Bound { get; set; }
}