using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using SE.Enterprise.Contracts.Constants;
using SE.TruckTicketing.Api.Functions.Processors;

using Trident.IoC;
using Trident.Logging;

namespace SE.TruckTicketing.Api.Functions.SpartanData;

public class SpartanIntegrationFunctions : ServiceBusProcessorFunctionBase
{
    public SpartanIntegrationFunctions(ILog log, IIoCServiceLocator serviceLocator) : base(log, serviceLocator)
    {
    }

    [Function(nameof(ProcessSpartanTicket))]
    public async Task ProcessSpartanTicket([ServiceBusTrigger(ServiceBusConstants.Topics.SpartanTickets,
                                                              ServiceBusConstants.Subscriptions.SpartanTickets,
                                                              Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                           string message,
                                           FunctionContext context)
    {
        await ProcessMessage(context, message);
    }
}
