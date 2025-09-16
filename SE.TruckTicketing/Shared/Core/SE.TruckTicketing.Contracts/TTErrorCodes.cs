using System.ComponentModel;

// ReSharper disable InconsistentNaming

namespace SE.TruckTicketing.Contracts;

public enum TTErrorCodes
{
    [Description("Valid Sequence Type required")]
    SequenceGeneration_Type = 2000,

    [Description("Valid Prefix required")]
    SequenceGeneration_Prefix = 2010,

    [Description("LastNumber should be greater than Seed for given SequenceType")]
    SequenceGeneration_LastNumber = 2020,

    [Description("Requested Blocksize should be greater than 0 and no greater than MaxRequestedBlockSize for given SequenceType")]
    SequenceGeneration_BlockSize = 2030,

    [Description("SequenceType is immutable")]
    SequenceGeneration_TypeCheck = 2040,

    [Description("SequencePrefix is immutable")]
    SequenceGeneration_PrefixCheck = 2050,

    //TruckTicket - 3xxx
    [Description("TruckTicket BillOfLading required")]
    TruckTicket_BillOfLading_Required = 3000,

    TruckTicket_BillingConfiguration_Required = 3001,

    [Description("Valid Date required for TruckTicket")]
    TruckTicket_Date = 3005,

    [Description("Valid FacilityId required for TruckTicket")]
    TruckTicket_FacilityId = 3010,

    [Description("Valid FacilityName required for TruckTicket")]
    TruckTicket_FacilityName = 3015,

    [Description("TruckTicket GeneratorId required")]
    TruckTicket_GeneratorId = 3020,

    [Description("TruckTicket GeneratorName required")]
    TruckTicket_GeneratorName = 3025,

    [Description("TruckTicket Dow required")]
    TruckTicket_Dow = 3030,

    [Description("TruckTicket LoadDate required")]
    TruckTicket_LoadDate_Required = 3035,

    [Description("Valid SourceLocationId required for TruckTicket")]
    TruckTicket_SourceLocationId_Required = 3040,

    [Description("Valid SourceLocationName required for TruckTicket")]
    TruckTicket_SourceLocationName = 3045,

    [Description("TruckTicket Status is required")]
    TruckTicket_Status = 3050,

    [Description("TruckTicket SubstanceId required")]
    TruckTicket_SubstanceId = 3055,

    [Description("TruckTicket SubstanceName required")]
    TruckTicket_SubstanceName = 3060,

    [Description("TruckTicket AdditionalServicesEnabled required")]
    TruckTicket_AdditionalServicesEnabled = 3064,

    [Description("TruckTicket TicketNumber is required")]
    TruckTicket_TicketNumber_Required = 3065,

    [Description("TruckTicket TicketNumber is required")]
    TruckTicket_Spartan_TicketNumber_Invalid = 3066,

    [Description("TruckTicket TimeIn required")]
    TruckTicket_TimeIn = 3070,

    [Description("TruckTicket TimeOut required")]
    TruckTicket_TimeOut = 3075,

    [Description("Valid TruckingCompanyId required for TruckTicket")]
    TruckTicket_TruckingCompanyId = 3080,

    [Description("Valid TruckingCompanyName required for TruckTicket")]
    TruckTicket_TruckingCompanyName = 3085,

    TruckTicket_VolumeOilBothOutOfRange = 3086,

    TruckTicket_VolumeOilGreaterThanMax = 3087,

    TruckTicket_VolumeOilLessThanMin = 3088,

    TruckTicket_VolumeWaterBothOutOfRange = 3089,

    TruckTicket_VolumeWaterGreaterThanMax = 3091,

    TruckTicket_VolumeWaterLessThanMin = 3092,

    TruckTicket_VolumeSolidBothOutOfRange = 3093,

    TruckTicket_VolumeSolidGreaterThanMax = 3094,

    TruckTicket_VolumeSolidLessThanMin = 3096,

