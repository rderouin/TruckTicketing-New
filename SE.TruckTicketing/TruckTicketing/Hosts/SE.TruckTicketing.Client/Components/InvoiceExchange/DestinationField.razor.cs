using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.BillingService.Contracts.Api.Models;

namespace SE.TruckTicketing.Client.Components.InvoiceExchange;

public partial class DestinationField
{
    [Parameter]
    public IList<DestinationFieldDto> AllItemsSource { get; set; }

    [Parameter]
    public string PidxNamespace { get; set; }

    [Parameter]
    public IList<ValueFormatDto> FormatsSource { get; set; }

    [Parameter]
    public InvoiceExchangeMessageFieldMappingDto Model { get; set; }

    [Parameter]
    public bool ShowFormatSelection { get; set; }

    public IEnumerable<DestinationFieldDto> ItemsSource => AllItemsSource.Where(s => s.Namespace == PidxNamespace);

    private string SelectedEntity { get; set; }

    private string CurrentPath => ItemsSource?.FirstOrDefault(m => m.Id == Model?.DestinationModelFieldId)?.JsonPath;

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

    private IEnumerable<DestinationFieldDto> FilteredByEntity
    {
        get
        {
            if (ItemsSource == null || string.IsNullOrWhiteSpace(SelectedEntity))
            {
                return Enumerable.Empty<DestinationFieldDto>();
            }

            return ItemsSource.Where(f => f?.EntityName == SelectedEntity).OrderBy(f => f.FieldName);
        }
    }

    protected override Task OnParametersSetAsync()
    {
        // ensure fields in sync
        SelectedEntity = ItemsSource?.FirstOrDefault(f => f?.Id == Model.DestinationModelFieldId)?.EntityName;
        if (SelectedEntity == null)
        {
            Model.DestinationModelFieldId = null;
        }

        return base.OnParametersSetAsync();
    }

    private void SelectedEntityChanged(object obj)
    {
        Model.DestinationModelFieldId = null;
    }
}
