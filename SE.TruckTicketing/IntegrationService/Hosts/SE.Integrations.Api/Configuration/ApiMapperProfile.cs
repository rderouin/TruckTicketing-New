using System;

using AutoMapper;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain;
using SE.Shared.Domain.Configuration;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

using Trident.Contracts.Api;
using Trident.Domain;
using Trident.Mapper;

using AccountContactAddress = SE.TruckTicketing.Contracts.Models.Operations.ContactAddress;
using CustomerContactAddress = SE.Enterprise.Contracts.Models.ContactAddress;

namespace SE.Integrations.Api.Configuration;

public class ApiMapperProfile : BaseMapperProfile
{
    public ApiMapperProfile()
    {
        //base search mappings
        this.ConfigureSearchMapping();
        this.ConfigureAllSupportedPrimitiveCollectionTypes();

        CreateIntegrationAuditableEntityMap<CustomerModel, AccountEntity>()
           .ForMember(src => src.AccountAddresses, opt => opt.MapFrom(dest => dest.AccountContactAddress));

        CreateIntegrationEntityMap<FacilityModel, FacilityEntity>();

        CreateAuditableEntityMap<BillingConfiguration, BillingConfigurationEntity>()
           .ReverseMap();

        CreateIntegrationEntityMap<FacilityService, FacilityServiceEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<MaterialApproval, MaterialApprovalEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<ApplicantSignatory, ApplicantSignatoryEntity>()
           .ReverseMap();
        
        CreateOwnedEntityMap<LoadSummaryReportRecipient, LoadSummaryReportRecipientEntity>()
           .ReverseMap();

        CreateIntegrationAuditableEntityMap<SourceLocation, SourceLocationEntity>()
           .ReverseMap();

        CreateIntegrationAuditableEntityMap<SourceLocationType, SourceLocationTypeEntity>()
           .ReverseMap();

        CreateIntegrationAuditableEntityMap<SpartanProductParameter, SpartanProductParameterEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<InvoiceConfiguration, InvoiceConfigurationEntity>()
           .ReverseMap();

        CreateIntegrationEntityMap<ServiceType, ServiceTypeEntity>()
           .ReverseMap();

        CreateIntegrationAuditableEntityMap<Account, AccountEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountContact, AccountContactEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountAddress, AccountAddressEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountContactAddress, ContactAddressEntity>()
           .ReverseMap();

        CreateSimpleMap<CustomerContactAddress, ContactAddressEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountAttachment, AccountAttachmentEntity>()
           .ReverseMap();

        CreateSimpleMap<AccountContactModel, AccountContactEntity>();

        CreateSimpleMap<BlobAttachment, AccountAttachmentEntity>()!
           .ForMember(src => src.Blob, opt => opt.MapFrom(dest => dest.BlobPath));

        CreateSimpleMap<CustomerContactAddress, AccountAddressEntity>()
           .ForMember(src => src.Country, opt => opt.MapFrom(dest => dest.CountryCode));

        CreateOwnedEntityMap<AccountAddress, ContactAddressEntity>();

        CreateSimpleMap<FacilityServiceSpartanProductParameter, FacilityServiceSpartanProductParameterEntity>();

        //
        // When mapping a collection property, if the source value is null AutoMapper will map the destination field to an empty collection rather than setting the
        // destination value to null. This aligns with the behavior of Entity Framework and Framework Design Guidelines that believe C# references, arrays, lists,
        // collections, dictionaries and IEnumerables should NEVER be null, ever.
        // This behavior can be changed by setting the AllowNullCollections property to true when configuring the mapper.
        // https://docs.automapper.org/en/stable/Lists-and-arrays.html
    }

    public IMappingExpression<TSource, TDestination> CreateIntegrationEntityMap<TSource, TDestination>()
        where TSource : ApiModelBase<Guid>
        where TDestination : TTEntityBase
    {
        return CreateMap<TSource, TDestination>()
           .IgnoreTTEntityBaseMembers();
    }

    public IMappingExpression<TSource, TDestination> CreateIntegrationAuditableEntityMap<TSource, TDestination>()
        where TSource : ApiModelBase<Guid>
        where TDestination : TTAuditableEntityBase
    {
        return CreateMap<TSource, TDestination>()
              .IgnoreTTEntityBaseMembers()
              .IgnoreTTAuditableEntityBaseMembers();
    }

    public IMappingExpression<TSource, TDestination> CreateSimpleMap<TSource, TDestination>()
        where TSource : class
        where TDestination : OwnedEntityBase<Guid>
    {
        return CreateMap<TSource, TDestination>();
    }
}
