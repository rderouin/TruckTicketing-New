namespace SE.Shared.Domain.Entities;

public interface IAmTemporal
{
    long? TimeToLive { get; set; }
}
