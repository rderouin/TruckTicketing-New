using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using SE.Enterprise.Contracts.Constants;

using Trident.IoC;
using Trident.Logging;

namespace SE.TruckTicketing.Api.Functions.Processors;

public class EntityFunctions : ServiceBusProcessorFunctionBase
{
    public EntityFunctions(ILog log, IIoCServiceLocator serviceLocator) : base(log, serviceLocator)
    {
    }

    [Function(nameof(ProcessEntityUpdate))]
    public async Task ProcessEntityUpdate([ServiceBusTrigger(ServiceBusConstants.Topics.EntityUpdates,
                                                             ServiceBusConstants.Subscriptions.TruckTicketing,
                                                             Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                          string message,
                                          FunctionContext context)
    {
        await ProcessMessage(context, message);
    }
}