    TruckTicket_VolumeOilFixedBothOutOfRange = 3076,

    TruckTicket_VolumeOilFixedGreaterThanMax = 3077,

    TruckTicket_VolumeOilFixedLessThanMin = 3078,

    TruckTicket_VolumeWaterFixedBothOutOfRange = 3079,

    TruckTicket_VolumeWaterFixedGreaterThanMax = 3081,

    TruckTicket_VolumeWaterFixedLessThanMin = 3082,

    TruckTicket_VolumeSolidFixedBothOutOfRange = 3083,

    TruckTicket_VolumeSolidFixedGreaterThanMax = 3084,

    TruckTicket_VolumeSolidFixedLessThanMin = 3097,

    [Description("TruckTicket ValidationStatus is required")]
    TruckTicket_ValidationStatus = 3090,

    [Description("Sample Intials must not be empty when truck ticket includes sampling")]
    TruckTicket_LandFillSampleInitials = 3095,

    [Description("TruckTicket Well Classification is required")]
    TruckTicket_WellClassification = 3100,

    [Description("TruckTicket FacilityServiceId is required")]
    TruckTicket_FacilityServiceId = 3105,

    [Description("TruckTicket Unit of Measure is required")]
    TruckTicket_UnitOfMeasure = 3110,

    [Description("TruckTicket MaterialApprovalId is required")]
    TruckTicket_MaterialApprovalId = 3115,

    [Description("TruckTicket Gross Weight is required")]
    TruckTicket_GrossWeight = 3120,

    [Description("TruckTicket Tare Weight is required")]
    TruckTicket_TareWeight = 3125,

    [Description("TruckTicket Net Weight is required")]
    TruckTicket_NetWeight = 3130,

    [Description("TruckTicket Haz/NonHaz is required")]
    TruckTicket_HazNonHaz = 3135,

    [Description("Sample is past due. Truck ticket must include random sample")]
    TruckTicket_LandfillSampleThreshold = 4000,

    [Description("Volume change reason comments required when reason is other")]
    TruckTicket_VolumeChangeReasonComments = 4005,

    //Facility - 4xxx
    [Description("Facility SiteId is required")]
    Facility_SiteId = 4000,

    [Description("Facility Type is required")]
    Facility_Type = 4010,

    [Description("Facility Name is required")]
    Facility_Name = 4020,

    [Description("Facility Legal Entity is required")]
    Facility_LegalEntity = 4030,

    [Description("Facility Province/State is required")]
    Facility_ProvinceState = 4040,

    Facility_DefaultDensityConversionFactor_StartDate_Required_ByWeight = 4050,

    Facility_DefaultDensityConversionFactor_StartDate_Required_ByMidWeight = 4060,

    Facility_DefaultDensityConversionFactor_OverlappingFacilityService_ByWeight = 4070,

    Facility_DefaultDensityConversionFactor_OverlappingFacilityService_ByMidWeight = 4080,

    Facility_DefaultDensityConversionFactor_OverlappingTimePeriod_ByWeight = 4090,

    Facility_DefaultDensityConversionFactor_OverlappingTimePeriod_ByMidWeight = 4100,

    //FacilityService - 2xxx
    [Description("ServiceTypeId is required")]
    FacilityService_ServiceTypeIdRequired = 2000,

    [Description("Description is required.")]
    FacilityService_DescriptionRequired = 2010,

    [Description("FacilityServiceNumber is required")]
    FacilityService_FacilityServiceNumberRequired = 2020,

    [Description("ServiceNumber must be positive")]
    FacilityService_ServiceNumberPositive = 2030,

    [Description("ServiceNumber must be unique")]
    FacilityService_ServiceNumberUnique = 2040,

    [Description("Country is required")]
    ServiceType_CountryCode = 8000,

    [Description("Service Type Id is required and length should not be greater than 50 characters")]
    ServiceType_ServiceTypeId = 8010,

    [Description("Service Type name is required and length should not be greater than 50 characters")]
    ServiceType_ServiceTypeName = 8020,

