using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.Accounts;
using SE.TruckTicketing.Client.Components.Customers;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Contracts.Api.Client;
using Trident.Extensions;
using Trident.Mapper;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;
using CreditStatus = SE.Shared.Common.Lookups.CreditStatus;

namespace SE.TruckTicketing.Client.Pages.Accounts;

public partial class NewAccountPage : BaseTruckTicketingComponent
{
    private Response<Account> _accountWorkflowValidationResponse;

    private Response<Account> _customerWorkflowValidationResponse;

    //Variables
    private EditContext _editContext;

    private bool _isLoading;

    private bool _isNextDisabled = true;

    private bool _isPreviousDisabled = true;

    private bool _isSaveEnabled;

    private bool _isValidating;

    private Response<NewAccountModel> _response;

    private string _saveButtonText = "Next";

    private RadzenSteps _steps;

    private NewAccountViewModel _viewModel;

    private bool isPageActingAsDialog = false;

    private bool isStepCreated;

    private string ReturnUrl => "/accounts";

    [CascadingParameter]
    private string AccountType { get; set; }

    [Parameter]
    public EventCallback<Account> AddAccount { get; set; }
    [Parameter]
    public EventCallback CloseDialog { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private INewAccountService NewAccountService { get; set; }

    [Inject]
    private IMapperRegistry Mapper { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        Account account = new()
        {
            Id = Guid.NewGuid(),
            AccountStatus = AccountStatus.Open,
            AccountAddresses = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsPrimaryAddress = true,
                },
            },
        };

