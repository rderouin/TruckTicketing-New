using SE.TridentContrib.Extensions.Azure.ServiceBus;

namespace SE.Shared.Domain.Infrastructure;

public class IntegrationsServiceBus : ServiceBus, IIntegrationsServiceBus
{
    public IntegrationsServiceBus(string connectionString) : base(connectionString)
    {
    }
}

public interface IIntegrationsServiceBus : IServiceBus
{   
}
