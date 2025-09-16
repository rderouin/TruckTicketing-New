using SE.Shared.Domain;
using SE.Shared.Domain.BusinessStream;
using SE.Shared.Domain.EmailTemplates;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Acknowledgement;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.EDIFieldLookup;
using SE.Shared.Domain.Entities.EntityStatus;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.Permission;
using SE.Shared.Domain.Entities.Role;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.SourceLocationType;
using SE.Shared.Domain.Entities.TicketType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Entities.VolumeChange;
using SE.Shared.Domain.LegalEntity;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Acknowledgement;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Models.FacilityServices;
using SE.TruckTicketing.Contracts.Models.Invoices;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Navigation;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.Sampling;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Statuses;
using SE.TruckTicketing.Domain.Entities.Configuration;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.Reports;
using SE.TruckTicketing.Domain.Entities.Sampling;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.TradeAgreementUploads;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Mapper;

namespace SE.TruckTicketing.Api.Configuration;

public class ApiMapperProfile : BaseMapperProfile
{
    public ApiMapperProfile()
    {
        this.ConfigureSearchMapping();
        this.ConfigureAllSupportedPrimitiveCollectionTypes();

        CreateAuditableEntityMap<UserProfile, UserProfileEntity>()
           .ReverseMap();

        CreateEntityMap<FacilityService, FacilityServiceEntity>()
           .ReverseMap();

        CreateEntityMap<Role, RoleEntity>()
           .ReverseMap();

        CreateOwnedLookupEntityMap<Permission, PermissionLookupEntity>()
           .ForMember(x => x.AssignedOperations, cfg => cfg.MapFrom(x => x.AllowedOperations))
           .ReverseMap();

        CreateBasicMap<Permission, Permission>()
           .ReverseMap();

        CreateEntityMap<Permission, PermissionEntity>()
           .ReverseMap();

        CreateOwnedLookupEntityMap<PermissionLookup, PermissionLookupEntity>()
           .ForMember(x => x.AssignedOperations, cfg => cfg.MapFrom(x => x.AllowedOperations))
           .ReverseMap();

        CreateOwnedLookupEntityMap<Operation, OperationEntity>()
           .ReverseMap();

        CreateEntityMap<Facility, FacilityEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<PreSetDensityConversionParams, PreSetDensityConversionParamsEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<TruckTicket, TruckTicketEntity>()
           .ReverseMap();

        CreateEntityMap<ServiceType, ServiceTypeEntity>()
           .ReverseMap();

        CreateEntityMap<Product, ProductEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<ProductSubstance, ProductSubstanceEntity>()
           .ReverseMap();

        CreateEntityMap<BusinessStream, BusinessStreamEntity>()
           .ReverseMap();

        CreateEntityMap<LegalEntity, LegalEntityEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<UserProfile, UserProfileEntity>()
           .ReverseMap();

        CreateBasicMap<UserProfileRole, UserProfileRoleEntity>()
           .ReverseMap();

        CreateOwnedLookupEntityMap<UserProfileFacilityAccess, UserProfileFacilityAccessEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<Note, NoteEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<TruckTicketAdditionalService, TruckTicketAdditionalServiceEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<Signatory, SignatoryEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<BillingContact, BillingContactEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<TruckTicketAttachment, TruckTicketAttachmentEntity>()
           .ReverseMap();

        CreateBasicMap<TruckTicketDensityConversionParams, TruckTicketDensityConversionParamsEntity>()
           .ReverseMap();

        CreateEntityMap<NavigationModel, NavigationConfigurationEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<NavigationItemModel, NavigationItemEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<SourceLocationType, SourceLocationTypeEntity>()
           .ReverseMap();

        CreateBasicMap<SourceLocationOwnerHistory, SourceLocationOwnerHistoryEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<SourceLocation, SourceLocationEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<Account, AccountEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountContact, AccountContactEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountAddress, AccountAddressEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<ContactAddress, ContactAddressEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AccountAttachment, AccountAttachmentEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<SpartanProductParameter, SpartanProductParameterEntity>()
           .ReverseMap();

        CreateBasicMap<FacilityServiceSpartanProductParameter, FacilityServiceSpartanProductParameterEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<MaterialApproval, MaterialApprovalEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<ApplicantSignatory, ApplicantSignatoryEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<LoadSummaryReportRecipient, LoadSummaryReportRecipientEntity>()
           .ReverseMap();

        CreateEntityMap<FacilityServiceSubstanceIndex, FacilityServiceSubstanceIndexEntity>()
           .ReverseMap();

        CreateEntityMap<TruckTicketWellClassification, TruckTicketWellClassificationUsageEntity>()
           .ReverseMap();

        CreateBasicMap<TruckTicketTareWeightCsvResult, TruckTicketTareWeightCsvResponse>()
           .ReverseMap();

        CreateAuditableEntityMap<TruckTicketTareWeight, TruckTicketTareWeightEntity>()
           .ReverseMap();

        CreateEntityMap<TicketType, TicketTypeEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<AdditionalServicesConfiguration, AdditionalServicesConfigurationEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AdditionalServicesConfigurationMatchPredicate, AdditionalServicesConfigurationMatchPredicateEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<AdditionalServicesConfigurationAdditionalService, AdditionalServicesConfigurationAdditionalServiceEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<PricingRule, PricingRuleEntity>()
           .ReverseMap();

        CreateEntityMap<LandfillSamplingRuleDto, LandfillSamplingRuleEntity>()
           .ReverseMap();

        CreateEntityMap<LandfillSamplingDto, LandfillSamplingEntity>()
           .ReverseMap();

        CreateEntityMap<AccountContactReferenceIndex, AccountContactReferenceIndexEntity>()
           .ReverseMap();

        CreateEntityMap<AccountContactIndex, AccountContactIndexEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<LoadConfirmation, LoadConfirmationEntity>()
           .ReverseMap();

        CreateEntityMap<EntityStatus, EntityStatusEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<LoadConfirmationGenerator, LoadConfirmationGeneratorEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<LoadConfirmationAttachment, LoadConfirmationAttachmentEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<TruckTicketVoidReason, TruckTicketVoidReasonEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<TruckTicketHoldReason, TruckTicketHoldReasonEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<SalesLine, SalesLineEntity>()
           .ReverseMap();

        CreateBasicMap<FSTWorkTicketRequest, FSTWorkTicketParametersDataset>()
           .ReverseMap();

        CreateOwnedEntityMap<SalesLineAttachment, SalesLineAttachmentEntity>()
           .ReverseMap();

        CreateEntityMap<EDIFieldLookup, EDIFieldLookupEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<TradeAgreementUpload, TradeAgreementUploadEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<Invoice, InvoiceEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceBillingConfiguration, InvoiceBillingConfigurationEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<InvoiceAttachment, InvoiceAttachmentEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<VolumeChange, VolumeChangeEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<EmailTemplate, EmailTemplateEntity>()
           .ReverseMap();

        CreateEntityMap<EmailTemplateEvent, EmailTemplateEventEntity>()
           .ReverseMap();

        CreateBasicMap<EmailTemplateEventField, EmailTemplateEventFieldEntity>()
           .ReverseMap();

        CreateOwnedEntityMap<EmailTemplateEventAttachment, EmailTemplateEventAttachmentEntity>()
           .ReverseMap();

        CreateAuditableEntityMap<Acknowledgement, AcknowledgementEntity>()
           .ReverseMap();
    }
}