        Account customerAccount = new()
        {
            AccountTypes = new() { AccountTypes.Customer.ToString() },
            AccountStatus = AccountStatus.Open,
            AccountAddresses = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsPrimaryAddress = true,
                },
            },
        };

        _viewModel = new(account, customerAccount);
        _editContext = new(account);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        if (AccountType.HasText())
        {
            _viewModel.SelectedAccountTypes = new List<AccountTypes> { Enum.Parse<AccountTypes>(AccountType) };
            isPageActingAsDialog = true;
        }

        _isLoading = false;
        await Task.CompletedTask;
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (AccountType.HasText() && _viewModel.SelectedAccountTypes.Any())
            {
                await OnAccountTypeChange(_viewModel.SelectedAccountTypes);
                StateHasChanged();
            }
        }
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        _viewModel.SubmitButtonDisabled = !_editContext.IsModified();
        _editContext.Validate();
    }

    private async Task OnHandleSubmit()
    {
        _isValidating = true;
        _viewModel.SetBillingConfigurationReferences();
        var cloneNewAccountRequest = _viewModel.Clone();
        cloneNewAccountRequest.CleanUpAccountReferences();
        cloneNewAccountRequest.Account.IncludeExternalDocumentAttachmentInLC = true;

        var newAccount = Mapper.Map<NewAccountModel>(cloneNewAccountRequest);
        var response = await NewAccountService.Create(newAccount);

        var showNewCustomerCreditReviewal = _viewModel.Account.AccountTypes.Contains(AccountTypes.Customer.ToString()) &&
                                            _viewModel.Account.BillingType != BillingType.CreditCard;

        if (response.IsSuccessStatusCode)
        {
            if (showNewCustomerCreditReviewal)
            {
                var _detailsViewModel = new NewCustomerCreditReviewalDialogViewModel { Account = _viewModel.Account };

                await DialogService.OpenAsync<NewCustomerCreditReviewalDialog>("Requires Customer Credit Reviewal",
                                                                               new()
                                                                               {
                                                                                   { nameof(NewCustomerCreditReviewalDialog.ViewModel), _detailsViewModel },
                                                                               });
            }

            if (AddAccount.HasDelegate)
            {
                await AddAccount.InvokeAsync(newAccount.Account);
            }

            if (!isPageActingAsDialog)
            {
                NotificationService.Notify(NotificationSeverity.Success, detail: _viewModel.SubmitSuccessNotificationMessage);
                NavigationManager.NavigateTo($"/account/edit/{_viewModel.Account.Id}");
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Failed to create new account(s).");
        }

        _response = response;
        _isValidating = false;
    }

    private void AddEditAccountContact()
    {
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(Account.Contacts)));
    }

    private async Task CloseButton_Click()
    {
        if (isPageActingAsDialog) 
        {
            await CloseDialog.InvokeAsync(true);
        }
        else 
        {
            if (!_editContext.IsModified())
            {
                NavigationManager.NavigateTo(ReturnUrl);
            }

            var confirmation = await DialogService.Confirm("Are you sure you want to close this page?", options: new()
            {
                OkButtonText = "Yes",
                CancelButtonText = "No",
            });

            if (confirmation.GetValueOrDefault())
            {
                NavigationManager.NavigateTo(ReturnUrl);
            }
        }
    }

    #region Billing Customer Step

    [SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    private async Task IsNewCustomerAdded(bool isBillingCustomerCreated)
    {
        _viewModel.IsCreateBillingCustomer = isBillingCustomerCreated;

        if (isBillingCustomerCreated)
        {
            _steps.AddStep(new()
            {
                Disabled = true,
                Text = "Billing Config",
                ChildContent = CreateDynamicComponent(typeof(NewAccountBillingConfiguration),
                                                      new()
                                                      {
                                                          { nameof(NewAccountBillingConfiguration.Account), _viewModel.BillingCustomer },
                                                          { nameof(NewAccountBillingConfiguration.billingConfiguration), _viewModel.BillingConfiguration },
                                                          { nameof(NewAccountBillingConfiguration.BillingCustomer), _viewModel.Account },
                                                          { nameof(NewAccountBillingConfiguration.EdiFieldDefinitions), _viewModel.EDIFieldDefinitions },
                                                          { nameof(NewAccountBillingConfiguration.MailingAddress), _viewModel.AccountMailingAddress },
                                                          { nameof(NewAccountBillingConfiguration.IsNewBillingCusomerCreated), _viewModel.IsCreateBillingCustomer },
                                                      }),
            });
        }

        if (!isBillingCustomerCreated)
        {
            _viewModel.NextButtonBusyText = "Saving";
            _saveButtonText = "Save";
            _isSaveEnabled = true;
            _isNextDisabled = false;
        }
        else
        {
            _viewModel.NextButtonBusyText = "Validating";
            _saveButtonText = "Next";
            _isSaveEnabled = false;
        }

        _isPreviousDisabled = _steps.SelectedIndex == 0;
        await Task.CompletedTask;
    }

    private async Task UpdateBillingCustomer(Account billingCustomer)
    {
        if (billingCustomer.Id == default)
        {
            //New BillingCustomerAccount created
            _viewModel.BillingCustomer.Id = default;
            _viewModel.BillingCustomer.Name = billingCustomer.Name;
            _viewModel.BillingCustomer.Contacts = new();
            _viewModel.BillingConfiguration.IsDefaultConfiguration = true;
            var primaryAccountAddress = _viewModel.BillingCustomer.AccountAddresses.First(x => x.IsPrimaryAddress);
            primaryAccountAddress = new();
        }
        else
        {
            //Existing BillingCustomerAccount selected
            _viewModel.BillingCustomer.Id = billingCustomer.Id;
            _viewModel.BillingCustomer.Name = billingCustomer.Name;
            _viewModel.BillingCustomer.BillingType = billingCustomer.BillingType;
            _viewModel.BillingConfiguration.BillingCustomerAccountId = billingCustomer.Id;
            _viewModel.BillingCustomer.Contacts = new(billingCustomer.Contacts);
            _viewModel.BillingCustomer.AccountAddresses = new(billingCustomer.AccountAddresses);
            var primaryAccountContact = _viewModel.BillingCustomer.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                  .FirstOrDefault(new AccountContact());

            _viewModel.SetBillingConfigurationContacts(primaryAccountContact);

            if (_viewModel.BillingConfiguration.Signatories.Count > 0)
            {
                _viewModel.BillingConfiguration.Signatories = new();
            }

            if (_viewModel.BillingConfiguration.EmailDeliveryContacts.Count > 0)
            {
                _viewModel.BillingConfiguration.EmailDeliveryContacts = new();
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Supporting Methods

    public RenderFragment CreateDynamicComponent(Type componentType, Dictionary<string, object> parameters)
    {
        return builder =>
               {
                   builder.OpenComponent(0, componentType);
                   var i = 0;
                   foreach (var kv in parameters)
                   {
                       builder.AddAttribute(i, kv.Key, kv.Value);
                   }

                   builder.CloseComponent();
               };
    }

    [SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    private async Task NextClick()
    {
        if (_isSaveEnabled)
        {
            await OnHandleSubmit();
            return;
        }

        var currentStepText = _steps.StepsCollection[_steps.SelectedIndex].Text;
        _isValidating = true;
        if (currentStepText == "General Info")
        {
            var response = await NewAccountService.AccountWorkflowValidation(_viewModel.Account);

            if (response.IsSuccessStatusCode)
            {
                _accountWorkflowValidationResponse = new() { StatusCode = HttpStatusCode.Accepted };
                _steps.StepsCollection[_steps.SelectedIndex + 1].Disabled = false;
                await ClickNextStep(_steps.SelectedIndex + 1);
            }

            _accountWorkflowValidationResponse = response;
        }
        else if (currentStepText == "Billing Customer")
        {
            var response = await NewAccountService.AccountWorkflowValidation(_viewModel.BillingCustomer);

            if (response.IsSuccessStatusCode)
            {
                _steps.StepsCollection[_steps.SelectedIndex + 1].Disabled = false;
                _customerWorkflowValidationResponse = new() { StatusCode = HttpStatusCode.Accepted };

                await ClickNextStep(_steps.SelectedIndex + 1);
            }

            _customerWorkflowValidationResponse = response;
        }

        _isValidating = false;
    }

    private async Task PreviousClick()
    {
        _isNextDisabled = _steps.SelectedIndex - 1 == _steps.StepsCollection.Count - 1;
        _isPreviousDisabled = _steps.SelectedIndex == 0;
        if (!_isNextDisabled)
        {
            _viewModel.NextButtonBusyText = "Validating";
            _saveButtonText = "Next";
            _isSaveEnabled = false;
        }

        await _steps.PrevStep();
    }

    private async Task ClickNextStep(int index)
    {
        _isNextDisabled = index == _steps.StepsCollection.Count - 1;
        await CheckForSaveEnabled();
        _isPreviousDisabled = index == 0;
        await _steps.NextStep();
        await Task.CompletedTask;
    }

    private async Task CheckForSaveEnabled()
    {
        if (_steps.SelectedIndex + 1 == _steps.StepsCollection.Count - 1 && !_isSaveEnabled)
        {
            _viewModel.NextButtonBusyText = "Saving";
            _saveButtonText = "Save";
            _isSaveEnabled = true;
            _isNextDisabled = false;
        }
        else
        {
            _viewModel.NextButtonBusyText = "Validating";
            _saveButtonText = "Next";
            _isSaveEnabled = false;
        }

        await Task.CompletedTask;
    }

    private void OnLegalEntityCountryCodeChange(CountryCode legalEntityCountryCode)
    {
        _viewModel.SelectedLegalEntityCountryCode = legalEntityCountryCode;
    }

    [SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
    private async Task OnAccountTypeChange(IEnumerable<AccountTypes> selectedAccountTypes)
    {
        _viewModel.SelectedAccountTypes = selectedAccountTypes;
        var viewModelSelectedAccountTypes = _viewModel.SelectedAccountTypes.ToList();
        _viewModel.IsAccountTypeCustomer = viewModelSelectedAccountTypes.Contains(AccountTypes.Customer);
        _viewModel.IsAccountTypeGenerator = viewModelSelectedAccountTypes.Contains(AccountTypes.Generator);
        var isTruckingCompanyAndThirdPartyAnalytical = viewModelSelectedAccountTypes.Contains(AccountTypes.ThirdPartyAnalytical) &&
                                                       viewModelSelectedAccountTypes.Contains(AccountTypes.TruckingCompany)
                                                    && !viewModelSelectedAccountTypes.Except(new[] { AccountTypes.ThirdPartyAnalytical, AccountTypes.TruckingCompany }).Any();

        _viewModel.IsAccountTypeThirdPartyAnalytical = !viewModelSelectedAccountTypes.Except(new[] { AccountTypes.ThirdPartyAnalytical }).Any() || isTruckingCompanyAndThirdPartyAnalytical;
        _viewModel.IsAccountTypeTruckingCompany = !viewModelSelectedAccountTypes.Except(new[] { AccountTypes.TruckingCompany }).Any() || isTruckingCompanyAndThirdPartyAnalytical;

        //If same account is selected as Customer and Generator
        if (_viewModel.IsAccountTypeCustomer && _viewModel.IsAccountTypeGenerator)
        {
            if (!_viewModel.Account.AccountAddresses.Any(x => x.IsPrimaryAddress))
            {
                _viewModel.Account.AccountAddresses = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        IsPrimaryAddress = true,
                    },
                };
            }

            _viewModel.Account.CreditStatus = CreditStatus.Pending;
            _viewModel.BillingCustomer.Contacts = new();
            _viewModel.BillingCustomer.AccountAddresses = new();
            _viewModel.BillingCustomer.Name = String.Empty;
            _viewModel.BillingCustomer.Id = default;
            _viewModel.BillingConfiguration.BillingCustomerAccountId = _viewModel.Account.Id;
            _viewModel.BillingConfiguration.IsDefaultConfiguration = true;
            _viewModel.BillingConfiguration.CustomerGeneratorId = _viewModel.Account.Id;
            _viewModel.BillingConfiguration.CustomerGeneratorName = _viewModel.Account.Name;
            var primaryAccountContact = _viewModel.Account.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                  .FirstOrDefault(new AccountContact());

            _viewModel.SetBillingConfigurationContacts(primaryAccountContact, true);
        }

        //If current account is Customer; no Generator Source Location created
        if (_viewModel.IsAccountTypeCustomer && !_viewModel.IsAccountTypeGenerator)
        {
            _viewModel.Account.CreditStatus = CreditStatus.Pending;
            _viewModel.BillingCustomer.Contacts = new();
            _viewModel.BillingCustomer.AccountAddresses = new();
            _viewModel.BillingCustomer.Name = String.Empty;
            _viewModel.BillingCustomer.Id = default;
            _viewModel.BillingConfiguration.CustomerGeneratorId = default;
            _viewModel.BillingConfiguration.CustomerGeneratorName = null;
            var primaryAccountContact = _viewModel.Account.Contacts.Where(x => x.IsPrimaryAccountContact)
                                                  .FirstOrDefault(new AccountContact());

            _viewModel.SetBillingConfigurationContacts(primaryAccountContact);
        }

        //If current account is Generator;
        if (_viewModel.IsAccountTypeGenerator && !_viewModel.IsAccountTypeCustomer)
        {
            _viewModel.BillingCustomer.AccountAddresses = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    IsPrimaryAddress = true,
                },
            };

            _viewModel.BillingCustomer.CreditStatus = CreditStatus.Pending;
            _viewModel.SetBillingConfigurationContacts(new());
            _viewModel.BillingConfiguration.CustomerGeneratorId = _viewModel.Account.Id;
            _viewModel.BillingConfiguration.CustomerGeneratorName = _viewModel.Account.Name;
        }

        //Remove existing steps
        var selectedSteps = new List<RadzenStepsItem>(_steps.StepsCollection);

        if (selectedSteps.Count > 1)
        {
            for (var i = 1; i < selectedSteps.Count; i++)
            {
                _steps.RemoveStep(selectedSteps[i]);
            }
        }

        _viewModel.Account.AccountTypes = new();

        if (_viewModel.IsAccountTypeGenerator || _viewModel.IsAccountTypeCustomer)
        {
            _viewModel.NextButtonBusyText = "Validating";
            _saveButtonText = "Next";
            _isSaveEnabled = false;
            _isNextDisabled = false;
        }

        //Add new steps
        foreach (var accountType in viewModelSelectedAccountTypes)
        {
            _viewModel.Account.AccountTypes.Add(accountType.ToString());

            if (!isStepCreated)
            {
                if (viewModelSelectedAccountTypes.Any() && _viewModel.IsAccountTypeGenerator &&
                    _viewModel.IsAccountTypeCustomer)
                {
                    _steps.AddStep(new()
                    {
                        Disabled = true,
                        Text = "Billing Config",
                        ChildContent = CreateDynamicComponent(typeof(NewAccountBillingConfiguration),
                                                              new()
                                                              {
                                                                  { nameof(NewAccountBillingConfiguration.Account), _viewModel.Account },
                                                                  { nameof(NewAccountBillingConfiguration.billingConfiguration), _viewModel.BillingConfiguration },
                                                                  { nameof(NewAccountBillingConfiguration.BillingCustomer), _viewModel.BillingCustomer },
                                                                  { nameof(NewAccountBillingConfiguration.EdiFieldDefinitions), _viewModel.EDIFieldDefinitions },
                                                                  { nameof(NewAccountBillingConfiguration.MailingAddress), _viewModel.AccountMailingAddress },
                                                                  { nameof(NewAccountBillingConfiguration.IsNewBillingCusomerCreated), false },
                                                              }),
                    });

                    isStepCreated = true;
                }
                else
                {
                    if (_viewModel.IsAccountTypeGenerator)
                    {
                        _steps.AddStep(new()
                        {
                            Disabled = true,
                            Text = "Billing Customer",
                            ChildContent = CreateDynamicComponent(typeof(NewAccountBillingCustomer),
                                                                  new()
                                                                  {
                                                                      { "billingConfiguration", _viewModel.BillingConfiguration },
                                                                      { "BillingCustomer", _viewModel.BillingCustomer },
                                                                      { "BillingCustomerAddress", _viewModel.BillingCustomer.AccountAddresses.First(x => x.IsPrimaryAddress) },
                                                                      { "Steps", _steps },
                                                                      { "IsCustomerCreated", _viewModel.IsCreateBillingCustomer },
                                                                      { nameof(NewAccountBillingCustomer.NewCustomerCreated), new EventCallback<bool>(this, IsNewCustomerAdded) },
                                                                      { nameof(NewAccountBillingCustomer.SelctedBillingCustomer), new EventCallback<Account>(this, UpdateBillingCustomer) },
                                                                  }),
                        });
                    }
                    else if (_viewModel.IsAccountTypeCustomer)
                    {
                        _steps.AddStep(new()
                        {
                            Disabled = true,
                            Text = "Billing Config",
                            ChildContent = CreateDynamicComponent(typeof(NewAccountBillingConfiguration),
                                                                  new()
                                                                  {
                                                                      { nameof(NewAccountBillingConfiguration.Account), _viewModel.Account },
                                                                      { nameof(NewAccountBillingConfiguration.billingConfiguration), _viewModel.BillingConfiguration },
                                                                      { nameof(NewAccountBillingConfiguration.BillingCustomer), _viewModel.BillingCustomer },
                                                                      { nameof(NewAccountBillingConfiguration.EdiFieldDefinitions), _viewModel.EDIFieldDefinitions },
                                                                      { nameof(NewAccountBillingConfiguration.MailingAddress), _viewModel.AccountMailingAddress },
                                                                      { nameof(NewAccountBillingConfiguration.IsNewBillingCusomerCreated), false },
                                                                  }),
                        });
                    }

                    isStepCreated = true;
                }
            }
        }

        if (_steps.StepsCollection.Count == 1)
        {
            _viewModel.NextButtonBusyText = "Saving";
            _saveButtonText = "Save";
            _isSaveEnabled = true;
            _isNextDisabled = false;
        }

        _steps.Visible = true;
        isStepCreated = false;

        await Task.CompletedTask;
    }

    #endregion
}
