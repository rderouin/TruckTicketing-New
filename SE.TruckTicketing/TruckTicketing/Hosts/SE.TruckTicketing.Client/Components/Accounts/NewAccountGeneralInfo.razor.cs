using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.UI.Blazor.Components;

using AccountTypes = SE.Shared.Common.Lookups.AccountTypes;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class NewAccountGeneralInfo : BaseRazorComponent
{
    public string PostalCodeMask = "*****-****";

    public string PostalCodePattern = "[0-9]";

    protected RadzenTemplateForm<AccountAddress> ReferenceToAccountAddressForm;

    protected RadzenTemplateForm<AccountAddress> ReferenceToForm;

    public Dictionary<StateProvince, string> StateProviceData = new();

    private string StateProvinceLabel = "State/Province";

    private string StateProvincePlaceHolder = "Select State/Province...";

    private string ZipPostalCodeLabel = "Zip/Postal Code";

    [Parameter]
    public AccountAddress accountAddress { get; set; } = new();

    [Parameter]
    public IEnumerable<AccountTypes> SelectedAccountTypes { get; set; }

    [Parameter]
    public CountryCode LegalEntityCountryCode { get; set; }

    private IDictionary<string, Dictionary<StateProvince, string>> stateProvinceDataByCategory => DataDictionary.GetDataDictionaryByCategory<StateProvince>();

    private CountryCode SelectedCountry
    {
        get => accountAddress.Country;
        set
        {
            accountAddress.Country = value;
            UpdateStateProvinceLabel(value.ToString());
            StateProviceData = stateProvinceDataByCategory[Enum.Parse<CountryCode>(value.ToString()).ToString()];
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        ReferenceToAccountAddressForm.EditContext.Validate();
        await Task.CompletedTask;
    }

    protected override Task OnInitializedAsync()
    {
        if (LegalEntityCountryCode != CountryCode.Undefined && !accountAddress.ZipCode.HasText() && accountAddress.Province == StateProvince.Unspecified)
        {
            SelectedCountry = LegalEntityCountryCode;
        }

        return base.OnInitializedAsync();
    }

    private void UpdateStateProvinceLabel(string value)
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

    protected string ClassNames(params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", (classNames ?? Array.Empty<(string className, bool include)>()).Where(_ => _.include).Select(_ => _.className));
        return $"{classes}";
    }
}
