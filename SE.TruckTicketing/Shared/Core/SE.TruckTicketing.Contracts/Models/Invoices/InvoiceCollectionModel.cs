using System;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Invoices;

public class InvoiceCollectionModel
{
    public CompositeKey<Guid> InvoiceKey { get; set; }

    public InvoiceCollectionOwners CollectionOwner { get; set; }

    public InvoiceCollectionReason CollectionReason { get; set; }

    public string CollectionNotes { get; set; }
}