    [Description("Class name is required")]
    ServiceType_Class = 8030,

    [Description("Total Item Number is required")]
    ServiceType_TotalItemProduct = 8040,

    [Description("Report As Cut Type is required")]
    ServiceType_ReportAsCutType = 8050,

    [Description("Stream is required")]
    ServiceType_Stream = 8060,

    [Description("ServiceType with similar details already exists.")]
    ServiceType_UniqueHash = 8070,

    [Description("Maximum percentage should be less than minimum percentage")]
    ServiceType_MinPercentageGreaterThanMax = 8080,

    [Description("Maximum percentage cannot be greater than 100")]
    ServiceType_MaxPercentageGreaterThan100 = 8090,

    ServiceType_InvalidReportAsCutTypeSelected = 8099,

    //Note - 9
    [Description("Comment can not be empty")]
    Note_Comment = 9000,

    [Description("CreatedBy is required")]
    Note_CreatedBy = 9010,

    [Description("Date of creation is required")]
    Note_CreatedAt = 9020,

    [Description("Last modified date is required")]
    Note_UpdatedAt = 9030,

    [Description("ThreadId is required")]
    Note_ThreadId = 9040,

    [Description("Updated By is required")]
    Note_UpdatedById = 9050,

    [Description("CreatedByid is required")]
    Note_CreatedById = 9060,

    InvoiceExchange_PlatformCodeRequired = 10000,

    InvoiceExchange_TypeRequired = 10001,

    InvoiceExchange_BasedOnInvoiceExchangeIdRequired = 10002,

    InvoiceExchange_BusinessStreamMustBeEmpty = 10011,

    InvoiceExchange_BusinessStreamMustNotBeEmpty = 10012,

    InvoiceExchange_LegalEntityMustBeEmpty = 10013,

    InvoiceExchange_LegalEntityMustNotBeEmpty = 10014,

    InvoiceExchange_AccountMustBeEmpty = 10015,

    InvoiceExchange_AccountMustNotBeEmpty = 10016,

    InvoiceExchange_MessageAdapterTypeNotEmpty_ForInvoices = 10050,

    InvoiceExchange_MessageAdapterTypeNotEmpty_ForFieldTickets = 10051,

    InvoiceExchange_ParentRequiredForChild = 10100,

    InvoiceExchange_ParentSupportsFieldTicketsMustMatch = 10101,

    InvoiceExchange_ParentInvoiceAdapterMustMatch = 10102,

    InvoiceExchange_ParentFieldTicketAdapterMustMatch = 10103,

    [Description("Start Date is required")]
    BillingConfiguration_StartDate_Required = 11000,

    [Description("End Date must be greater than Start Date")]
    BillingConfiguration_EndDate_GreaterThan_StartDate = 11010,

    [Description("MatchCriteria contains invalid date range(s)")]
    BillingConfiguration_MatchCriteria_Dates_WithIn = 11005,

    [Description("field value doesn't not match field datatype")]
    BillingConfiguration_EDIFieldValue_DataTypeInvalid = 11015,

    [Description("field value is required")]
    EDIFieldDefinition_EDIFieldValue_Required = 11025,

    [Description("field value should match validation pattern")]
    EDIFieldDefinition_EDIFieldValue_ValidationFailed = 11035,

    [Description("Name is required")]
    BillingConfiguration_Name_Required = 11045,

    BillingConfiguration_MatchCriteria_Invalid_WellClassificationState = 11055,

    BillingConfiguration_MatchCriteria_Invalid_SourceLocationValueState = 11065,

    BillingConfiguration_MatchCriteria_Invalid_SubstanceValueState = 11075,

    BillingConfiguration_MatchCriteria_Invalid_ServiceTypeValueState = 11085,

    BillingConfiguration_MatchCriteria_Invalid_StreamValueState = 11095,

    BillingConfiguration_IncludeForAutomation_SetToFalse_NoMatchPredicate = 11055,

