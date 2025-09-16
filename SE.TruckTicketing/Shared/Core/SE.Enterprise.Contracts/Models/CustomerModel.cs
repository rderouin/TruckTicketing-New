using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using SE.Shared.Common.JsonConverters;
using SE.Shared.Common.Lookups;

using Trident.Contracts.Api;

namespace SE.Enterprise.Contracts.Models;

public class CustomerModel : ApiModelBase<Guid>
{
    [JsonProperty("AccountNum")]
    public string AccountNumber { get; set; }

    public string CustomerNumber { get; set; }

    public string Name { get; set; }

    public string PriceGroup { get; set; }

    [JsonProperty("CustomerTmaGroup")]
    public string TmaGroup { get; set; }

    public string DataAreaId { get; set; }

    [JsonConverter(typeof(YesNoBooleanJsonConverter))]
    public bool? IsBlocked { get; set; }

    [JsonProperty("Email")]
    public string Email { get; set; }

    public WatchListStatus WatchListStatus { get; set; }

    public BillingType BillingType { get; set; }

    public AccountStatus AccountStatus { get; set; }

    public CreditStatus CreditStatus { get; set; }

    public Guid? BillingTransferRecipientId { get; set; }

    public string BillingTransferRecipientName { get; set; }

    public string DUNSNumber { get; set; }

    public string GSTNumber { get; set; }

    public string OperatorLicenseCode { get; set; }

    public string MailingRecipientName { get; set; }

    public bool HasPriceBook { get; set; }

    public bool IsEdiFieldsEnabled { get; set; }

    public bool IsElectronicBillingEnabled { get; set; }

    [JsonProperty("Blobs")]
    public List<BlobAttachment> Attachments { get; set; }

    [JsonProperty("Addresses")]
    public List<ContactAddress> AccountContactAddress { get; set; }

    [JsonProperty("IsRedFlagged")]
    public bool IsRedFlagged { get; set; }

    [JsonProperty("EnableCreditMessagingRedFlag")]
    public bool? EnableCreditMessagingRedFlag { get; set; }

    public bool? NetOff { get; set; }

    public bool? CreditApplicationReceived { get; set; }

    public bool? EnableCreditMessagingGeneral { get; set; }

    public double? CreditLimit { get; set; }
}

public class ContactAddress
{
    public string AddressType { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    [JsonProperty("Country")]
    public string CountryCode { get; set; }

    [JsonProperty("IsPrimary")]
    public bool IsPrimaryAddress { get; set; }

    public string Province { get; set; }
}
