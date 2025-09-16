using System;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models;

public class LegalEntity : GuidApiModelBase
{
    public Guid? BusinessStreamId { get; set; }

    public string Name { get; set; }

    public string Code { get; set; }

    public CountryCode CountryCode { get; set; }

    public string Display => Code;

    public int CreditExpirationThreshold { get; set; }

    public bool IsCustomerPrimaryContactRequired { get; set; }

    public bool? ShowAccountsInTruckTicketing { get; set; }
}
