namespace SE.TruckTicketing.Contracts.Models;

public class UserRequest<T>
{
    public T Model { get; set; }

    public string UserToken { get; set; }
}