    BillingConfiguration_Signatories_Required = 11105,

    SourceLocationType_CountryCodeRequired = 20000,

    SourceLocationType_ProvinceRequired = 20010,

    SourceLocationType_NameRequired = 20001,

    SourceLocationType_CategoryRequired = 20003,

    SourceLocationType_ShortFormCodeRequired = 20002,

    SourceLocationType_NameMustBeUniqueInEachCountry = 20004,

    SourceLocationType_DownHoleTypeDefaultRequired = 20005,

    SourceLocationType_DeliveryMethodDefaultRequired = 20006,

    SourceLocation_IdentifierRequired = 21000,

    SourceLocation_FormattedIdentifierInvalid = 21001,

    SourceLocation_GeneratorStartDateRequired = 21010,

    SourceLocation_GeneratorStartDateMustBeLaterThanPreviousStartDate = 22020,

    SourceLocation_GeneratorRequired = 21005,

    SourceLocation_ContractOperatorRequired = 22030,

    SourceLocation_WellMustBeAssociatedWithSurfaceLocation = 22040,

    SourceLocation_SurfaceLocationsCannotInitiateAssociations = 22045,

    SourceLocation_SourceLocationCode_Required = 22046,

    SourceLocation_NameRequiredForUSLocations = 22050,

    SourceLocation_NameMustBeUnique = 22051,

    SourceLocation_IdentifierMustBeUnique = 22055,

    SourceLocation_LicenseNumberInvalid = 22059,

    SourceLocation_PlsNumberIsInvalid = 22060,

    SourceLocation_PlsNumberIsRequired = 22061,

    SourceLocation_ApiNumberIsInvalid = 22062,

    SourceLocation_ApiNumberIsRequired = 22063,

    SourceLocation_WellFileNumberIsInvalid = 22064,

    SourceLocation_WellFileNumberIsRequired = 22065,

    SourceLocation_CtbNumberIsInvalid = 22066,

    SourceLocation_CtbNumberIsRequired = 22067,

    SourceLocation_DownHoleTypeRequired = 22070,

    SourceLocation_DeliveryMethodRequired = 22080,

    SourceLocation_SourceLocationCodeInvalid = 22090,

    SpartanProductParam_ProductNameMustBeUnique = 22100,

    SpartanProductParam_ProductNameIsRequired = 22110,

    SpartanProductParam_MaxLessThanMin = 22120,

    SpartanProductParam_ProductNameLessThan100 = 22130,

    SpartanProductParam_percentageLessThan100 = 22150,

    [Description("Facility is required")]
    MaterialApproval_Facility = 15000,

    [Description("Description length cannot be more than 50")]
    MaterialApproval_Description = 15010,

    [Description("Generator is required")]
    MaterialApproval_Generator = 15020,

    [Description("Billing Customer is required")]
    MaterialApproval_BillingCustomer = 15030,

    [Description("Billing Customer Contact is required")]
    MaterialApproval_BillingCustomerContact = 15040,

    [Description("Billing Customer Contact Address is required")]
    MaterialApproval_BillingCustomerContactAddress = 15040,

    [Description("Facility service number is required")]
    MaterialApproval_FacilityServiceNumber = 15050,

    [Description("Facility service name is required")]
    MaterialApproval_FacilityServiceName = 15060,

    [Description("Substance is required")]
    MaterialApproval_Substance = 15070,

    [Description("Analytical Expiry date is required")]
    MaterialApproval_AnalyticalExpiryDate = 15080,

    [Description("Accumulated Tonnage is required")]
    MaterialApproval_AccumulatedTonnage = 15090,

    [Description("Source Region is required")]
    MaterialApproval_SourceRegion = 15100,

    [Description("Secure Representative is required")]
    MaterialApproval_SecureRepresentative = 15110,

    [Description("Waste Code is required")]
    MaterialApproval_WasteCode = 15120,

