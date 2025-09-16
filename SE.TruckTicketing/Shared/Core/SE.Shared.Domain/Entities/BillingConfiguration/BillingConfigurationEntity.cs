using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.BillingConfiguration;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Billing, nameof(DocumentType), Databases.DocumentTypes.BillingConfiguration, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.BillingConfiguration)]
[GenerateManager]
[GenerateProvider]
public class BillingConfigurationEntity : TTAuditableEntityBase
{
    public Guid InvoiceConfigurationId { get; set; }

    public bool IncludeSaleTaxInformation { get; set; }

    public string Name { get; set; }

    public bool BillingConfigurationEnabled { get; set; }

    //Dates
    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public Guid CustomerGeneratorId { get; set; }

    public string CustomerGeneratorName { get; set; }

    public Guid BillingCustomerAccountId { get; set; }

    public Guid? ThirdPartyCompanyId { get; set; }

    public string ThirdPartyCompanyName { get; set; }

    public Guid? GeneratorRepresentativeId { get; set; }

    public Guid? BillingContactId { get; set; }

    public string BillingContactName { get; set; }

    public string BillingContactAddress { get; set; }

    public Guid? ThirdPartyBillingContactId { get; set; }

    public string ThirdPartyBillingContactName { get; set; }

    public string ThirdPartyBillingContactAddress { get; set; }

    [OwnedHierarchy]
    public List<EDIFieldValueEntity> EDIValueData { get; set; } = new();

    public List<MatchPredicateEntity> MatchCriteria { get; set; } = new();

    public bool EmailDeliveryEnabled { get; set; }

    [OwnedHierarchy]
    public List<EmailDeliveryContactEntity> EmailDeliveryContacts { get; set; } = new();

    [OwnedHierarchy]
    public List<SignatoryContactEntity> Signatories { get; set; } = new();

    public bool IsSignatureRequired { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<Guid> Facilities { get; set; }

    public bool LoadConfirmationsEnabled { get; set; }

    public LoadConfirmationFrequency? LoadConfirmationFrequency { get; set; }

    public bool IncludeExternalAttachmentInLC { get; set; }

    public bool IncludeInternalAttachmentInLC { get; set; }

    public bool FieldTicketsUploadEnabled { get; set; }

    public string InvoiceExchange { get; set; }

    public bool IsDefaultConfiguration { get; set; }

    public string LastComment { get; set; }

    public string Description { get; set; }

    public string RigNumber { get; set; }

    public bool IncludeForAutomation { get; set; }

    public bool IsValid { get; set; } = true;

    public bool IsEdiValid { get; set; } = true;

    public string BillingCustomerName { get; set; }

    public DayOfWeek? FirstDayOfTheWeek { get; set; } = DayOfWeek.Sunday;

    public int? FirstDayOfTheMonth { get; set; } = 1;

    public FieldTicketDeliveryMethod? FieldTicketDeliveryMethod { get; set; } = TruckTicketing.Contracts.Lookups.FieldTicketDeliveryMethod.LoadConfirmationBatch;

    public string GetSignatoryEmails()
    {
        return string.Join("; ", Signatories.Where(s => s.Email.HasText() && s.IsAuthorized).Select(s => s.Email));
    }

    public string GetEmailContacts()
    {
        return string.Join("; ", EmailDeliveryContacts.Where(s => s.EmailAddress.HasText() && s.IsAuthorized).Select(s => s.EmailAddress));
    }
}

public class EmailDeliveryContactEntity : OwnedEntityBase<Guid>
{
    public Guid AccountContactId { get; set; }

    public bool IsAuthorized { get; set; }

    public string SignatoryContact { get; set; }

    public string EmailAddress { get; set; }
}

public class SignatoryContactEntity : OwnedEntityBase<Guid>
{
    public bool IsAuthorized { get; set; }

    public Guid AccountId { get; set; }

    public Guid AccountContactId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }
}

public class MatchPredicateEntity : OwnedEntityBase<Guid>
{
    public bool IsEnabled { get; set; } = true;

    //Dates
    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public MatchPredicateValueState WellClassificationState { get; set; }

    public WellClassifications WellClassification { get; set; }

    public MatchPredicateValueState SourceLocationValueState { get; set; }

    public Guid? SourceLocationId { get; set; }

    public string SourceIdentifier { get; set; }

    public MatchPredicateValueState SubstanceValueState { get; set; }

    public Guid? SubstanceId { get; set; }

    public string SubstanceName { get; set; }

    public MatchPredicateValueState StreamValueState { get; set; }

    public Stream Stream { get; set; }

    public MatchPredicateValueState ServiceTypeValueState { get; set; }

    public Guid? ServiceTypeId { get; set; }

    public string ServiceType { get; set; }

    public string Hash { get; set; }

    public void ComputeHash()
    {
        var matchPredicate = new MatchPredicateEntity
        {
            WellClassificationState = WellClassificationState,
            WellClassification = WellClassification,
            SourceLocationValueState = SourceLocationValueState,
            SourceLocationId = SourceLocationId,
            ServiceTypeValueState = ServiceTypeValueState,
            ServiceTypeId = ServiceTypeId,
            StreamValueState = StreamValueState,
            Stream = Stream,
            SubstanceValueState = SubstanceValueState,
            SubstanceId = SubstanceId,
        };

        using var sHa256 = SHA256.Create();
        Hash = Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(matchPredicate))));
    }
}
