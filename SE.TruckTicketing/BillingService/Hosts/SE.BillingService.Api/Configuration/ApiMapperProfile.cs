using SE.BillingService.Contracts.Api.Models;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.Shared.Domain;

using Trident.Mapper;

namespace SE.BillingService.Api.Configuration;

public class ApiMapperProfile : BaseMapperProfile
{
    public ApiMapperProfile()
    {
        //base search mappings
        this.ConfigureSearchMapping();
        this.ConfigureAllSupportedPrimitiveCollectionTypes();
        AddInvoiceExchangeMaps();

        // When mapping a collection property, if the source value is null AutoMapper will map the destination field to an empty collection rather than setting the
        // destination value to null. This aligns with the behavior of Entity Framework and Framework Design Guidelines that believe C# references, arrays, lists,
        // collections, dictionaries and IEnumerables should NEVER be null, ever.
        // This behavior can be changed by setting the AllowNullCollections property to true when configuring the mapper.
        // https://docs.automapper.org/en/stable/Lists-and-arrays.html
    }

    private void AddInvoiceExchangeMaps()
    {
        CreateEntityMap<SourceFieldDto, SourceModelFieldEntity>()
           .ReverseMap();

        CreateEntityMap<DestinationFieldDto, DestinationModelFieldEntity>()
           .ReverseMap();

        CreateEntityMap<ValueFormatDto, ValueFormatEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<InvoiceExchangeDto, InvoiceExchangeEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceExchangeDeliveryConfigurationDto, InvoiceExchangeDeliveryConfigurationEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceExchangeMessageAdapterSettingsDto, InvoiceExchangeMessageAdapterSettingsEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceExchangeTransportSettingsDto, InvoiceExchangeTransportSettingsEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceExchangeMessageFieldMappingDto, InvoiceExchangeMessageFieldMappingEntity>()
           .ReverseMap();
    }
}
