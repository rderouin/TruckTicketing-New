using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;

namespace SE.TruckTicketing.Client.Components.InvoiceConfigurationComponents;

public partial class InvoiceConfigurationSplitting : BaseTruckTicketingComponent
{
    private IEnumerable<Guid> _ediFields;

    private TridentApiListBox<EDIFieldDefinition, Guid> _ediFieldsListBox;

    private EditContext _editContext;

    [Parameter]
    public EventCallback<bool> OnInvoiceConfigSplittingChanged { get; set; }

    private IEnumerable<InvoiceSplittingCategories> SplittingCategories
    {
        get => InvoiceConfiguration.SplittingCategories?.Select(Enum.Parse<InvoiceSplittingCategories>);
        set
        {
            InvoiceConfiguration.IsSplitByServiceType = value.Contains(InvoiceSplittingCategories.ServiceType);
            InvoiceConfiguration.IsSplitBySourceLocation = value.Contains(InvoiceSplittingCategories.SourceLocation);
            InvoiceConfiguration.IsSplitBySubstance = value.Contains(InvoiceSplittingCategories.Substance);
            InvoiceConfiguration.IsSplitByWellClassification = value.Contains(InvoiceSplittingCategories.WellClassification);
            InvoiceConfiguration.SplittingCategories = value.Select(x => x.ToString()).ToList();
        }
    }

    [Parameter]
    public InvoiceConfiguration InvoiceConfiguration { get; set; }

    private IEnumerable<Guid> EdiFields
    {
        get => _ediFields ?? InvoiceConfiguration.SplitEdiFieldDefinitions;
        set
        {
            InvoiceConfiguration.SplitEdiFieldDefinitions = value.ToList();
            _ediFields = value;
        }
    }

    [Inject]
    private IServiceBase<EDIFieldDefinition, Guid> EdiFieldDefinitionServiceProxy { get; set; }

    protected override void OnInitialized()
    {
        _editContext = new(InvoiceConfiguration);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        base.OnInitialized();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnInvoiceConfigSplittingChanged.InvokeAsync(_editContext.IsModified());
    }

    private void BeforeLoadingEdiFields(SearchCriteriaModel criteria)
    {
        criteria.Filters.TryAdd(nameof(EDIFieldDefinition.CustomerId), InvoiceConfiguration.CustomerId);
    }

    public async Task ReloadEDIFieldsOnCustomerChange()
    {
        if (_ediFieldsListBox != null)
        {
            await _ediFieldsListBox.ReloadData();
        }
    }

    private async Task OnEDIFieldSelect()
    {
        var defs = new List<EDIFieldDefinition>();
        foreach (var ediId in EdiFields)
        {
            var ediDef = await EdiFieldDefinitionServiceProxy.GetById(ediId, true);
            defs.Add(ediDef);
        }

        InvoiceConfiguration.SplitEdiFieldDefinitionNames = defs.Select(d => d.EDIFieldName).ToList();
    }
}
