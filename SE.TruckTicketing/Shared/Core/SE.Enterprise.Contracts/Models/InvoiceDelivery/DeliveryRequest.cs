using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SE.Enterprise.Contracts.Models.InvoiceDelivery;

public class DeliveryRequest : EntityEnvelopeModel<JObject>
{
    [JsonIgnore]
    public string Platform => TryGetJsonObjectValue<string>(Payload, "Platform");

    [JsonIgnore]
    public Guid CustomerId => TryGetJsonObjectValue<Guid>(Payload, "BillingCustomerId");

    [JsonIgnore]
    public string InvoiceId => TryGetJsonObjectValue<string>(Payload, "InvoiceId");

    private static T TryGetJsonObjectValue<T>(JObject jObject, string propertyName)
    {
        return jObject?.TryGetValue(propertyName, out var jToken) == true ? jToken.ToObject<T>() : default;
    }

    public MessageType? GetMessageType()
    {
        return Enum.TryParse<MessageType>(MessageType, true, out var requestType) ? requestType : default(MessageType?);
    }

    public virtual DeliveryResponse CreateResponse()
    {
        // response message type
        var messageType = GetMessageType() switch
                          {
                              InvoiceDelivery.MessageType.InvoiceRequest => InvoiceDelivery.MessageType.InvoiceResponse,
                              InvoiceDelivery.MessageType.FieldTicketRequest => InvoiceDelivery.MessageType.FieldTicketResponse,
                              _ => throw new ArgumentOutOfRangeException(),
                          };

        // response message
        return new()
        {
            SourceId = SourceId,
            CorrelationId = CorrelationId,
            Operation = Operation,
            Source = "BS",
            MessageType = messageType.ToString(),
            MessageDate = DateTime.UtcNow,
        };
    }
}
