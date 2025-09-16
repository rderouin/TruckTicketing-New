namespace SE.Shared.Domain;

public static class Databases
{
    public const string SecureEnergyDB = nameof(SecureEnergyDB);

    public static class Containers
    {
        public const string Accounts = nameof(Accounts);

        public const string Billing = nameof(Billing);

        public const string Operations = nameof(Operations);

        public const string Products = nameof(Products);

        public const string Pricing = nameof(Pricing);

        public const string Temporal = nameof(Temporal);
    }

    public static class DocumentTypes
    {
        public const string Account = "Accounts";

        public const string Sequence = nameof(Sequence);

        public const string Products = nameof(Products);

        public const string Customer = nameof(Customer);

        public const string TruckTicketWellClassification = nameof(TruckTicketWellClassification);

        public const string SourceLocation = nameof(SourceLocation);

        public const string SourceLocationType = nameof(SourceLocationType);

        public const string InvoiceExchange = nameof(InvoiceExchange);

        public const string DestinationModelField = nameof(DestinationModelField);

        public const string SourceModelField = nameof(SourceModelField);

        public const string ValueFormat = nameof(ValueFormat);

        public const string BillingConfiguration = nameof(BillingConfiguration);

        public const string FacilityService = nameof(FacilityService);

        public const string AdditionalServicesConfiguration = nameof(AdditionalServicesConfiguration);

        public const string PricingRule = nameof(PricingRule);

        public const string TruckTicketTareWeight = nameof(TruckTicketTareWeight);

        public const string AccountContact = nameof(AccountContact);

        public const string Change = nameof(Change);

        public const string InvoiceConfiguration = nameof(InvoiceConfiguration);

        public const string SalesLine = nameof(SalesLine);

        public const string LoadConfirmation = nameof(LoadConfirmation);

        public const string LandfillSamplingRule = nameof(LandfillSamplingRule);

        public const string LandfillSampling = nameof(LandfillSampling);

        public const string Substances = nameof(Substances);

        public const string InvoiceDeliveryRequest = nameof(InvoiceDeliveryRequest);

        public const string TradeAgreementUpload = nameof(TradeAgreementUpload);

        public const string EmailTemplate = nameof(EmailTemplate);

        public const string EmailTemplateEvent = nameof(EmailTemplateEvent);

        public const string VolumeChange = nameof(VolumeChange);

        public const string Invoice = nameof(Invoice);

        public const string Acknowledgement = nameof(Acknowledgement);

        public const string ServiceType = nameof(ServiceType);

        public const string EntityStatus = nameof(EntityStatus);
    }

    public static class Discriminators
    {
        public const string UserProfile = nameof(UserProfile);

        public const string Sequence = nameof(Sequence);

        public const string Facility = nameof(Facility);

        public const string Customer = nameof(Customer);

        public const string Account = nameof(Account);

        public const string SourceLocation = nameof(SourceLocation);

        public const string SourceLocationType = nameof(SourceLocationType);

        public const string TruckTicketWellClassification = nameof(TruckTicketWellClassification);

        public const string InvoiceExchange = nameof(InvoiceExchange);

        public const string DestinationModelField = nameof(DestinationModelField);

        public const string SourceModelField = nameof(SourceModelField);

        public const string ValueFormat = nameof(ValueFormat);

        public const string BillingConfiguration = nameof(BillingConfiguration);

        public const string FacilityService = nameof(FacilityService);

        public const string FacilityServiceSubstanceIndex = nameof(FacilityServiceSubstanceIndex);

        public const string AccountContactReferenceIndex = nameof(AccountContactReferenceIndex);

        public const string AccountContactIndex = nameof(AccountContactIndex);

        public const string AdditionalServicesConfiguration = nameof(AdditionalServicesConfiguration);

        public const string TruckTicketTareWeightIndex = nameof(TruckTicketTareWeightIndex);

        public const string Change = nameof(Change);

        public const string SalesLine = nameof(SalesLine);

        public const string InvoiceConfiguration = nameof(InvoiceConfiguration);

        public const string LoadConfirmation = nameof(LoadConfirmation);

        public const string LandfillSamplingRule = nameof(LandfillSamplingRule);

        public const string LandfillSampling = nameof(LandfillSampling);

        public const string Substance = nameof(Substance);

        public const string InvoiceConfigurationPermutations = nameof(InvoiceConfigurationPermutations);

        public const string InvoiceDeliveryRequest = nameof(InvoiceDeliveryRequest);

        public const string Invoice = nameof(Invoice);

        public const string VolumeChange = nameof(VolumeChange);

        public const string EmailTemplate = nameof(EmailTemplate);

        public const string EmailTemplateEvent = nameof(EmailTemplateEvent);

        public const string Acknowledgement = nameof(Acknowledgement);

        public const string TaxGroup = nameof(TaxGroup);

        public const string EntityStatus = nameof(EntityStatus);
    }
}
