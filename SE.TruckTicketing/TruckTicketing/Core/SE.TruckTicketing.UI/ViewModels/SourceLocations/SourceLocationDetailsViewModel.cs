using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Extensions;

namespace SE.TruckTicketing.UI.ViewModels.SourceLocations;

public class SourceLocationDetailsViewModel
{
    public SourceLocationDetailsViewModel(SourceLocation sourceLocation, SourceLocationSettings settings)
    {
        Settings = settings;
        SourceLocation = sourceLocation;
        IsNew = SourceLocation.Id == Guid.Empty;
        Backup = sourceLocation.Clone();
        if (IsNew)
        {
            var now = DateTimeOffset.Now;
            SourceLocation.GeneratorStartDate = new DateTimeOffset(now.Year, now.Month, 1, 7, 0, 0, default);
        }
    }

    private SourceLocation Backup { get; }

    private string Separator =>
        (SourceLocation.CountryCode == CountryCode.CA ? !string.IsNullOrEmpty(SourceLocation.FormattedIdentifier) : !string.IsNullOrEmpty(SourceLocation.SourceLocationName)) &&
        !string.IsNullOrEmpty(SourceLocation.GeneratorName)
            ? "-"
            : string.Empty;

    private string SourceLocationIdentifier => SourceLocation.CountryCode == CountryCode.CA ? SourceLocation.FormattedIdentifier : SourceLocation.SourceLocationName;

    public SourceLocationSettings Settings { get; }

    public string AssociatedSourceLocationsCardTitle =>
        SourceLocation.SourceLocationTypeCategory switch
        {
            SourceLocationTypeCategory.Well => "Associated Surface Source Locations",
            SourceLocationTypeCategory.Surface => "Associated Well Source Locations",
            _ => "Associated Source Locations",
        };

    public string IdentifierMask { get; set; }

    public string SourceLocationCodeMask { get; set; }

    public string IdentifierRadzenMask { get; set; }

    public bool IsAssociatedSourceLocationDisabled => SourceLocation.SourceLocationTypeCategory != SourceLocationTypeCategory.Well;

    public bool IsNew { get; }

    public SourceLocation SourceLocation { get; }

    public SourceLocationType SourceLocationType { get; set; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save";

    public string SubmitSuccessNotificationMessage => IsNew ? "Source location created." : "Source location updated.";

    public string Title => IsNew ? "Creating Source Location" : $"Editing {SourceLocationIdentifier} {Separator} {SourceLocation.GeneratorName}";

    public StateProvince Province
    {
        get => SourceLocation.ProvinceOrState;
        set
        {
            SourceLocation.ProvinceOrState = value;
            SourceLocation.ProvinceOrStateString = value == StateProvince.Unspecified ? default : value.ToString();
            UpdateSourceLocationCode();
        }
    }

    public bool HasStartDateChanged =>
        !IsNew &&
        Backup.GeneratorStartDate.HasValue &&
        SourceLocation.GeneratorStartDate.HasValue &&
        Backup.GeneratorStartDate.Value.CompareTo(SourceLocation.GeneratorStartDate.Value) != 0;

    public void SetSourceLocationType(SourceLocationType sourceLocationType)
    {
        SourceLocationType = sourceLocationType;
        SourceLocation.SourceLocationTypeId = sourceLocationType?.Id ?? Guid.Empty;
        SourceLocation.SourceLocationTypeName = sourceLocationType?.Name;
        SourceLocation.SourceLocationTypeCategory = sourceLocationType?.Category ?? SourceLocationTypeCategory.Undefined;

        SourceLocation.DownHoleType ??= SourceLocationType.DefaultDownHoleType;
        SourceLocation.DeliveryMethod ??= SourceLocationType.DefaultDeliveryMethod;

        SetFormattedIdentifier(string.Empty);
        UpdateIdentifierMask(sourceLocationType);
        UpdateSourceLocationCodeMask(sourceLocationType);
        UpdateProvince();
        UpdateSourceLocationCode();
    }

    private void UpdateProvince()
    {
        if (SourceLocationType is null)
        {
            Province = StateProvince.Unspecified;
            UpdateSourceLocationCode();
            return;
        }

        if (SourceLocationType.CountryCode == CountryCode.US)
        {
            Province = SourceLocationType.RequiresApiNumber ? GetUsStateByApiNumber(SourceLocation.ApiNumber) : StateProvince.ND;
            UpdateSourceLocationCode();
        }
    }

    private StateProvince GetUsStateByApiNumber(string apiNumber)
    {
        var prefix = new string((apiNumber ?? string.Empty).Take(2).ToArray());
        Settings.ApiNumberPrefixStateMap.TryGetValue(prefix, out var state);
        return Enum.TryParse<StateProvince>(state, out var stateProvince) ? stateProvince : StateProvince.Unspecified;
    }

    private void UpdateSourceLocationCode()
    {
        if (Province == StateProvince.Unspecified || !SourceLocationType.FormatMask.HasText())
        {
            SourceLocation.SourceLocationCode = default;
            return;
        }

        switch (SourceLocationType.CountryCode)
        {
            case CountryCode.CA:
            {
                var code = Province + SourceLocationType.ShortFormCode;

                if (SourceLocationType.Category is SourceLocationTypeCategory.Well)
                {
                    code += SourceLocation.Identifier;
                }
                else if (SourceLocationType.Category is SourceLocationTypeCategory.Surface)
                {
                    code += SourceLocation.Identifier;
                    code = code.PadRight(20, '0');
                }

                SourceLocation.SourceLocationCode = code;
                break;
            }
            case CountryCode.US when SourceLocationType.ShortFormCode.HasText():
                SourceLocation.SourceLocationCode = Province + SourceLocationType.ShortFormCode;
                break;
            default:
                SourceLocation.SourceLocationCode = default;
                break;
        }
    }

    public void SetFormattedIdentifier(string formattedIdentifier)
    {
        SourceLocation.FormattedIdentifier = formattedIdentifier;
        SourceLocation.Identifier = Regex.Replace(formattedIdentifier, "[^A-Z0-9]", "");
        UpdateSourceLocationCode();
    }

    public void UpdateIdentifierMask(SourceLocationType sourceLocationType)
    {
        IdentifierMask = sourceLocationType?.FormatMask;
        IdentifierRadzenMask = IdentifierMask?.Replace('#', '0').Replace('@', 'a');
    }

    public void UpdateSourceLocationCodeMask(SourceLocationType sourceLocationType)
    {
        SourceLocationCodeMask = sourceLocationType is not { EnforceSourceLocationCodeMask: true } ? string.Empty : $"{sourceLocationType.SourceLocationCodeMask}";
    }

    public void SetApiNumber(string apiNumber)
    {
        SourceLocation.ApiNumber = apiNumber;
        UpdateProvince();
    }
}

public class SourceLocationSettings
{
    public Dictionary<string, string> ApiNumberPrefixStateMap { get; set; } = new();

    public List<string> AllowedProvinces { get; set; }

    public List<string> AllowedStates { get; set; }
}
