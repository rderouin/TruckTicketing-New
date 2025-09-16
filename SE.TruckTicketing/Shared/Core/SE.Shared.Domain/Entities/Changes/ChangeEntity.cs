using System;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Trident.Data;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Changes;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Temporal, nameof(DocumentType), Databases.DocumentTypes.Change, PartitionKeyType.Composite)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Change)]
[GenerateRepository]
[GenerateProvider]
[GenerateManager]
public class ChangeEntity : TTEntityBase<string>, IHaveCompositePartitionKey, IAmTemporal
{
    // JSON settings for the object ID generation
    private static readonly JsonSerializerSettings JsonSettings = new();

    /// <summary>
    ///     Entity Type (or Discriminator) of the entity being changed: LoadConfirmation
    /// </summary>
    public string ReferenceEntityType { get; set; }

    /// <summary>
    ///     ID of the entity being changed: e7c6531a-49f8-4a58-97b9-abaddad276d9
    /// </summary>
    public string ReferenceEntityId { get; set; }

    /// <summary>
    ///     DocumentType (partition key) of the referenced entity: LoadConfirmation|052023
    /// </summary>
    public string ReferenceEntityDocumentType { get; set; }

    /// <summary>
    ///     Where this field is located within the entire hierarchy: Signatories[0]
    /// </summary>
    public string FieldLocation { get; set; }

    /// <summary>
    ///     Name of the property that has been changed: FacilityServiceName
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    ///     Original value of the field: Landfill Disposal
    /// </summary>
    public string ValueBefore { get; set; }

    /// <summary>
    ///     New value of the field: Landfill Disposal - Drilling
    /// </summary>
    public string ValueAfter { get; set; }

    /// <summary>
    ///     A clean path to the leaf-level property: Contacts.AccountContactAddress.City
    /// </summary>
    public string AgnosticPath { get; set; }

    /// <summary>
    ///     A custom user tag to help identify records within hierarchy: C6A4
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    ///     Full user name who made the change: Jackie Hoffmann
    /// </summary>
    public string ChangedBy { get; set; }

    /// <summary>
    ///     AD ID of the person who made the change: dzhurba@secure-energy.com
    /// </summary>
    public string ChangedById { get; set; }

    /// <summary>
    ///     When the change occurred: 2023-10-25T16:38:56.5841462+00:00
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    ///     Arbitrary ID that groups all changes for the same entity.
    /// </summary>
    public Guid ChangeId { get; set; }

    /// <summary>
    ///     Name of the operation (or function name) that initiated the change: PersistTruckTicketAndSalesLines
    /// </summary>
    public string FunctionName { get; set; }

    /// <summary>
    ///     Arbitrary ID that groups all operations for a single request.
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    ///     Arbitrary ID that groups similar requests into a single transaction.
    /// </summary>
    public string TransactionId { get; set; }

    /// <summary>
    ///     Arbitrary ID that groups transactions for a single session.
    /// </summary>
    public string CorrelationId { get; set; }

    /// <summary>
    ///     Change entity lifespan in seconds.
    /// </summary>
    [JsonProperty("ttl", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public long? TimeToLive { get; set; }

    public void InitPartitionKey(string customPartitionKey = null)
    {
        DocumentType ??= customPartitionKey ?? $"{Databases.DocumentTypes.Change}|{ReferenceEntityType}|{ReferenceEntityId}";
    }

    public void InitPrimaryKey()
    {
        // the ID will be generated based on values in these fields
        var hashedFields = new object[]
        {
            ReferenceEntityType,
            ReferenceEntityId,
            ReferenceEntityDocumentType,
            FieldLocation,
            FieldName,
            ValueBefore,
            ValueAfter,
            ChangedById,
            ChangedAt,
            ChangeId,
            OperationId,
            TransactionId,
            CorrelationId,
        };

        // use SHA256 to generate the ID
        using var sha256 = SHA256.Create();
        Id = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(hashedFields, JsonSettings))));
        _id = $"{EntityType}|{Id}";
    }

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

    public void InitAgnosticPath()
    {
        AgnosticPath = this.GetAgnosticPath();
    }
}
