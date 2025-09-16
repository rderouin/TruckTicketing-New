using System.Collections.Generic;

namespace SE.TruckTicketing.UI.ViewModels.TruckTickets;

public record TruckTicketClassViewModel(int Id, string Value)
{
    public static readonly TruckTicketClassViewModel All = new(-1, "All");
    public static readonly TruckTicketClassViewModel Class1 = new(1, "Class 1");
    public static readonly TruckTicketClassViewModel Class2 = new(2, "Class 2");

    public static List<TruckTicketClassViewModel> GetAll()
    {
        return new() { All, Class1, Class2};
    }

    public static TruckTicketClassViewModel GetById(int id)
    {
        TruckTicketClassViewModel found = GetAll().Find(x => x.Id == id);

        return found ?? All;
    }

    public static TruckTicketClassViewModel GetByValue(string value)
    {
        TruckTicketClassViewModel found = GetAll().Find(x => x.Value.Equals(value));

        return found ?? All;
    }
}
