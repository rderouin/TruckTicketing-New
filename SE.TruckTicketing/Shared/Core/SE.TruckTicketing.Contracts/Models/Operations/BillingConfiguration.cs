using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class BillingConfiguration : GuidApiModelBase
{
    public bool IsValid { get; set; } = true;

    public Guid InvoiceConfigurationId { get; set; }

    public bool IncludeSaleTaxInformation { get; set; }

    public string Name { get; set; }

    public bool BillingConfigurationEnabled { get; set; } = true;

    public FieldTicketDeliveryMethod? FieldTicketDeliveryMethod { get; set; } = Lookups.FieldTicketDeliveryMethod.LoadConfirmationBatch;

    public DayOfWeek? FirstDayOfTheWeek { get; set; } = DayOfWeek.Sunday;

    public int? FirstDayOfTheMonth { get; set; } = 1;

    public bool IsSignatureRequired { get; set; }

    //Dates
    public DateTimeOffset? StartDate { get; set; } = GetDefaultStartDate();

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

    public List<EDIFieldValue> EDIValueData { get; set; } = new();

    public List<MatchPredicate> MatchCriteria { get; set; } = new();

    public bool EmailDeliveryEnabled { get; set; } = true;

    public List<EmailDeliveryContact> EmailDeliveryContacts { get; set; } = new();

    public List<SignatoryContact> Signatories { get; set; } = new();

    public bool LoadConfirmationsEnabled { get; set; } = true;

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

    public List<Guid> Facilities { get; set; }

    public bool IsEdiValid { get; set; } = true;

    public string CreatedBy { get; set; }

    public string CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public string UpdatedById { get; set; }

    public string BillingCustomerName { get; set; }

    private static DateTimeOffset GetDefaultStartDate()
    {
        var now = DateTimeOffset.Now;
        return new(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
    }
}

public class EmailDeliveryContact : GuidApiModelBase
{
    public Guid AccountContactId { get; set; }

    public string ReferenceId => AccountContactId.ToReferenceId();

    public bool IsAuthorized { get; set; }

    public string SignatoryContact { get; set; }

    public string EmailAddress { get; set; }
}

public class SignatoryContact : GuidApiModelBase
{
    public bool IsAuthorized { get; set; }

    public Guid AccountContactId { get; set; }

    public string ReferenceId => AccountContactId.ToReferenceId();

    public Guid AccountId { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }

    [NotMapped]
    public string DisplayName => $"{FirstName} {LastName}";
}

public class MatchPredicate : GuidApiModelBase
{
    public bool IsEnabled { get; set; } = true;

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
        var matchPredicate = new MatchPredicate
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
