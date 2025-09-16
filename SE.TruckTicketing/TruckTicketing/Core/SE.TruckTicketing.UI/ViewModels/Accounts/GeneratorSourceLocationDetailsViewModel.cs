using System;
using System.Text.RegularExpressions;

using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.ViewModels.Accounts;

public class GeneratorSourceLocationDetailsViewModel
{
    public GeneratorSourceLocationDetailsViewModel(SourceLocation sourceLocation)
    {
        SourceLocation = sourceLocation;
        IsNew = sourceLocation?.Id == Guid.Empty;
        Breadcrumb = IsNew ? "New Source Location" : "Source Location " + SourceLocation?.SourceLocationName;
    }

    public string IdentifierMask { get; set; }

    public string IdentifierRadzenMask { get; set; }

    public bool IsAssociatedSourceLocationDisabled => SourceLocation.SourceLocationTypeCategory != SourceLocationTypeCategory.Well;

    public SourceLocationType SourceLocationType { get; set; }

    public Response<SourceLocation> Response { get; set; }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public SourceLocation SourceLocation { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string Title => IsNew ? "Add Source Location" : $"Editing {SourceLocation?.SourceLocationName}";

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string ValidationSummary { get; set; }

    public void SetIdentifier(string formattedIdentifier)
    {
        SourceLocation.Identifier = Regex.Replace(formattedIdentifier, "[^A-Z0-9]", "");
    }

    public void UpdateGeneratorAccountPropertyDependencies(Guid selectedGeneratorId)
    {
    }

    public void UpdateIdentifierMask(SourceLocationType sourceLocationType)
    {
        IdentifierRadzenMask = sourceLocationType?.FormatMask
                                                 ?.Replace('#', '*')
                                                  .Replace('@', '*');

        IdentifierMask = sourceLocationType?.FormatMask;
    }
}
