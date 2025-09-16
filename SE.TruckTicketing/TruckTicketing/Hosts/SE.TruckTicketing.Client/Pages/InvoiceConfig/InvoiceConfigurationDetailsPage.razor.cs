using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;
using Radzen.Blazor;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Contracts.Api.Models;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.InvoiceConfigurationComponents;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Pages.BillingConfig;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.InvoiceConfigurations;

using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.Extensions;

using SortOrder = Trident.Contracts.Enums.SortOrder;

namespace SE.TruckTicketing.Client.Pages.InvoiceConfig;

public partial class InvoiceConfigurationDetailsPage : BaseTruckTicketingComponent
{
    private readonly List<SourceLocation> SelectedSourceLocations = new();

    private List<BillingConfiguration> _clonedBillingConfigurations = new();

    private Account _customer;

    private List<BillingConfiguration> _invalidBillingConfigurations = new();

    private RadzenDropDownDataGrid<Guid?> _invoiceContactDropdown;

    private List<InvoiceExchangeDto> _invoiceExchanges = new();

    private BillingConfigurationGrid billingConfigurationGrid;

    private InvoiceConfigurationPermutationGrid invoiceConfigPermutationsGrid;

    private InvoiceConfigurationSplitting InvoiceConfigSplit;

    private InvoiceConfiguration invoiceConfigurationClone = new();

    private InvoiceConfigurationInvoiceInfo InvoiceInfo;

    private bool isInvoiceConfigSplittingSelectionChanged;

    private bool isInvoiceInfoSelectionChanged;

    private SearchResultsModel<InvoiceConfiguration, SearchCriteriaModel> Results = new();

    [Parameter]
    public Guid? Id { get; set; }

    [Parameter]
    public Guid? CustomerId { get; set; }

    [Parameter]
    public string Operation { get; set; }

    [Inject]
    private IInvoiceConfigurationService InvoiceConfigurationService { get; set; }

    [Inject]
    private IServiceBase<BillingConfiguration, Guid> BillingConfigurationService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IServiceBase<Account, Guid> AccountService { get; set; }

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    public IServiceBase<InvoiceExchangeDto, Guid> InvoiceExchangeService { get; set; }

    private bool SubmitButtonDisabled =>
        _viewModel.SubmitButtonDisabled || !HasWritePermission(Permissions.Resources.Account) ||
        (_viewModel.InvoiceConfiguration.CatchAll && !HasWritePermission(Permissions.Resources.DefaultInvoiceConfig));

    private bool IsCloneInvoiceConfiguration => Operation == "clone";

    private string ReturnUrl => NavigationHistoryManager.GetReturnUrl();

    private AccountContact InvoiceContact => _invoiceContactDropdown?.SelectedItem as AccountContact;

    private string InvoiceContactAddress => InvoiceContact?.Address;

    private string InvoiceContactEmail => InvoiceContact?.Email;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;

        if (CustomerId != default)
        {
            await LoadBillingCustomerAsync(CustomerId);
        }
        else
        {
            _customer = new();
        }

        if (Id != default)
        {
            await LoadInvoiceConfigurationAsync(Id, Operation);
        }
        else
        {
            await LoadInvoiceConfigurationAsync(null, Operation);
        }

        await LoadInvoiceExchanges();

        _isLoading = false;
        StateHasChanged();

