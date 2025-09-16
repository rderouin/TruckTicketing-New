using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Humanizer;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

using Radzen;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Pages.InvoiceExchange;

public partial class InvoiceExchangeConfigPage
{
    private AccountsDropDown<Guid?> _accountsDdl;

    private TridentApiDropDown<BusinessStream, Guid?> _businessStreamDdl;

    private EditContext _editContext;

    private TridentApiDropDown<LegalEntity, Guid?> _legalEntityDdl;

    private InvoiceExchangeDto _model;

    private Response<InvoiceExchangeDto> _response;

    public InvoiceExchangeConfigPage()
    {
        // null-object
        _editContext = new(_model = new());

        // general init
        SupportedMessageAdapters = Enum.GetValues<MessageAdapterType>()
                                       .Where(v => v != MessageAdapterType.Undefined)
                                       .ToDictionary(t => t, t => t.Humanize());
    }

    [Inject]
    public IServiceBase<InvoiceExchangeDto, Guid> InvoiceExchangeService { get; set; }

    [Inject]
    public IServiceBase<SourceFieldDto, Guid> InvoiceExchangeSourceFieldService { get; set; }

    [Inject]
    public IServiceBase<DestinationFieldDto, Guid> InvoiceExchangeDestinationFieldService { get; set; }

    [Inject]
    public IServiceBase<ValueFormatDto, Guid> InvoiceExchangeValueFormatService { get; set; }
    
    [Inject]
    private NotificationService NotificationService { get; set; }

    [Parameter]
    public Guid? Id { get; set; }

    [Parameter]
    public InvoiceExchangeType Type { get; set; }

    [Parameter]
    public Guid? OriginalInvoiceExchangeId { get; set; }

    [Parameter]
    public Guid? InvoiceExchangeIdToClone { get; set; }

    private Dictionary<MessageAdapterType, string> SupportedMessageAdapters { get; }

    private List<SourceFieldDto> SourceFields { get; set; } = new();

    private List<DestinationFieldDto> PidxFields { get; set; } = new();

    private List<ValueFormatDto> ValueFormats { get; set; } = new();

    private List<EDIFieldDefinition> Fields { get; set; } = new();

    private int SelectedTabIndex { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await WithLoadingScreen(async () =>
                                {
                                    // remote lookups
                                    var searchSourceFieldsTask = InvoiceExchangeSourceFieldService.Search(new());
                                    var destinationFieldsTask = InvoiceExchangeDestinationFieldService.Search(new());
                                    var formatsTask = InvoiceExchangeValueFormatService.Search(new());
                                    var sourceFieldsSearchResult = await searchSourceFieldsTask;
                                    var destinationFieldsSearchResult = await destinationFieldsTask;
                                    var valueFormatsSearchResults = await formatsTask;
                                    SourceFields = sourceFieldsSearchResult.Results.ToList();
                                    PidxFields = destinationFieldsSearchResult.Results.ToList();
                                    ValueFormats = valueFormatsSearchResults.Results.ToList();
                                });
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        await WithLoadingScreen(async () =>
                                {
                                    // fetch the existing config
                                    if (Id.HasValue)
                                    {
                                        var config = await InvoiceExchangeService.GetById(Id.Value);
                                        if (config == null)
                                        {
                                            await Console.Error.WriteLineAsync($"The specified configuration doesn't exist: {Id}");
                                            NavigationManager.NavigateTo("/invoice-exchanges");
                                            return;
                                        }

                                        // postfix
                                        config.InvoiceDeliveryConfiguration ??= new();
                                        config.InvoiceDeliveryConfiguration.Mappings ??= new();
                                        config.FieldTicketsDeliveryConfiguration ??= new();
                                        config.FieldTicketsDeliveryConfiguration.Mappings ??= new();

                                        // take it
                                        _editContext = new(_model = config);
                                    }
                                    else
                                    {
                                        // init new
                                        var newInvoiceExchange = new InvoiceExchangeDto { Type = Type };
                                        
                                        // fetch the referenced Invoice Exchange
                                        if (OriginalInvoiceExchangeId.HasValue)
                                        {
                                            var invoiceExchange = await InvoiceExchangeService.GetById(OriginalInvoiceExchangeId.Value);
                                            newInvoiceExchange.InheritValuesFrom(invoiceExchange, false);
                                        }
                                        else if (InvoiceExchangeIdToClone.HasValue)
                                        {
                                            var invoiceExchange = await InvoiceExchangeService.GetById(InvoiceExchangeIdToClone.Value);
                                            newInvoiceExchange.InheritValuesFrom(invoiceExchange, true);
                                        }
                                        
                                        // init the context
                                        _editContext = new(_model = newInvoiceExchange);
                                    }
                                });
    }

    private async Task SaveConfig(EditContext context)
    {
        if (Id == null)
        {
            _response = await InvoiceExchangeService.Create(_model);
        }
        else
        {
            _response = await InvoiceExchangeService.Update(_model);
        }

        if (_response.IsSuccessStatusCode)
        {
            NavigationManager.NavigateTo("/invoice-exchanges");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, detail: "Failed to save the configuration.");
        }
    }

    private void SupportsFieldTicketsUpdated(bool newValue)
    {
        _model.SupportsFieldTickets = newValue;

        if (_model.SupportsFieldTickets == false)
        {
            SelectedTabIndex = 0;
        }
    }

    private void OnEDIFieldsChanged(List<EDIFieldDefinition> list)
    {
        Fields = list;
        StateHasChanged();
    }

    private Task OnCancelClick(MouseEventArgs arg)
    {
        NavigationManager.NavigateTo("/invoice-exchanges");
        return Task.CompletedTask;
    }

    private void OnBusinessStreamLoading(SearchCriteriaModel arg)
    {
    }

    private async Task OnBusinessStreamSelect(BusinessStream arg)
    {
        _model.BusinessStreamName = arg?.Name;

        _model.LegalEntityId = null;
        _model.LegalEntityName = null;

        _model.BillingAccountId = null;
        _model.BillingAccountName = null;
        _model.BillingAccountNumber = null;
        _model.BillingAccountDunsNumber = null;

        await _legalEntityDdl.Reload();
    }

    private void OnLegalEntityLoading(SearchCriteriaModel arg)
    {
        arg.Filters[nameof(LegalEntity.BusinessStreamId)] = _model.BusinessStreamId;
    }

    private async Task OnLegalEntitySelect(LegalEntity arg)
    {
        _model.LegalEntityName = arg?.Name;

        _model.BillingAccountId = null;
        _model.BillingAccountName = null;
        _model.BillingAccountNumber = null;
        _model.BillingAccountDunsNumber = null;

        await _accountsDdl.Reload();
    }

    private void OnAccountLoading(SearchCriteriaModel arg)
    {
        arg.Filters[nameof(Account.LegalEntityId)] = _model.LegalEntityId;
    }

    private void OnAccountSelect(Account arg)
    {
        _model.BillingAccountName = arg?.Name;
        _model.BillingAccountNumber = arg?.CustomerNumber;
        _model.BillingAccountDunsNumber = arg?.DUNSNumber;
    }
}
