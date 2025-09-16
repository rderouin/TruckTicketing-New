using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class SourceField
{
    [Parameter]
    public IList<SourceFieldDto> ItemsSource { get; set; }

    [Parameter]
    public IList<ValueFormatDto> FormatsSource { get; set; }

    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    [Parameter]
    public bool ShowFormatSelection { get; set; }

    private string SelectedEntity { get; set; }

    private string CurrentPath => ItemsSource?.FirstOrDefault(m => m.Id == Model?.SourceModelFieldId)?.JsonPath;

    private IEnumerable<string> DistinctEntityNames
    {
        get
        {
            if (ItemsSource == null)
            {
                return Enumerable.Empty<string>();
            }

            return ItemsSource.Where(f => !string.IsNullOrWhiteSpace(f?.EntityName))
                              .Select(f => f.EntityName)
                              .Distinct()
                              .OrderBy(f => f);
        }
    }

    private IEnumerable<SourceFieldDto> FilteredByEntity
    {
        get
        {
            if (ItemsSource == null || string.IsNullOrWhiteSpace(SelectedEntity))
            {
                return Enumerable.Empty<SourceFieldDto>();
            }

            return ItemsSource.Where(f => f?.EntityName == SelectedEntity).OrderBy(f => f.FieldName);
        }
    }

    protected override Task OnParametersSetAsync()
    {
        // ensure fields in sync
        SelectedEntity = ItemsSource?.FirstOrDefault(f => f?.Id == Model.SourceModelFieldId)?.EntityName;
        if (SelectedEntity == null)
        {
            Model.SourceModelFieldId = null;
        }

        return base.OnParametersSetAsync();
    }

    private void SelectedEntityChanged(object obj)
    {
        Model.SourceModelFieldId = null;
    }
}
