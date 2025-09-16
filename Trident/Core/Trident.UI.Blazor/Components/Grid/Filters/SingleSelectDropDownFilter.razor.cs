using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trident;
using Trident.Api.Search;
using Trident.Search;

namespace Trident.UI.Blazor.Components.Grid.Filters
{
    public abstract class SingleSelectDropDownFilter : ListFilterComponentBase<SingleSelectDropDownFilterOptions, FilterEventArgs<SingleSelectDropDownFilter>> { }

    public partial class SingleSelectDropDownFilter<TOutputValue> : SingleSelectDropDownFilter
    {

        /// <summary>
        ///  // get init config data here for list loading
        /// </summary>
        /// <returns></returns>
        protected override Task OnParametersSetAsync()
        {

            return base.OnParametersSetAsync();
        }

        /// <summary>
        /// Validates that the filter value is of the matching column type
        /// </summary>
        /// <returns></returns>
        public override Task<bool> IsValid()
        {
            try
            {
                if (Value == null) return Task.FromResult(true);
                var specificedTypeValue = Value.ChangeType(DataType);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private string bindValue = string.Empty;

        /// <summary>
        /// Apply filter data to the criteria
        /// </summary>
        /// <param name="searchCriteria"></param>
        public override void ApplyFilter(SearchCriteriaModel searchCriteria)
        {
            base.ApplyFilter(searchCriteria);
        }

        /// <summary>
        /// Implment change event for your control types to collect the values as they
        /// are changing..only if valid do we raise the event to the parent grid panel
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns></returns>

        public async Task OnSelectionChanged(object newValue)
        {
            var oldValue = Value;
            Value = newValue;
            await RaisOnChangeEvent(oldValue, newValue);
        }
        protected override async Task ResetToDefault()
        {
            var oldValue = Value;
            Value = null;
            bindValue = string.Empty;
            await RaisOnChangeEvent(oldValue, Value);
        }


        private async Task RaisOnChangeEvent(object oldValue, object newValue)
        {
            if (OnChange.HasDelegate && await IsValid())
            {
                await OnChange.InvokeAsync(new FilterEventArgs<SingleSelectDropDownFilter>(this)
                {
                    OriginalValue = oldValue,
                    NewValue = Value
                });
            }
        }

    }
}
