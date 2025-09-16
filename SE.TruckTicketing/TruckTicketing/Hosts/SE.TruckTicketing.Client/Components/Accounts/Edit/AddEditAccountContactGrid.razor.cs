using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Security;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.Search;
using Trident.UI.Blazor.Components.Grid;
using Trident.UI.Blazor.Components.Grid.Filters;

using CompareOperators = Trident.Api.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Components.Accounts.Edit;

public partial class AddEditAccountContactGrid : BaseTruckTicketingComponent
{
    private SearchResultsModel<AccountContact, SearchCriteriaModel> _accountContacts = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<AccountContact>(),
    };

    private AccountContactViewModel _contactViewModel;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    protected PagableGridView<AccountContact> grid;

    private CountryCode _legalEntityCountryCode { get; set; }

    [Parameter]
    public Account Account { get; set; }

    [Parameter]
    public EventCallback OnAddEditAccountContact { get; set; }

    [Inject]
    public IServiceBase<LegalEntity, Guid> LegalEntityService { get; set; }

    [Parameter]
    public bool IsEdit { get; set; }

    private Dictionary<string, FilterOptions> GridFilterOptionDict { get; set; } = new();

    private bool HasAccountWritePermission => HasWritePermission(Permissions.Resources.Account);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task UpdateAccountContact(AccountContact contact)
    {
        if (contact.Id == default)
        {
            contact.Id = Guid.NewGuid();
            Account.Contacts.Add(contact);
        }
        else
        {
            var updatedContact = Account.Contacts.FirstOrDefault(x => x.Id == contact.Id, new());
            if (updatedContact.Id != default)
            {
                var index = Account.Contacts.IndexOf(updatedContact);
                if (index != -1)
                {
                    Account.Contacts[index] = contact;
                }
            }
        }

        DialogService.Close();
        await OnAddEditAccountContact.InvokeAsync();
        await grid.ReloadGrid();
    }

    protected override async Task OnParametersSetAsync()
    {
        ConfigureFilters();
        await LoadAccountContacts(new() { PageSize = 10 });
        await base.OnParametersSetAsync();
    }

    private async Task LoadAccountContacts(SearchCriteriaModel current)
    {
        _isLoading = true;

        var contacts = Account.Contacts.Where(x => !x.IsDeleted).ToList();

        if (!string.IsNullOrEmpty(current.Keywords))
        {
            var phoneKeyWord = current.Keywords.ToLower().Replace("(", "").Replace(")", "").Replace("-", "");
            var lowerKeyword = current.Keywords.ToLower();
            contacts = contacts.Where(x => x.Name.ToLower().Contains(lowerKeyword)
                                        || x.LastName.ToLower().Contains(lowerKeyword)
                                        || x.ReferenceId == current.Keywords
                                        || (x.PhoneNumber.HasText() && x
                                                                      .PhoneNumber.ToLower().Replace(" ", "").Replace("(", "").Replace(")", "")
                                                                      .Replace("-", "")
                                                                      .Contains(phoneKeyWord))
                                        || (x.Email.HasText() && x.Email.ToLower().Contains(lowerKeyword))).ToList();

            // lets check if legalentity contains keyword if we havent found a contact
            if (Account.LegalEntityId != default && !contacts.Any())
            {
                var legalContainsKeyword = Account.LegalEntity.ToLower().Contains(lowerKeyword);
                if (legalContainsKeyword)
                {
                    // we found that legalentity contains the keyword, so let's just return contacts that havent been deleted
                    contacts = Account.Contacts.Where(x => !x.IsDeleted).ToList();
                }
            }
        }

        if (current.Filters.Any())
        {
            var filterCompare = JsonConvert.DeserializeObject<Compare>(current.Filters["ContactFunctions.Raw"].ToString());
            contacts = contacts.Where(x => x.ContactFunctions.Contains(filterCompare.Value)).ToList();
        }

        var myList = contacts.ToList();
        var morePages = current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault() < myList.Count;
        var results = new SearchResultsModel<AccountContact, SearchCriteriaModel>
        {
            Results = myList
                     .Skip(current.PageSize.GetValueOrDefault() * current.CurrentPage.GetValueOrDefault())
                     .Take(current.PageSize.GetValueOrDefault()),
            Info = new()
            {
                TotalRecords = myList.Count,
                NextPageCriteria = morePages
                                       ? new SearchCriteriaModel
                                       {
                                           CurrentPage = current.CurrentPage + 1,
                                           Keywords = current.Keywords,
                                           Filters = current.Filters,
                                       }
                                       : null,
                Keywords = current.Keywords,
                Filters = current.Filters,
            },
        };

        _accountContacts = results;

        if (Account.LegalEntityId != default)
        {
            var legalEntity = await LegalEntityService.GetById(Account.LegalEntityId);
            _legalEntityCountryCode = legalEntity?.CountryCode ?? CountryCode.Undefined;
        }

        _isLoading = false;

        StateHasChanged();

        await Task.CompletedTask;
    }

    private async Task DeleteAccountContact(AccountContact contact)
    {
        var deletedContact = Account.Contacts.FirstOrDefault(x => x.Id == contact.Id, new());
        if (deletedContact.Id != default)
        {
            var index = Account.Contacts.IndexOf(deletedContact);
            if (index != -1)
            {
                contact.IsDeleted = true;
                Account.Contacts[index] = contact;
            }
        }

        DialogService.Close();
        await OnAddEditAccountContact.InvokeAsync();
        await grid.ReloadGrid();
    }

    private async Task OpenEditDialog(AccountContact model = null)
    {
        _contactViewModel = new(model?.Clone() ?? new AccountContact
        {
            AccountContactAddress = new() { Country = _legalEntityCountryCode },
            IsActive = true,
        }, Account);

        await DialogService.OpenAsync<AddEditAccountContact>(_contactViewModel.Title,
                                                             new()
                                                             {
                                                                 { nameof(AddEditAccountContact.ViewModel), _contactViewModel },
                                                                 {
                                                                     nameof(AddEditAccountContact.OnSubmit),
                                                                     new EventCallback<AccountContact>(this, (Func<AccountContact, Task>)(async model => await UpdateAccountContact(model)))
                                                                 },
                                                                 {
                                                                     nameof(AddEditAccountContact.OnDelete),
                                                                     new EventCallback<AccountContact>(this, (Func<AccountContact, Task>)(async model => await DeleteAccountContact(model)))
                                                                 },
                                                                 { nameof(AddEditAccountContact.OnCancel), new EventCallback(this, () => DialogService.Close()) },
                                                             },
                                                             new()
                                                             {
                                                                 Width = "60%",
                                                             });
    }

    private FilterOptions GetFilterOption(string key)
    {
        if (!string.IsNullOrWhiteSpace(key) && (GridFilterOptionDict?.ContainsKey(key) ?? false))
        {
            return GridFilterOptionDict[key];
        }

        return null;
    }

    private void ConfigureFilters()
    {
        GridFilterOptionDict = new()
        {
            {
                nameof(AccountContact.ContactFunctions), new SingleSelectDropDownFilterOptions
                {
                    FilterGroupNumber = 1,
                    Label = "Function Type",
                    FilterPath = nameof(AccountContact.ContactFunctions).AsPrimitiveCollectionFilterKey()!,
                    Operator = CompareOperators.contains,
                    ListItems = DataDictionary.For<AccountContactFunctions>().Select(x =>
                                                                                         new ListOption<string>
                                                                                         {
                                                                                             Display = x.Value,
                                                                                             Value = x.Key.ToString(),
                                                                                         }).ToList(),
                }
            },
        };
    }
}