    [Description("Notes length cannot be more than 100")]
    MaterialApproval_Notes = 15130,

    MaterialApproval_Signatories_Constraint = 15140,

    [Description("Landfill facilities must select either Hazardous or Non-Hazardous")]
    MaterialApproval_HazardousClassification = 15150,

    [Description("WLAF Number must have the correct format.")]
    MaterialApproval_WlafNumber = 15160,

    //Accounts 3xxxx
    Account_NameMustBeUniqueForOpenAccounts = 30000,

    Account_NameRequired = 30001,

    Account_TypeRequired = 30002,

    Account_Address_AtleastOnePrimaryAddressRequiredForAccounts = 30003,

    [Description("Account Nickname must be less than 50 characters.")]
    Account_NickNameMax50 = 30012,

    Account_Address_PostalCodeInvalidFormat = 30004,

    Account_Address_MinimumOnePrimaryAddressForAccount = 30005,

    Account_Contact_FirstNameRequiredForPrimaryContact = 30006,

    Account_Contact_LastNameRequiredForPrimaryContact = 30007,

    Account_Contact_ContactEmailRequired = 30008,

    Account_Contact_ContactEmailInvalidFormat = 30009,

    Account_Contact_ContactPhoneNumberRequired = 30010,

    Account_Contact_ContactPhoneNumberInvalidFormat = 30011,

    Account_LegalEntity_Required = 30012,

    Account_LegalEntityName_Required = 30013,

    Account_Contact_AtleastOnePrimaryContact = 30014,

    Account_Address_StreetRequired = 30015,

    Account_Address_CityRequired = 30016,

    Account_Address_PostalCodeRequired = 30017,

    Account_Address_PostalCodeValidFormat = 30020,

    Account_Address_CountryCodeRequired = 30018,

    Account_Address_StateProvinceRequired = 30019,

    Account_Contact_Required = 30021,

    Account_Contact_OnlySinglePrimaryContactAllowed = 30022,

    Account_Contact_FunctionsRequired = 30023,

    Account_Contact_Duplicate = 300230,

    Account_Contact_Account_CountryCodeRequired = 30024,

    Account_Contact_Account_PostalCodeRequired = 30025,

    Account_Contact_Address_PostalCodeValidFormat = 30028,

    Account_Contact_Account_StateProvinceRequired = 30026,

    Account_Contact_Account_StreetRequired = 30036,

    Account_Contact_Account_CityRequired = 30037,

    Account_Contact_ActiveAssociationFound = 30027,

    Account_AccountType_CustomerFlagUpdated = 30028,

    Account_ActiveAssociationFound = 30029,

    Account_MailingAddress_StreetRequired = 30030,

    Account_MailingAddress_CityRequired = 30031,

    Account_MailingAddress_PostalCodeRequired = 30032,

    Account_MailingAddress_PostalCodeValidFormat = 30033,

    Account_MailingAddress_CountryCodeRequired = 30034,

    Account_MailingAddress_StateProvinceRequired = 30035,

    Account_Contact_InvalidContactFunction_PrimaryBillingContact = 30038,

    Account_BillingType_Required = 30039,

    Account_Contact_Address_Required = 30040,

    //AdditionalServicesConfiguration 5xxxx
    [Description("Name is required")]
    AdditionalServicesConfiguration_Name = 5000,

    [Description("Customer is required")]
    AdditionalServicesConfiguration_Customer = 5005,

    [Description("Facility is required")]
    AdditionalServicesConfiguration_Facility = 5010,

    InvoiceConfiguration_Name_Length = 50010,

    InvoiceConfiguration_Description_Length = 50011,

    InvoiceConfiguration_Name_Invalid = 50012,

    InvoiceConfiguration_Customer_Required = 50013,

    InvoiceConfiguration_Invalid_BillingConfigurations = 50014,

    InvoiceConfiguration_Name_Required = 50015,

    InvoiceConfiguration_ExistingCatchAll_For_SelectedCustomer = 50016,