        await base.OnInitializedAsync();
    }

    private async Task LoadInvoiceExchanges()
    {
        var resultsModel = await InvoiceExchangeService?.Search(new()
        {
            PageSize = int.MaxValue,
            CurrentPage = 0,
            Keywords = string.Empty,
            OrderBy = nameof(InvoiceExchangeDto.PlatformCode),
            SortOrder = SortOrder.Asc,
            Filters = new()
            {
                [nameof(InvoiceExchangeDto.IsDeleted)] = false,
                [nameof(InvoiceExchangeDto.Type)] = InvoiceExchangeType.Global.ToString(),
            },
        })!;

        _invoiceExchanges = resultsModel?.Results?.ToList() ?? new();
        StateHasChanged();
    }

    private async Task LoadBillingCustomerAsync(Guid? id = null)
    {
        _customer = id is null ? new() : await AccountService?.GetById(id.Value)!;
    }

    private async Task LoadInvoiceConfigurationAsync(Guid? invoiceConfigurationId, string operation)
    {
        var invoiceConfigurationResult = invoiceConfigurationId is null
                                             ? new()
                                             {
                                                 SplittingCategories = new()
                                                 {
                                                     InvoiceSplittingCategories.Facility.ToString(),
                                                     InvoiceSplittingCategories.SourceLocation.ToString(),
                                                     InvoiceSplittingCategories.WellClassification.ToString(),
                                                 },
                                             }
                                             : await InvoiceConfigurationService.GetById(invoiceConfigurationId.Value);

        if (operation == "clone")
        {
            var timeSpan = invoiceConfigurationResult.UpdatedAt != default
                               ? DateTimeOffset.UtcNow - invoiceConfigurationResult.UpdatedAt
                               : DateTimeOffset.UtcNow - invoiceConfigurationResult.CreatedAt;

            invoiceConfigurationResult.Id = Guid.NewGuid();
            invoiceConfigurationResult.Name = $"(COPY-{(int)timeSpan.TotalMinutes}) {invoiceConfigurationResult.Name}";
            await LoadClonedBillingConfigurations(invoiceConfigurationId);
            if (_clonedBillingConfigurations != null && _clonedBillingConfigurations.Any())
            {
                _clonedBillingConfigurations.ForEach(x =>
                                                     {
                                                         var billingConfigTimeSpan = x.UpdatedAt != default
                                                                                         ? DateTimeOffset.UtcNow - x.UpdatedAt
                                                                                         : DateTimeOffset.UtcNow - x.CreatedAt;

                                                         x.Id = Guid.NewGuid();
                                                         x.Name = $"(COPY-{(int)billingConfigTimeSpan.TotalMinutes}) {x.Name}";
                                                         x.InvoiceConfigurationId = invoiceConfigurationResult.Id;
                                                         x.IsValid = false;
                                                         if (x.Facilities is { Count: 0 })
                                                         {
                                                             x.Facilities = null;
                                                         }
                                                     });
            }
        }

        if (CustomerId != null && CustomerId != Guid.Empty && invoiceConfigurationId is null)
        {
            invoiceConfigurationResult.CustomerId = CustomerId.Value;
            invoiceConfigurationResult.CustomerName = _customer.Name;
            invoiceConfigurationResult.CustomerLegalEntityId = _customer.LegalEntityId;
            invoiceConfigurationResult.IncludeExternalDocumentAttachment = _customer.IncludeExternalDocumentAttachmentInLC;
            invoiceConfigurationResult.IncludeInternalDocumentAttachment = _customer.IncludeInternalDocumentAttachmentInLC;
        }

        _viewModel = new(invoiceConfigurationResult, Operation, _customer);
        await LoadSourceLocationGeneratorMap();
        _editContext = new(invoiceConfigurationResult);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
    }

    private async Task LoadSourceLocationGeneratorMap()
    {
        if (_viewModel.InvoiceConfiguration.Id != Guid.Empty && (!_viewModel.InvoiceConfiguration.AllSourceLocations ||
                                                                 (_viewModel.InvoiceConfiguration.SourceLocations != null && _viewModel.InvoiceConfiguration.SourceLocations.Any())))
        {
            foreach (var selectedSourceLocation in _viewModel.InvoiceConfiguration.SourceLocations)
            {
                var sourceLocation = await SourceLocationService!.GetById(selectedSourceLocation);
                if (sourceLocation != null)
                {
                    SelectedSourceLocations.Add(sourceLocation);
                }
            }

            if (SelectedSourceLocations.Any())
            {
                foreach (var sourceLocation in SelectedSourceLocations)
                {
                    GeneratorSourceLocationMap.TryAdd(sourceLocation.GeneratorId, new());
                    GeneratorSourceLocationMap[sourceLocation.GeneratorId].Add(sourceLocation);
                }
            }
        }
    }

    private async Task LoadClonedBillingConfigurations(Guid? invoiceConfigurationId = null)
    {
        var clonedBillingConfigurations = invoiceConfigurationId is null
                                              ? new()
                                              : await BillingConfigurationService.Search(new()
                                              {
                                                  Filters = new()
                                                  {
                                                      { nameof(BillingConfiguration.InvoiceConfigurationId), invoiceConfigurationId },
                                                  },
                                              });

        _clonedBillingConfigurations = clonedBillingConfigurations?.Results?.ToList() ?? new();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private async Task OnHandleSubmit()
    {
        _isSaving = true;
        Response<InvoiceConfiguration> invoiceConfigurationResponse = new();
        Response<CloneInvoiceConfigurationModel> cloneInvoiceConfigurationResponse = new();

        invoiceConfigurationClone = _viewModel.InvoiceConfiguration.Clone();
        //TODO : Maintain references of InvoiceConfiguration into BillingConfiguration when operation is cloned
        _viewModel.CleanupPrimitiveCollections();

        if (IsCloneInvoiceConfiguration)
        {
            var model = new CloneInvoiceConfigurationModel
            {
                InvoiceConfiguration = _viewModel.InvoiceConfiguration,
                BillingConfigurations = _clonedBillingConfigurations,
            };

            cloneInvoiceConfigurationResponse = await InvoiceConfigurationService.CloneInvoiceConfiguration(model);
            _cloneInvoiceConfigResponse = cloneInvoiceConfigurationResponse;
        }
        else
        {
            invoiceConfigurationResponse = _viewModel.IsNew
                                               ? await InvoiceConfigurationService.Create(_viewModel.InvoiceConfiguration)
                                               : await InvoiceConfigurationService.Update(_viewModel.InvoiceConfiguration);

            _response = invoiceConfigurationResponse;
        }

        _isSaving = false;

        if (invoiceConfigurationResponse.IsSuccessStatusCode || cloneInvoiceConfigurationResponse.IsSuccessStatusCode)
        {
            NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
            NavigationManager.NavigateTo(ReturnUrl);
        }
        else
        {
            _viewModel = new(invoiceConfigurationClone, Operation, _customer);
        }
    }

    private void AddSourceLocation(SourceLocation addedSourceLocation)
    {
        GeneratorSourceLocationMap.TryAdd(addedSourceLocation.GeneratorId, new());
        if (GeneratorSourceLocationMap[addedSourceLocation.GeneratorId].All(x => x.Id != addedSourceLocation.Id))
        {
            GeneratorSourceLocationMap[addedSourceLocation.GeneratorId].Add(addedSourceLocation);
        }
    }

    private void DeleteSourceLocation(SourceLocation deletedSourceLocation)
    {
        if (GeneratorSourceLocationMap[deletedSourceLocation.GeneratorId].Any(x => x.Id == deletedSourceLocation.Id))
        {
            GeneratorSourceLocationMap[deletedSourceLocation.GeneratorId].Remove(deletedSourceLocation);
        }

        if (!GeneratorSourceLocationMap[deletedSourceLocation.GeneratorId].Any())
        {
            GeneratorSourceLocationMap.Remove(deletedSourceLocation.GeneratorId);
        }
    }

    private async Task OnFacilityChanged(bool isFacilitySelectionChanged)
    {
        await invoiceConfigPermutationsGrid.ReloadPermutationsGrid();
    }

    private void OnInvoiceInfoSelectionChanged(bool isChanged)
    {
        _viewModel.SubmitButtonDisabled = !isChanged;
        isInvoiceInfoSelectionChanged = isChanged;
    }

    private void OnInvoiceConfigSplittingChange(bool isChanged)
    {
        _viewModel.SubmitButtonDisabled = !isChanged;
        isInvoiceConfigSplittingSelectionChanged = isChanged;
    }

    private async Task OnCustomerDropdownChange(Account account)
    {
        _viewModel.InvoiceConfiguration.CustomerName = account.Name;
        _viewModel.InvoiceConfiguration.IncludeInternalDocumentAttachment = account.IncludeInternalDocumentAttachmentInLC;
        _viewModel.InvoiceConfiguration.IncludeExternalDocumentAttachment = account.IncludeExternalDocumentAttachmentInLC;
        _viewModel.InvoiceConfiguration.CustomerLegalEntityId = account.LegalEntityId;
        if (InvoiceConfigSplit != null)
        {
            await InvoiceConfigSplit.ReloadEDIFieldsOnCustomerChange();
        }
    }

    private async Task AddBillingConfiguration()
    {
        await DialogService.OpenAsync<BillingConfigurationEdit>("New Billing Configuration", new()
        {
            { nameof(BillingConfigurationEdit.BillingCustomerId), _viewModel.InvoiceConfiguration.CustomerId.ToString() },
            { nameof(BillingConfigurationEdit.InvoiceConfigurationId), _viewModel.InvoiceConfiguration.Id },
            { nameof(BillingConfigurationEdit.AddEditBillingConfiguration), new EventCallback<BillingConfiguration>(this, NewBillingConfigurationAdded) },
            { nameof(BillingConfigurationEdit.CancelAddEditBillingConfiguration), new EventCallback<bool>(this, CancelBillingConfigurationAddEdit) },
        }, new()
        {
            Width = "80%",
            Height = "95%",
        });
    }

    private async Task NewBillingConfigurationAdded(BillingConfiguration billingConfiguration)
    {
        await ReloadBillingConfigurationGrid();
    }

    private async Task CancelBillingConfigurationAddEdit(bool isCanceled)
    {
        await ReloadBillingConfigurationGrid();
    }

    private async Task ReloadBillingConfigurationGrid()
    {
        DialogService.Close();
        await billingConfigurationGrid.ReloadBillingConfigurationGrid();
        StateHasChanged();
    }

    private async Task LoadInvalidBillingConfigurations(List<BillingConfiguration> invalidBillingConfigurations)
    {
        _invalidBillingConfigurations = new();
        if (!invalidBillingConfigurations.Any())
        {
            _viewModel.SubmitButtonDisabled = !invalidBillingConfigurations.Any();
            NotificationService.Notify(NotificationSeverity.Warning, detail: "No Invalid Billing Configuration(s) found");
        }
        else
        {
            _viewModel.SubmitButtonDisabled = true;
            _invalidBillingConfigurations.AddRange(invalidBillingConfigurations);
            NotificationService.Notify(NotificationSeverity.Error, detail: $"{invalidBillingConfigurations.Count} Invalid Billing Configuration(s) found");
        }

        await billingConfigurationGrid.ReloadBillingConfigurationGrid();
    }

    private void BillingContactChange(object arg)
    {
        if (arg is Guid billingContactId)
        {
            if (_viewModel.BillingContacts.FirstOrDefault(c => c.Id == billingContactId) is { } selectedContact)
            {
                _viewModel.InvoiceConfiguration.BillingContactName = selectedContact.DisplayName;
            }
        }
    }

    #region Variables

    private EditContext _editContext;

    private bool _isLoading;

    private bool _isSaving;

    private Response<InvoiceConfiguration> _response;

    private Response<CloneInvoiceConfigurationModel> _cloneInvoiceConfigResponse;

    private InvoiceConfigurationDetailsViewModel _viewModel = new(new(), null, new());

    public Dictionary<Guid, List<SourceLocation>> GeneratorSourceLocationMap = new();

    protected bool enableSave = false;

    #endregion
}
