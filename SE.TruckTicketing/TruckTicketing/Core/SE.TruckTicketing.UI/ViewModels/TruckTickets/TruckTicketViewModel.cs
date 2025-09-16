using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels.TruckTickets;

public class TruckTicketViewModel
{
    public TruckTicketViewModel(TruckTicket truckTicket)
    {
        TruckTicket = truckTicket;
        Title = "Edit Ticket";
        ValidationStatus = truckTicket.ValidationStatus.ToString();
        IsAcknowledged = !string.IsNullOrEmpty(truckTicket.Acknowledgement);
    }

    public TruckTicket TruckTicket { get; }

    public string Title { get; }

    public string ValidationStatus { get; set; }

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitSuccessNotificationMessage => "Truck Ticket updated.";

    public string DiscardButtonNotificationMessage => "Changes discarded successfully.";

    public string SubmitButtonBusyText => "Saving";

    public string SubmitButtonIcon => "save";

    public bool IsAcknowledged { get; set; }

    public string LastComment { get; set; }
}