    InvoiceConfiguration_Facility_Required = 50017,

    //EDIFieldDefinitionEntity
    [Description("Field Lookup is required")]
    EDIFieldDefinitionEntity_FieldLookupRequired = 5050,

    [Description("Field Name is required")]
    EDIFieldDefinitionEntity_FieldNameRequired = 5055,

    [Description("Validation Pattern is required")]
    EDIFieldDefinitionEntity_ValidationPatternRequired = 5060,

    [Description("Validation Error Message is required")]
    EDIFieldDefinitionEntity_ValidationErrorMessageRequired = 5065,

    [Description("EDI Field already exists")]
    EDIFieldDefinitionEntity_EDIFieldAlreadyExists = 5070,

    //VolumeChange 6xxxx
    [Description("Ticket Date is required")]
    VolumeChange_TicketDate = 6000,

    [Description("Ticket Number is required")]
    VolumeChange_TicketNumber = 6005,

    [Description("Original Process is required")]
    VolumeChange_ProcessOriginal = 6010,

    [Description("Original Oil Volume is required")]
    VolumeChange_OilVolumeOriginal = 6015,

    [Description("Original Water Volume is required")]
    VolumeChange_WaterVolumeOriginal = 6020,

    [Description("Original Solid Volume is required")]
    VolumeChange_SolidVolumeOriginal = 6025,

    [Description("Original Total Volume is required")]
    VolumeChange_TotalVolumeOriginal = 6030,

    [Description("Adjusted Process is required")]
    VolumeChange_ProcessAdjusted = 6035,

    [Description("Adjusted Oil Volume is required")]
    VolumeChange_OilVolumeAdjusted = 6040,

    [Description("Adjusted Water Volume is required")]
    VolumeChange_WaterVolumeAdjusted = 6045,

    [Description("Adjusted Solid Volume is required")]
    VolumeChange_SolidVolumeAdjusted = 6050,

    [Description("Adjusted Total Volume is required")]
    VolumeChange_TotalVolumeAdjusted = 6055,

    [Description("Volume Change Reason is required")]
    VolumeChange_VolumeChangeReason = 6060,

    [Description("Facility Id is required")]
    VolumeChange_FacilityId = 6065,

    [Description("Facility Name is required")]
    VolumeChange_FacilityName = 6070,

    [Description("Truck Ticket Status is required")]
    VolumeChange_TruckTicketStatus = 6075,
    // SalesLine 6xxxx

    // Email Templates
    EmailTemplate_Name_Required = 70010,

    EmailTemplate_Name_Length = 70011,

    EmailTemplate_Name_Unique = 70012,

    EmailTemplate_EventId_Required = 70020,

    EmailTemplate_Subject_Length = 70030,

    EmailTemplate_Body_Length = 70040,

    EmailTemplate_CustomReplyEmail_Invalid = 70050,

    EmailTemplate_CustomReplyEmail_Required = 70051,

    EmailTemplate_CustomBccEmails_Invalid = 70060,

    EmailTemplate_CustomBccEmails_Required = 70061,

    EmailTemplate_FacilitySiteIds_GloballyUnique = 70070,

    EmailTemplate_FacilitySiteIds_NoFacilityOverlap = 70071,

    EmailTemplate_OverrideSender_EmailRequired = 70072,

    EmailTemplate_SenderEmail_ValidFormat = 70073,

    // Invoice
    [Description("InvoiceCollectionReason is required")]
    Invoice_InvoiceCollectionReason = 8000,

    [Description("Name is required")]
    Role_Name = 9000,

    //Substance
    Duplicate_Substance_Already_Exist = 9500,

    //Load Confirmation
    LoadConfirmationVoid_ActiveSalesLines_Exist = 10500,

    InvoiceVoid_ActiveLoadConfirmations_Exist = 10501,

    InvoicePosted_NoActiveSalesLines_Exist = 10502,

    InvoicePosted_PendingSalesLines_Exist = 10503,
}
