using System;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class Change : ApiModelBase<string>, IHaveCompositeKey<string>
{
    public string ReferenceEntityType { get; set; }

    public string ReferenceEntityId { get; set; }

    public string ReferenceEntityDocumentType { get; set; }

    public string FieldLocation { get; set; }

    public string FieldName { get; set; }

    public string ValueBefore { get; set; }

    public string ValueAfter { get; set; }

    public string AgnosticPath { get; set; }

    public string Tag { get; set; }

    public string ChangedBy { get; set; }

    public string ChangedById { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public Guid ChangeId { get; set; }

    public string FunctionName { get; set; }

    public Guid OperationId { get; set; }

    public string TransactionId { get; set; }

    public string CorrelationId { get; set; }

    public long? TimeToLive { get; set; }

    public string DocumentType { get; set; }

    [JsonIgnore]
    [NotMapped]
    public CompositeKey<string> Key => new(Id, DocumentType);

    public string Format()
    {
        var entityReference = $"{ReferenceEntityId} @ {ReferenceEntityDocumentType} ({ReferenceEntityType})";
        var valueChange = $"{FieldLocation}.{FieldName}: {ValueBefore} => {ValueAfter}";
        var person = $"By {ChangedBy} ({ChangedById}) @ {ChangedAt:O} ({ChangeId})";
        var operation = $"Op {FunctionName}: {OperationId}";
        var transaction = $"T: {TransactionId}";
        var correlation = $"C: {CorrelationId}";
        var ttl = $"TTL: {(TimeToLive.HasValue ? TimeSpan.FromSeconds(TimeToLive.Value) : "?")}";
        return string.Join(Environment.NewLine, entityReference, valueChange, person, operation, transaction, correlation, ttl);
    }
}
