using System;
using System.Collections.Generic;
using System.Linq;

using Humanizer;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.Extensions;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Account;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.Account, PartitionKeyType.WellKnown)]
[Discriminator(nameof(EntityType), Databases.Discriminators.Account)]
[GenerateManager]
[GenerateProvider]
public class AccountEntity : TTAuditableEntityBase, ISupportOptimisticConcurrentUpdates
{
    public string Name { get; set; }

    public string NickName { get; set; }

    public string AccountNumber { get; set; }

    public string CustomerNumber { get; set; }

    public BillingType BillingType { get; set; }

    public AccountStatus AccountStatus { get; set; }

    public WatchListStatus WatchListStatus { get; set; }

    public CreditStatus CreditStatus { get; set; }

    public double? CreditLimit { get; set; }

    public Guid? AccountPrimaryContactId { get; set; }

    public string AccountPrimaryContactName { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntity { get; set; }

    public string AccountPrimaryContactPhoneNumber { get; set; }

    public string AccountPrimaryContactEmail { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> AccountTypes { get; set; } = new();

    [OwnedHierarchy]
    public List<AccountContactEntity> Contacts { get; set; } = new();

    public List<AccountAddressEntity> AccountAddresses { get; set; } = new();

    public Guid? BillingTransferRecipientId { get; set; }

    public string BillingTransferRecipientName { get; set; }

    public bool EnableNewTruckingCompany { get; set; }

    public bool EnableNewThirdPartyAnalytical { get; set; }

    public bool HasPriceBook { get; set; }

    public bool IsEdiFieldsEnabled { get; set; }

    public bool IsElectronicBillingEnabled { get; set; }

    public string MailingRecipientName { get; set; }

    public bool? IsBlocked { get; set; }

    public string Email { get; set; }

    public List<AccountAttachmentEntity> Attachments { get; set; } = new();

    public DateTimeOffset? LastTransactionDate { get; set; }

    public DateTime? LastIntegrationDateTime { get; set; }

    public bool? EnableCreditMessagingRedFlag { get; set; }

    public string DUNSNumber { get; set; }

    public string GSTNumber { get; set; }

    public string OperatorLicenseCode { get; set; }

    public bool IncludeExternalDocumentAttachmentInLC { get; set; }

    public bool IncludeInternalDocumentAttachmentInLC { get; set; }

    public CurrencyCode? CurrencyCode { get; set; }

    public bool IsAccountActive { get; set; } = true;

    public bool? NetOff { get; set; }

    public bool? CreditApplicationReceived { get; set; }

    public bool? EnableCreditMessagingGeneral { get; set; }

    public bool? IsShowAccount { get; set; }

    public bool? EnableDataScavengerIntegrationFlag { get; set; }

    public bool? EnablePetrotranzIntegrationFlag { get; set; }

    public string PriceGroup { get; set; }

    public string TmaGroup { get; set; }

    public string VersionTag { get; set; }

    public IEnumerable<object> GetFieldsToCompare()
    {
        return new object[]
        {
            Name,
            AccountTypes.Raw,
            AccountAddresses?.ToJson(),
            CustomerNumber,
            WatchListStatus,
            CreditStatus,
            CreditLimit,
            CurrencyCode,
            DUNSNumber,
            GSTNumber,
            BillingTransferRecipientId,
            BillingTransferRecipientName,
            BillingType,
            EnableNewTruckingCompany,
            HasPriceBook,
            EnableNewThirdPartyAnalytical,
            IncludeInternalDocumentAttachmentInLC,
            IncludeExternalDocumentAttachmentInLC,
            EnableDataScavengerIntegrationFlag,
            EnableCreditMessagingGeneral,
            EnableCreditMessagingRedFlag,
        };
    }

    public AccountAddressEntity GetPrimaryAddress()
    {
        return AccountAddresses?.FirstOrDefault(a => a.IsPrimaryAddress);
    }
}

public class AccountContactEntity : OwnedEntityBase<Guid>
{
    public bool IsPrimaryAccountContact { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string JobTitle { get; set; }

    public string Contact { get; set; }

    public ContactAddressEntity AccountContactAddress { get; set; }

    [OwnedHierarchy]
    public PrimitiveCollection<string> ContactFunctions { get; set; } = new();

    public AccountFieldSignatoryContactType SignatoryType { get; set; }

    public string GetFullName()
    {
        return $"{Name} {LastName}";
    }

    public string GetFullAddress(bool oneLine)
    {
        return AccountContactAddress?.GetFullAddress(oneLine);
    }
}

public class AccountAddressEntity : OwnedEntityBase<Guid>
{
    public bool IsPrimaryAddress { get; set; }

    public bool IsDeleted { get; set; }

    public AddressType AddressType { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    public CountryCode Country { get; set; }

    public StateProvince Province { get; set; }

    public string Format()
    {
        var line1 = $"{Street}";
        var line2 = $"{City}, {Province}, {ZipCode}";
        var line3 = $"{Country.Humanize()}";
        return string.Join(Environment.NewLine, line1, line2, line3);
    }
}

public class ContactAddressEntity : OwnedEntityBase<Guid>
{
    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    public CountryCode Country { get; set; }

    public StateProvince Province { get; set; }

    public string GetFullAddress(bool oneLine)
    {
        if (oneLine)
        {
            return string.Join(", ", Street, City, ZipCode, Province.Humanize(), Country.Humanize());
        }

        var line1 = $"{Street}";
        var line2 = $"{City}, {Province}, {ZipCode}";
        var line3 = $"{Country.Humanize()}";
        return string.Join(Environment.NewLine, line1, line2, line3);
    }
}

public class AccountAttachmentEntity : OwnedEntityBase<Guid>
{
    public string ContainerName { get; set; }

    public string FileName { get; set; }

    public string Blob { get; set; }
}
