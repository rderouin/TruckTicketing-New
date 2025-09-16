using Radzen;
using Radzen.Blazor;

namespace SE.TruckTicketing.Client.Components.Validators;

/// <summary>
///     A validator component similar to RadzenNumericRangeValidator that doesn't fail if the value to validate is null.
/// </summary>
public class TridentNullableNumericRangeValidator : RadzenNumericRangeValidator
{
    protected override bool Validate(IRadzenFormComponent component)
    {
        dynamic value = component.GetValue();

        if (Min != null && (value != null && value < Min))
        {
            return false;
        }

        if (Max != null && (value != null && value > Max))
        {
            return false;
        }

        return true;
    }
}
