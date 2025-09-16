using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class AccountAddressAddEdit : BaseTruckTicketingComponent
{
    public string PostalCodeMask = "*****-****";

    public string PostalCodePattern = "[0-9]";

    protected RadzenTemplateForm<AccountAddress> ReferenceToForm;

    public Dictionary<StateProvince, string> StateProviceData = new();

    private string StateProvinceLabel = "State/Province";

    private string StateProvincePlaceHolder = "Select State/Province...";

    private string ZipPostalCodeLabel = "Zip/Postal Code";

    [Parameter]
    public AccountAddress AccountAddress { get; set; } = new();

    [Parameter]
    public EventCallback<AccountAddress> OnSubmit { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    private bool IsNew { get; set; }

    private bool IsSaveEnabled { get; set; }

    private IDictionary<string, Dictionary<StateProvince, string>> stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    private CountryCode SelectedCountry
    {
        get => AccountAddress.Country;
        set
        {
            AccountAddress.Country = value;
            UpdateStateProvinceLabel(value.ToString());
            StateProviceData = stateProvinceDataByCategory[value.ToString()];
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            IsNew = AccountAddress.Id == default;
            if (!IsNew || (AccountAddress != null && AccountAddress.Country != CountryCode.Undefined))
            {
                UpdateStateProvinceLabel(AccountAddress.Country.ToString());
                StateProviceData = stateProvinceDataByCategory[AccountAddress.Country.ToString()];
            }
        }
        catch (Exception e)
        {
            HandleException(e, nameof(AccountAddress), "An exception occurred getting claims in OnInitializedAsync");
        }
    }

    public async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task HandleSubmit()
    {
        await OnSubmit.InvokeAsync(AccountAddress);
    }

    public async Task OnChange()
    {
        IsSaveEnabled = !ReferenceToForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    public void UpdateStateProvinceLabel(string value)
    {
        if (Enum.Parse<CountryCode>(value) == CountryCode.CA)
        {
            StateProvincePlaceHolder = "Select Province...";
            StateProvinceLabel = "Province";
            PostalCodeMask = "*** ***";
            PostalCodePattern = "[0-9A-z]";
            ZipPostalCodeLabel = "Postal Code";
        }
        else
        {
            StateProvincePlaceHolder = "Select State...";
            StateProvinceLabel = "State";
            PostalCodeMask = "*****-****";
            PostalCodePattern = "[0-9]";
            ZipPostalCodeLabel = "Zip Code";
        }
    }
}
