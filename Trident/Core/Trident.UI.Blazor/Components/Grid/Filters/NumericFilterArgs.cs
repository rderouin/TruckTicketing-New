using System;

namespace Trident.UI.Blazor.Components.Grid.Filters
{
    internal class NumericFilterArgs : FilterEventArgs<NumericFilter>
    {
        public NumericFilterArgs(NumericFilter source) : base(source) { }
        public Bounds Bound { get; set; }
    }
}
