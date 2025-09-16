namespace SE.TruckTicketing.Contracts.Security;

public static class Permissions
{
    public static class Resources
    {
        public const string UserProfile = nameof(UserProfile);

        public const string Roles = nameof(Roles);

        public const string Account = nameof(Account);

        public const string SpartanProductParameter = nameof(SpartanProductParameter);

        public const string MaterialApproval = nameof(MaterialApproval);

        public const string TruckTicket = nameof(TruckTicket);

        public const string AdditionalServicesConfiguration = nameof(AdditionalServicesConfiguration);

        public const string TruckTicketTareWeight = nameof(TruckTicketTareWeight);

        public const string LoadConfirmation = nameof(LoadConfirmation);

        public const string LandfillSampleRule = nameof(LandfillSampleRule);

        public const string SalesLine = nameof(SalesLine);

        public const string TradeAgreement = nameof(TradeAgreement);

        public const string InvoiceExchangeConfiguration = nameof(InvoiceExchangeConfiguration);

        public const string EmailTemplate = nameof(EmailTemplate);

        public const string Acknowledgement = nameof(Acknowledgement);

        public const string SourceLocation = nameof(SourceLocation);

        public const string SourceLocationType = nameof(SourceLocationType);

        public const string ServiceType = nameof(ServiceType);

        public const string Facility = nameof(Facility);

        public const string Invoice = nameof(Invoice);

        public const string ProducerReport = nameof(ProducerReport);

        public const string LandfillDailyWorkReport = nameof(LandfillDailyWorkReport);

        public const string FstDailyWorkReport = nameof(FstDailyWorkReport);

        public const string LoadSummaryReport = nameof(LoadSummaryReport);

        public const string VolumeChangeReport = nameof(VolumeChangeReport);

        public const string DefaultInvoiceConfig = "Account.InvoiceConfig.Default";

        public const string DefaultBillingConfig = "Account.InvoiceConfig.Default";

        public const string AccountEdiFieldDefinition = "Account.EdiFieldDefinition";

        public const string AccountOtherInfo = "Account.OtherInfo";

        public const string NonBillableAccountName = "Account.NonBillable.Name";

        public const string SourceLocationNameId = "SourceLocation.NameId";

        public const string SourceLocationOwnershipInfo = "SourceLocation.OwnershipInfo";

        public const string SourceLocationOwnershipInfoBulk = "SourceLocation.OwnershipInfo.Bulk";

        public const string TruckTicketBilling = "TruckTicket.Billing";

        public const string TruckTicketSalesPricing = "TruckTicket.Sales.Pricing";

        public const string TruckTicketAttachment = "TruckTicket.Attachment";

        public const string SalesLinePricingBulk = "SalesLine.Pricing.Bulk";

        public const string SalesLinePricingRefresh = "SalesLine.PriceRefresh";

        public const string LoadConfirmationSalesLineRemoval = "LoadConfirmation.SalesLineRemoval";

        public const string InvoiceReversal = "Invoice.Reversal";

        public const string InvoicePaidUnsettledTransition = "Invoice.PaidUnstelledTransition";

        public const string InvoiceCollectionNotes = "Invoice.CollectionNotes";
    }

    public static class Operations
    {
        public const string View = "v";

        public const string Read = "r";

        public const string Write = "w";

        public const string Delete = "d";

        public const string Manage = "m";

        public const string Escalate = "e";

        public const string Process = "p";

        public const string Cancel = "c";

        public const string Create = "n";

        public const string Approve = "a";

        public const string Upload = "u";

        public const string Publish = "h";

        public const string Execute = "x";
    }
}
