using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trident.Api.Search;
using Trident.Search;

namespace Trident.UI.Blazor.Components.Grid.Filters
{
    public abstract class NumericFilter : FilterComponentBase<NumericFilterOptions, FilterEventArgs<NumericFilter>> { }

    public partial class NumericFilter<TOutputValue> : NumericFilter
    {
        public long? LowerBoundValue { get; set; }
        public long? UpperBoundValue { get; set; }

        private static Type[] SupportedTypeParameters { get; } = new[] {
                   typeof(long), typeof(double)
               };

        public NumericFilter()
        {
            if (!SupportedTypeParameters.Any(x => x == typeof(TOutputValue)))
                throw new ArgumentOutOfRangeException(
                    $"Only the following types are supported as the {string.Join(", ", SupportedTypeParameters.Select(x => x.FullName))}");
        }


        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        private async Task ValueChanged(Bounds bound, long? newValue)
        {
            long? origValue;
            if (bound == Bounds.Upper)
            {
                origValue = UpperBoundValue;
                UpperBoundValue = newValue;
            }
            else
            {
                LowerBoundValue = newValue;
                origValue = LowerBoundValue;
            }

            await RaiseOnChangeEvent(origValue, newValue, bound);

        }

        private async Task RaiseOnChangeEvent(object origValue, object newValue, Bounds bound)
        {
            if (OnChange.HasDelegate && await IsValid())
            {
                await OnChange.InvokeAsync(new NumericFilterArgs(this)
                {
                    OriginalValue = origValue,
                    NewValue = newValue,
                    Bound = bound

                }); ;
            }
        }

        public override Task<bool> IsValid()
        {
            var isValid = !(LowerBoundValue > UpperBoundValue);
            return Task.FromResult(isValid);
        }

        protected override async Task ResetToDefault()
        {
            var origValueLower = LowerBoundValue;
            var origValueUpper = UpperBoundValue;

            LowerBoundValue = FilterOptions.LowerBoundDefaultValue.HasValue ? FilterOptions.LowerBoundDefaultValue.GetValueOrDefault() : null;
            await RaiseOnChangeEvent(origValueLower, LowerBoundValue, Bounds.Lower);

            UpperBoundValue = FilterOptions.UpperBoundDefaultValue.HasValue ? FilterOptions.UpperBoundDefaultValue.GetValueOrDefault() : null;
            await RaiseOnChangeEvent(origValueUpper, UpperBoundValue, Bounds.Upper);
        }

        public override void ApplyFilter(SearchCriteriaModel searchCriteria)
        {
            var hasLowerBound = LowerBoundValue.HasValue;
            var hasUpperBound = UpperBoundValue.HasValue;
            int keyIdx = 1;
            string keyBase = FilterOptions.FilterPath;

            if (!hasLowerBound && !hasUpperBound)
            {
                if (searchCriteria.Filters.ContainsKey(FilterOptions.FilterPath))
                {
                    searchCriteria.Filters.Remove(FilterOptions.FilterPath);
                }

                return;
            }

            IJunction query = AxiomFilterBuilder
                .CreateFilter()
                .StartGroup();

            // lower only 
            if (hasLowerBound)
            {
                query = ((GroupStart)query).AddAxiom(new Axiom
                {
                    Field = FilterOptions.FilterPath,
                    Key = $"{keyBase}{keyIdx++}",
                    Operator = Search.CompareOperators.gte,
                    Value = LowerBoundValue.Value
                });
            }

            if (hasLowerBound && hasUpperBound)
            {
                query = ((AxiomTokenizer)query).And();
            }

            if (hasUpperBound)
            {
                if (query is ILogicalOperator and)
                {
                    query = and.AddAxiom(new Axiom
                    {
                        Field = FilterOptions.FilterPath,
                        Key = $"{keyBase}{keyIdx++}",
                        Operator = Search.CompareOperators.lt,
                        Value = UpperBoundValue.Value
                    });
                }
                else
                {
                    query = ((GroupStart)query).AddAxiom(new Axiom
                    {
                        Field = FilterOptions.FilterPath,
                        Key = $"{keyBase}{keyIdx++}",
                        Operator = Search.CompareOperators.gte,
                        Value = UpperBoundValue.Value
                    });
                }
            }

            var filter = ((AxiomTokenizer)query)
                .EndGroup()
                .Build();

            if (filter != null)
            {
                searchCriteria.Filters ??= new Dictionary<string, object>();
                searchCriteria.Filters[FilterOptions.FilterPath] = filter;
            }
        }


    }
}
