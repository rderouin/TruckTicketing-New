using System;
using System.Collections.Generic;

using Humanizer;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class AccountContactIndex : GuidApiModelBase
{
    public Guid AccountId { get; set; }

    public bool IsPrimaryAccountContact { get; set; }

    public bool IsActive { get; set; }

    public string Name { get; set; }

    public string LastName { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string JobTitle { get; set; }

    public string Contact { get; set; }

    public string Street { get; set; }

    public string City { get; set; }

    public string ZipCode { get; set; }

    public CountryCode Country { get; set; }

    public StateProvince Province { get; set; }

    public string Display => $"{Name} {LastName}";

    public List<string> ContactFunctions { get; set; } = new();

    public AccountFieldSignatoryContactType SignatoryType { get; set; }

    public string Address => String.Join(", ", Street, City, ZipCode, Province.Humanize(), Country.Humanize());
}
