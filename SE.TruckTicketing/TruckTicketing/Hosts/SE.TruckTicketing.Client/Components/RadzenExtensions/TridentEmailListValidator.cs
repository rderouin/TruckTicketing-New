using System;
using System.Linq;
using System.Text.RegularExpressions;

using Radzen;
using Radzen.Blazor;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Client.Components.RadzenExtensions;

public partial class TridentEmailListValidator : ValidatorBase
{
    private readonly Regex _emailRegex = new("^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");

    public override string Text { get; set; } = "Email list must contain valid emails delimited by semi-colons";

    protected override bool Validate(IRadzenFormComponent component)
    {
        var value = component.GetValue() as string ?? string.Empty;
        if (!value.HasText())
        {
            return true;
        }

        var emails = value.Split(";");
        return emails.Where(email => email.HasText())
                     .All(IsValidEmail);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            return _emailRegex.IsMatch(email);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
