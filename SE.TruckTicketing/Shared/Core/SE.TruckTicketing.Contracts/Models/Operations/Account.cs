using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using Humanizer;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class Account : GuidApiModelBase
{
    public string Name { get; set; }

    public string NickName { get; set; }

    public string Display => Name;

    public string AccountNumber { get; set; }

    public string CustomerNumber { get; set; }

    public BillingType BillingType { get; set; }

    public AccountStatus AccountStatus { get; set; }

    public WatchListStatus WatchListStatus { get; set; }

    public CreditStatus CreditStatus { get; set; }

    public double? CreditLimit { get; set; }

    public Guid? AccountPrimaryContactId => Contacts.Any(x => x.IsPrimaryAccountContact) ? Contacts.First(x => x.IsPrimaryAccountContact).Id : null;

    public string AccountPrimaryContactName => Contacts.Any(x => x.IsPrimaryAccountContact) ? Contacts.First(x => x.IsPrimaryAccountContact).Name : null;

    public string AccountPrimaryContactPhoneNumber => Contacts.Any(x => x.IsPrimaryAccountContact) ? Contacts.First(x => x.IsPrimaryAccountContact).PhoneNumber : null;

    public string AccountPrimaryContactEmail => Contacts.Any(x => x.IsPrimaryAccountContact) ? Contacts.First(x => x.IsPrimaryAccountContact).Email : null;

    public Guid LegalEntityId { get; set; }

    public string LegalEntity { get; set; }

    public List<string> AccountTypes { get; set; } = new();

    public List<AccountContact> Contacts { get; set; } = new();

    public List<AccountAddress> AccountAddresses { get; set; } = new();

    public Guid? BillingTransferRecipientId { get; set; }

    public string BillingTransferRecipientName { get; set; }

    public bool EnableNewTruckingCompany { get; set; }

    public bool EnableNewThirdPartyAnalytical { get; set; }

    public bool HasPriceBook { get; set; }

    public DateTimeOffset? LastTransactionDate { get; set; }

    public bool IsEdiFieldsEnabled { get; set; }

    public bool IsElectronicBillingEnabled { get; set; }

    public string MailingRecipientName { get; set; }

    public List<AccountAttachment> Attachments { get; set; } = new();

    public bool? EnableCreditMessagingRedFlag { get; set; }

    public string DUNSNumber { get; set; }

    public string GSTNumber { get; set; }

    public string OperatorLicenseCode { get; set; }

    public bool IncludeExternalDocumentAttachmentInLC { get; set; }

    public bool IncludeInternalDocumentAttachmentInLC { get; set; }

    public bool IsAccountActive { get; set; } = true;

    public bool? NetOff { get; set; }

    public bool? CreditApplicationReceived { get; set; }

    public bool? EnableCreditMessagingGeneral { get; set; }

    public bool IsShowAccount { get; set; }

    public bool EnableDataScavengerIntegrationFlag { get; set; }

    public bool EnablePetrotranzIntegrationFlag { get; set; }

    public string VersionTag { get; set; }
}

public class AccountContact : ApiModelBase<Guid>
{
    public string ReferenceId => Id.ToReferenceId();

    public bool IsPrimaryAccountContact { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; }

    public string LastName { get; set; }

    public string Address =>
        AccountContactAddress != null
            ? String.Join(", ", AccountContactAddress.Street, AccountContactAddress.City, AccountContactAddress.ZipCode, AccountContactAddress.Province.Humanize(),
                          AccountContactAddress.Country.Humanize())
            : string.Empty;

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string JobTitle { get; set; }

    public string Contact { get; set; }

    public ContactAddress AccountContactAddress { get; set; }

    public List<string> ContactFunctions { get; set; } = new();

    public AccountFieldSignatoryContactType SignatoryType { get; set; }

    [NotMapped]
    public string DisplayName => $"{Name} {LastName}";
}

public class AccountAddress : ApiModelBase<Guid>
{
    public string ReferenceId => Id.ToReferenceId();

    public bool IsPrimaryAddress { get; set; }

    public bool IsDeleted { get; set; }

    public AddressType AddressType { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    public CountryCode Country { get; set; }

    public StateProvince Province { get; set; }

    public string Display => $"{Street} {City}, {Province}, {ZipCode}, {Country}";
}

public class ContactAddress : ApiModelBase<Guid>
{
    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    public CountryCode Country { get; set; }

    public StateProvince Province { get; set; }
}

public class AccountAttachment : ApiModelBase<Guid>
{
    public string ContainerName { get; set; }

    public string FileName { get; set; }

    public string Blob { get; set; }
}
