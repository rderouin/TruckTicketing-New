using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels.Facilities;

public class FacilityDetailsViewModel
{
    public FacilityDetailsViewModel(Facility facility)
    {
        Facility = facility;
        Breadcrumb = IsNew ? "New Facility" : "Facility " + Facility?.SiteId;
        IsNew = Facility?.Id == default;
        if (Facility?.SpartanActive == null)
        {
            SpartanActive = false;
        }
    }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public Facility Facility { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    public string SubmitSuccessNotificationMessage => IsNew ? "Facility created." : "Facility updated.";

    public string Title => IsNew ? "Add New Facility" : "Editing Facility";

    public bool SpartanActive
    {
        get => Facility.SpartanActive != null && (bool)Facility.SpartanActive;
        set => Facility.SpartanActive = value;
    }

    public void CleanUpPrimitiveCollections()
    {
        if (Facility.ShowConversionCalculator)
        {
            Facility.MidWeightConversionParameters.ForEach(x =>
                                                           {
                                                               if (x.FacilityServiceId is not { Count: 0 })
                                                               {
                                                                   return;
                                                               }

                                                               x.FacilityServiceId = null;
                                                               x.FacilityServiceName = null;
                                                           });

            Facility.WeightConversionParameters.ForEach(x =>
                                                        {
                                                            if (x.FacilityServiceId is not { Count: 0 })
                                                            {
                                                                return;
                                                            }

                                                            x.FacilityServiceId = null;
                                                            x.FacilityServiceName = null;
                                                        });
        }
        else
        {
            Facility.MidWeightConversionParameters = new();
            Facility.WeightConversionParameters = new();
        }
    }
}
