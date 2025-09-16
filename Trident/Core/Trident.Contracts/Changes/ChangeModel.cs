using System;

using Newtonsoft.Json.Linq;

namespace Trident.Contracts.Changes;

public class ChangeModel
{
    public string ReferenceEntityType { get; set; }

    public string ReferenceEntityId { get; set; }

    public string ReferenceEntityDocumentType { get; set; }

    public JObject ObjectBefore { get; set; }

    public JObject ObjectAfter { get; set; }

    public string ChangedBy { get; set; }

    public string ChangedById { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public Guid ChangeId { get; set; }

    public string FunctionName { get; set; }

    public Guid OperationId { get; set; }

    public string TransactionId { get; set; }

    public string CorrelationId { get; set; }
}
