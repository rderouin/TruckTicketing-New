using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Changes;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.EDIFieldValue;
using SE.Shared.Domain.Entities.EDIValidationPatternLookup;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.Substance;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.Substances;

using Trident.Mapper;

namespace SE.Shared.Domain;

public class ApiMapperProfile : BaseMapperProfile
{
    public ApiMapperProfile()
    {
        //base search mappings
        this.ConfigureSearchMapping();
        this.ConfigureAllSupportedPrimitiveCollectionTypes();

        //BillingConfiguration
        CreateAuditableEntityMap<BillingConfiguration, BillingConfigurationEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<EmailDeliveryContact, EmailDeliveryContactEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<MatchPredicate, MatchPredicateEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<EDIFieldValue, EDIFieldValueEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<EDIFieldDefinition, EDIFieldDefinitionEntity>()
           .ReverseMap();

        CreateEntityMap<EDIValidationPatternLookup, EDIValidationPatternLookupEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<SignatoryContact, SignatoryContactEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<InvoiceConfiguration, InvoiceConfigurationEntity>()
           .ReverseMap();

        CreateEntityMap<Substance, SubstanceEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceConfigurationPermutations, InvoiceConfigurationPermutationsEntity>()
           .ReverseMap();

        CreateEntityMap<InvoiceConfigurationPermutationsIndex, InvoiceConfigurationPermutationsIndexEntity>()
           .ReverseMap();

        CreateMap<Change, ChangeEntity>()
           .ForMember(entity => entity.DocumentType, options => options.Ignore())
           .ForMember(entity => entity.EntityType, options => options.Ignore())
           .ReverseMap();

        // When mapping a collection property, if the source value is null AutoMapper will map the destination field to an empty collection rather than setting the
        // destination value to null. This aligns with the behavior of Entity Framework and Framework Design Guidelines that believe C# references, arrays, lists,
        // collections, dictionaries and IEnumerables should NEVER be null, ever.
        // This behavior can be changed by setting the AllowNullCollections property to true when configuring the mapper.
        // https://docs.automapper.org/en/stable/Lists-and-arrays.html
    }
}
