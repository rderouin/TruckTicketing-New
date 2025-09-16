using System;

using Humanizer;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data;
using Trident.Domain;
using Trident.SourceGeneration.Attributes;

namespace SE.Shared.Domain.Entities.Account;

[UseSharedDataSource(Databases.SecureEnergyDB)]
[Container(Databases.Containers.Operations, nameof(DocumentType), Databases.DocumentTypes.AccountContact, PartitionKeyType.WellKnown)]
[Discriminator(Databases.Discriminators.AccountContactIndex, Property = nameof(EntityType))]
[GenerateManager]
[GenerateProvider]
public class AccountContactIndexEntity : TTEntityBase
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

    [OwnedHierarchy]
    public PrimitiveCollection<string> ContactFunctions { get; set; } = new();

    public AccountFieldSignatoryContactType SignatoryType { get; set; }

    public string GetFullName()
    {
        return $"{Name} {LastName}";
    }

    public string GetFullAddress()
    {
        return String.Join(", ", Street, City, ZipCode, Province.Humanize(), Country.Humanize());
    }
}
