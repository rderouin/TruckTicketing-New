using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Integrations.Domain.Processors;

using Trident.Extensions;
using Trident.IoC;
using Trident.Logging;

namespace SE.Integrations.Api.Functions;

public sealed class EntityFunctions
{
    private readonly ILog _log;

    private readonly IIoCServiceLocator _serviceLocator;

    public EntityFunctions(ILog log, IIoCServiceLocator serviceLocator)
    {
        _log = log;
        _serviceLocator = serviceLocator;
    }

    [Function(nameof(ProcessEntityUpdate))]
    public async Task ProcessEntityUpdate([ServiceBusTrigger(ServiceBusConstants.Topics.EntityUpdates,
                                                             ServiceBusConstants.Subscriptions.TruckTicketing,
                                                             Connection = ServiceBusConstants.PrivateServiceBusNamespace)]
                                          string message,
                                          FunctionContext context)
    {
        var startTime = DateTimeOffset.UtcNow;

        // message info
        var messageId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageId);
        var messageType = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageType);
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        var sourceId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.SourceId);

        // general null-check
        if (string.IsNullOrEmpty(message))
        {
            var logMessage = GetLogMessageContext("Message is blank.");
            _log.Warning(messageTemplate: logMessage);
            throw new ArgumentException(logMessage);
        }

        // determine the message/entity type
        var messageTypeValue = messageType as string;
        if (string.IsNullOrWhiteSpace(messageTypeValue))
        {
            var logMessage = GetLogMessageContext("Message Type is blank.");
            _log.Warning(messageTemplate: logMessage);
            throw new ArgumentException(logMessage);
        }

        try
        {
            // log all incoming messages
            LogProcessEntityEvent("Processing");

            // get the required processor
            var entityProcessor = _serviceLocator.GetOptionalNamed<IEntityProcessor>(messageTypeValue);
            if (entityProcessor == null)
            {
                var logMessage = GetLogMessageContext($"Entity Processor is not defined for the message type '{messageTypeValue}'.");
                _log.Warning(messageTemplate: logMessage);
                LogProcessEntityEvent("NoEntityProcessor");
                throw new ArgumentException(logMessage);
            }

            // pass down the message to the processor
            await entityProcessor.Process(message);
            LogProcessEntityEvent("Successful");
        }
        catch (Exception e)
        {
            _log.Error(exception: e, messageTemplate: GetLogMessageContext("Unable to process a message."));
            LogProcessEntityEvent("Failed");
            throw;
        }

        string GetLogMessageContext(string customMessage)
        {
            return JsonConvert.SerializeObject(new
            {
                customMessage,
                sourceId,
                messageId,
                messageType,
                correlationId,
            });
        }

        void LogProcessEntityEvent(string status)
        {
            var elapsed = DateTimeOffset.UtcNow - startTime;
            _log.Information(messageTemplate: new Dictionary<string, string>
            {
                ["EventName"] = nameof(ProcessEntityUpdate),
                [nameof(MessageConstants.EntityUpdate.MessageType)] = messageTypeValue,
                [nameof(elapsed.Duration)] = elapsed.ToString(),
                ["Status"] = status,
            }.ToJson());
        }
    }
}
