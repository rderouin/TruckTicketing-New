using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Api.Functions;

public sealed class SalesOrderEntityReceiverFunctions
{
    private static ILog _log;

    private readonly IAppSettings _appSettings;

    private readonly IHttpClientFactory _clientFactory;

    private readonly IMapperRegistry _mapper;

    public SalesOrderEntityReceiverFunctions(ILog log,
                                             IMapperRegistry mapper,
                                             IAppSettings appSettings,
                                             IHttpClientFactory clientFactory)
    {
        _log = log;
        _mapper = mapper;
        _appSettings = appSettings;
        _clientFactory = clientFactory;
    }

    [Function("SalesOrderEntityReceiverFunctions")]
    public async Task Run([ServiceBusTrigger(ServiceBusConstants.Topics.EntityUpdates,
                                             ServiceBusConstants.Subscriptions.Sales,
                                             Connection = ServiceBusConstants.PrivateServiceBusNamespace, IsSessionsEnabled = true)]
                          string message,
                          FunctionContext context)
    {
        var messageId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageId);
        var messageType = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.EntityUpdate.MessageType);
        var correlationId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.CorrelationId);
        var sessionId = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.SessionId);
        var enqueuedSequenceNumber = context.BindingContext.BindingData.GetValueOrDefault(MessageConstants.SequenceNumber);

        try
        {
            var httpEndPoint = _appSettings.GetKeyOrDefault(ServiceBusConstants.HttpEndPoints.FOSalesOrderLogicAppHttpEndPoint.Trim('%'));

            var request = new HttpRequestMessage(HttpMethod.Post, httpEndPoint);

            request.Headers.Add("MessageType", messageType.ToString());
            request.Headers.Add("X-Session-Id", sessionId.ToString());
            request.Headers.Add("X-Enqueued-Sequence-Number", enqueuedSequenceNumber.ToString());
            request.Headers.Accept.Add(new("application/json"));

            request.Content = new StringContent(message);
            request.Content.Headers.ContentType = new("application/json");

            var response = await _clientFactory.CreateClient("client").SendAsync(request);

            response.EnsureSuccessStatusCode();

            await Task.CompletedTask;
        }
        catch (Exception e)
        {
            _log.Error(exception: e, messageTemplate: $"Unable to process a SalesOrder message. (msgId: {GetLogMessageContext()})");
            throw;
        }

        string GetLogMessageContext()
        {
            return JsonConvert.SerializeObject(new
            {
                messageId,
                messageType,
                correlationId,
                sessionId,
                enqueuedSequenceNumber,
            });
        }
    }
}
