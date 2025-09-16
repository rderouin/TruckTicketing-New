using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen.Blazor;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Client.Components;

public partial class EmailAddressComponent<TModel> : BaseTruckTicketingComponent
    where TModel : class
{
    private List<string> _allSelectedContacts = new();

    private RadzenEmailValidator _customEmailValidator;

    private IEnumerable<string> _selectedContacts = new List<string>();

    private bool _showAddCustomEmail = false;

    private bool _showAddExistingContact = false;

    private string _value;

    private string CustomEmailAddress { get; set; }

    [Parameter]
    public string Value
    {
        get => _value;
        set => SetPropertyValue(ref _value, value, ValueChanged);
    }

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<string> Change { get; set; }

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public List<TModel> Contacts { get; set; }

    [Parameter]
    public bool IsRequired { get; set; }

    [Parameter]
    public string ContactDropdownLabel { get; set; }

    [Parameter]
    public string TextProperty { get; set; }

    [Parameter]
    public string ValueProperty { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (Value.HasText())
        {
            _allSelectedContacts.AddRange(Value.Split(";"));
        }

        await base.OnInitializedAsync();
    }

    private void OnContactDropDownChange()
    {
        if (_selectedContacts != null)
        {
            _allSelectedContacts.AddRange(_selectedContacts.Except(_allSelectedContacts));
        }

        Value = string.Join(";", _allSelectedContacts);
    }

    private void AddCustomEmailAddress()
    {
        if (CustomEmailAddress.HasText() && _customEmailValidator.IsValid)
        {
            _allSelectedContacts.Add(CustomEmailAddress);
            Value = string.Join(";", _allSelectedContacts);
            CustomEmailAddress = string.Empty;
        }
    }

    private void RemoveEmail(string email)
    {
        _allSelectedContacts.Remove(email);
        Value = string.Join(";", _allSelectedContacts);
    }

    private void ShowAddCustomEmail()
    {
        _showAddExistingContact = false;
        _showAddCustomEmail = true;
    }

    private void ShowAddExistingContact()
    {
        _showAddExistingContact = true;
        _showAddCustomEmail = false;
    }
}
