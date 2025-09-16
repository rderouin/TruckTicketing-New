using System.Collections.Generic;

namespace SE.TruckTicketing.UI.ViewModels.TruckTickets;

public record TruckTicketStatusViewModel
{
    public int Id { get; set; }
    public string Value { get; set; }

    public static readonly TruckTicketStatusViewModel NotFound = new(-1, "Not Found");
    public static readonly TruckTicketStatusViewModel Open = new(1, "Open");
    public static readonly TruckTicketStatusViewModel Approved = new(2, "Approved");
    public static readonly TruckTicketStatusViewModel Void = new(3, "Void");
    public static readonly TruckTicketStatusViewModel Hold = new(4, "Hold");
    public static readonly TruckTicketStatusViewModel Invoiced = new(5, "Invoiced");
    public static readonly TruckTicketStatusViewModel Stub = new(6, "Stub");
    
    private TruckTicketStatusViewModel(int id, string value)
    {
        Id = id;
        Value = value;
    }

    public static List<TruckTicketStatusViewModel> GetAll()
    {
        return new() { Open, Approved, Hold, Invoiced };
    }

    public static TruckTicketStatusViewModel GetById(int id)
    {
        TruckTicketStatusViewModel found = GetAll().Find(x => x.Id == id);

        return found ?? NotFound;
    }

    public static TruckTicketStatusViewModel GetByValue(string value)
    {
        TruckTicketStatusViewModel found = GetAll().Find(x => x.Value.Equals(value));

        return found ?? NotFound;
    }
}
