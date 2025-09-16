using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using Radzen;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.ViewModels.Accounts;

using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class GeneratorSourceLocationDetails : BaseRazorComponent
{
    private IEnumerable<AccountContact> _contractOperatorContacts;

    private EditContext _editContext;

    private IEnumerable<AccountContact> _generatorContacts;

    private Response<SourceLocation> _sourceLocationWorkflowValidationResponse;

    protected bool IsBusy;

    public Dictionary<StateProvince, string> StateProvinceData = new();

    private string StateProvinceLabel = "State/Province";

    private string StateProvincePlaceHolder = "Select State/Province...";

    [Parameter]
    public GeneratorSourceLocationDetailsViewModel ViewModel { get; set; }

    [Inject]
    private NotificationService notificationService { get; set; }

    [Parameter]
    public EventCallback<SourceLocation> OnSubmit { get; set; }

    [Inject]
    private INewAccountService NewAccountService { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public Account Account { get; set; }

    [Inject]
    private IServiceBase<SourceLocation, Guid> SourceLocationService { get; set; }

    [Inject]
    private IServiceBase<SourceLocationType, Guid> SourceLocationTypeService { get; set; }

    private IDictionary<string, Dictionary<StateProvince, string>> stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _editContext = new(ViewModel.SourceLocation);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;
        await LoadSourceLocationType();
        _generatorContacts = Account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.ProductionAccountant.ToString())).OrderBy(x => x.Name).ToList() ??
                             new List<AccountContact>();
    }

    private void AssociateSourceLocation(Guid? associatedSourceLocationId)
    {
        ViewModel.SourceLocation.AssociatedSourceLocationId = associatedSourceLocationId;
        _editContext.NotifyFieldChanged(_editContext.Field(nameof(SourceLocation.AssociatedSourceLocationId)));
    }

    private async Task LoadSourceLocationType()
    {
        if ((ViewModel.SourceLocation?.SourceLocationTypeId ?? Guid.Empty) == Guid.Empty)
        {
            return;
        }

        if (ViewModel.SourceLocation != null)
        {
            var sourceLocation = await SourceLocationTypeService.GetById(ViewModel.SourceLocation.SourceLocationTypeId);
            ViewModel.SourceLocationType = sourceLocation;
        }
    }

    private void OnContractOperatorDropdownChange(Account account)
    {
        _contractOperatorContacts = account?.Contacts.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.ProductionAccountant.ToString())).OrderBy(x => x.Name).ToList() ??
                                    new List<AccountContact>();

        ViewModel.SourceLocation.GeneratorName = account?.Name;
        ViewModel.SourceLocation.ContractOperatorProductionAccountContactId = null;
    }

    private void OnContractOperatorDropdownLoad(Account account)
    {
        _contractOperatorContacts = account?.Contacts?.Where(x => x.ContactFunctions.Contains(AccountContactFunctions.ProductionAccountant.ToString())).OrderBy(x => x.Name).ToList() ??
                                    new List<AccountContact>();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        ViewModel.SubmitButtonDisabled = !_editContext.IsModified();
    }

    private void OnFormattedIdentifierTextBoxChange(object args)
    {
        var formattedIdentifier = args as string ?? String.Empty;
        ViewModel.SetIdentifier(formattedIdentifier);
    }

    private void OnSourceLocationTypeChange(SourceLocationType sourceLocationType)
    {
        ViewModel.UpdateIdentifierMask(sourceLocationType);
        ViewModel.SourceLocationType = sourceLocationType;
        ViewModel.SourceLocation.SourceLocationTypeName = sourceLocationType.Name;
        ViewModel.SourceLocation.SourceLocationTypeCategory = sourceLocationType.Category;
        ViewModel.SourceLocation.Identifier = null;
        ViewModel.SourceLocation.FormattedIdentifier = null;

        ViewModel.SourceLocation.DownHoleType ??= sourceLocationType.DefaultDownHoleType;
        ViewModel.SourceLocation.DeliveryMethod ??= sourceLocationType.DefaultDeliveryMethod;

        if (sourceLocationType.CountryCode == CountryCode.CA)
        {
            StateProvincePlaceHolder = "Select Province...";
            StateProvinceLabel = "Province";
        }
        else
        {
            StateProvincePlaceHolder = "Select State...";
            StateProvinceLabel = "State";
        }

        StateProvinceData = stateProvinceDataByCategory[sourceLocationType.CountryCode.ToString()];
    }

    private async Task HandleSubmit()
    {
        IsBusy = true;
        var response = await NewAccountService.SourceLocationWorkflowValidation(ViewModel.SourceLocation);
        _sourceLocationWorkflowValidationResponse = response;
        if (_sourceLocationWorkflowValidationResponse is { IsSuccessStatusCode: true })
        {
            notificationService.Notify(NotificationSeverity.Success,
                                       $"{ViewModel.SourceLocation.SourceLocationName} Source Location {(ViewModel.SourceLocation.Id == default ? "created" : "updated")}.");

            if (ViewModel.SourceLocation.Id == default)
            {
                ViewModel.SourceLocation.Id = Guid.NewGuid();
            }

            await OnSubmit.InvokeAsync(ViewModel.SourceLocation);
        }
        else
        {
            notificationService.Notify(NotificationSeverity.Error,
                                       $"{ViewModel.SourceLocation.SourceLocationName} Source Location {(ViewModel.SourceLocation.Id == default ? "create" : "update")} failed.");

            IsBusy = false;
        }
    }
}
