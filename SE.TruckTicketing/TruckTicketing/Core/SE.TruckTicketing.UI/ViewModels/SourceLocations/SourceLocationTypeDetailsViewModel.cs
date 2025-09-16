using System;

using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.ViewModels.SourceLocations;

public class SourceLocationTypeDetailsViewModel
{
    public SourceLocationTypeDetailsViewModel(SourceLocationType sourceLocationType)
    {
        SourceLocationType = sourceLocationType;
        IsNew = sourceLocationType?.Id == Guid.Empty;
    }

    public SourceLocationType SourceLocationType { get; }

    public Response<SourceLocationType> Response { get; set; }

    public bool IsNew { get; }

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Updating";

    public string Title => IsNew ? "Add Source Location Type" : $"Editing {SourceLocationType.Name}";

    public string ValidationSummary { get; set; }
}
