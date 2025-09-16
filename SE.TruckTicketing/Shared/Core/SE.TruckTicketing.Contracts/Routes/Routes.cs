namespace SE.TruckTicketing.Contracts.Routes;

public static class Routes
{
    public const string Accounts_IdRoute = "accounts/{id}";

    public const string Accounts_BaseRoute = "accounts";

    public const string Accounts_SearchRoute = "accounts/search";

    public const string Accounts_InitiateCreditReviewal = "accounts/creditreviewal";

    public const string UserProfile_IdRouteTemplate = "userprofiles/{id}";

    public const string UserProfile_BaseRoute = "userprofiles";

    public const string UserProfile_SearchRoute = "userprofiles/search";

    public const string UserProfile_Signature = "userprofiles/signature";

    public const string Backend_Toggles = "backend/toggles";

    public const string Role_IdRouteTemplate = "roles/{id}";

    public const string Role_BaseRoute = "roles";

    public const string Role_SearchRoute = "roles/search";

    public const string FacilityService_IdRouteTemplate = "facilityservices/{id}";

    public const string FacilityService_BaseRoute = "facilityservices";

    public const string FacilityService_SearchRoute = "facilityservices/search";

    public const string Permission_IdRouteTemplate = "permissions/{id}";

    public const string Permission_BaseRoute = "permissions";

    public const string Permission_SearchRoute = "permissions/search";

    public const string Facility_IdRouteTemplate = "facilities/{id}";

    public const string Facility_BaseRoute = "facilities";

    public const string Facility_SearchRoute = "facilities/search";

    public const string TruckTicket_IdRouteTemplate = "truck-tickets/{id}";

    public const string TruckTicket_BaseRoute = "truck-tickets";

    public const string TruckTicket_SearchRoute = "truck-tickets/search";

    public const string TruckTicket_Stubs = "truck-tickets/stubs";

    public const string TruckTicket_Attachment = "truck-tickets/attachment";

    public const string TruckTicket_MatchingBillingConfiguration = "truck-tickets/matching-billing-configurations";

    public const string TruckTicket_Initialize_Sales_Billing = "truck-tickets/initialize";

    public const string SourceLocationType_BaseRoute = "source-location-types";

    public const string SourceLocationType_IdRoute = "source-location-types/{id}";

    public const string SourceLocationType_SearchRoute = "source-location-types/search";

    public const string SourceLocation_BaseRoute = "source-locations";

    public const string SourceLocation_IdRoute = "source-locations/{id}";

    public const string SourceLocation_SearchRoute = "source-locations/search";

    public const string SourceLocation_MarkDelete = "source-locations/{id}/markdelete";

    public const string ServiceType_IdRouteTemplate = "servicetype/{id}";

    public const string ServiceType_BaseRoute = "servicetype";

    public const string ServiceType_SearchRoute = "servicetype/search";

    public const string Note_IdRouteTemplate = "note/{id}";

    public const string Note_BaseRoute = "note";

    public const string Note_SearchRoute = "note/search";

    public const string NavigationConfig_IdRouteTemplate = "navigation/{id}";

    public const string NavigationConfig_BaseRoute = "navigation";

    public const string NavigationConfig_SearchRoute = "navigation/search";

    public const string EDIFieldLookup_IdRouteTemplate = "edifieldlookup/{id}";

    public const string EDIFieldLookup_BaseRoute = "edifieldlookup";

    public const string EDIFieldLookup_SearchRoute = "edifieldlookup/search";

    public const string EDIFieldDefinition_IdRouteTemplate = "edifielddefinition/{id}";

    public const string EDIFieldDefinition_BaseRoute = "edifielddefinition";

    public const string EDIFieldDefinition_SearchRoute = "edifielddefinition/search";

    public const string EDIValidationPatternLookup_IdRouteTemplate = "edivalidationpatternlookup/{id}";

    public const string EDIValidationPatternLookup_BaseRoute = "edivalidationpatternlookup";

    public const string EDIValidationPatternLookup_SearchRoute = "edivalidationpatternlookup/search";

    public const string InvoiceExchange_BaseRoute = "invoice-exchange";

    public const string InvoiceExchange_IdRoute = "invoice-exchange/{id:guid}";

    public const string InvoiceExchange_SearchRoute = "invoice-exchange/search";

    public const string InvoiceExchange_SourceFields_BaseRoute = "invoice-exchange/source-fields";

    public const string InvoiceExchange_SourceFields_SearchRoute = "invoice-exchange/source-fields/search";

    public const string InvoiceExchange_DestinationFields_BaseRoute = "invoice-exchange/destination-fields";

    public const string InvoiceExchange_DestinationFields_SearchRoute = "invoice-exchange/destination-fields/search";

    public const string InvoiceExchange_ValueFormats_BaseRoute = "invoice-exchange/value-formats";

    public const string InvoiceExchange_ValueFormats_SearchRoute = "invoice-exchange/value-formats/search";

    public const string Product_IdRouteTemplate = "products/{id}";

    public const string Product_BaseRoute = "products";

    public const string Product_SearchRoute = "products/search";

    public const string SpartanProductParameter_BaseRoute = "spartanproductparameter";

    public const string SpartanProductParameter_IdRoute = "spartanproductparameter/{id}";

    public const string SpartanProductParameter_SearchRoute = "spartanproductparameter/search";

    public const string BillingConfiguration_IdRouteTemplate = "billingconfiguration/{id}";

    public const string BillingConfiguration_BaseRoute = "billingconfiguration";

    public const string BillingConfiguration_SearchRoute = "billingconfiguration/search";

    public const string MaterialApproval_BaseRoute = "material-approval";

    public const string MaterialApproval_IdRoute = "material-approval/{id}";

    public const string MaterialApproval_SearchRoute = "material-approval/search";

    public const string MaterialApproval_WasteCodeRoute = "material-approval/wastecodes/{id}";

    public const string MaterialApproval_DownloadScaleTicketStub = "material-approl/{id}/pdfs/scale-ticket-stub";

    public const string LegalEntity_BaseRoute = "legal-entity";

    public const string LegalEntity_IdRoute = "legal-entity/{id}";

    public const string LegalEntity_SearchRoute = "legal-entity/search";

    public const string NewAccounts_BaseRoute = "newaccounts";

    public const string NewAccounts_Account_ValidationRoute = "newaccounts/account/validate";

    public const string NewAccounts_SourceLocation_ValidationRoute = "newaccounts/source-location/validate";

    public const string BusinessStream_BaseRoute = "business-stream";

    public const string BusinessStream_IdRoute = "business-stream/{id}";

    public const string BusinessStream_SearchRoute = "business-stream/search";

    public const string TicketType_IdRouteTemplate = "tickettypes/{id}";

    public const string TicketType_BaseRoute = "tickettypes";

    public const string TicketType_SearchRoute = "tickettypes/search";

    public const string AdditionalServicesConfiguration_BaseRoute = "additionalservicesconfiguration";

    public const string AdditionalServicesConfiguration_SearchRoute = "additionalservicesconfiguration/search";

    public const string AdditionalServicesConfiguration_IdRouteTemplate = "additionalservicesconfiguration/{id:guid}";

    public const string PricingSelection_ComputeRoute = "pricing/compute";

    public const string PricingSelection_IdRouteTemplate = "pricing/{id}";

    public const string PricingSelection_BaseRoute = "pricing";

    public const string PricingSelection_SearchRoute = "pricing/search";

    public const string Change_IdRouteTemplate = "changes/{id}";

    public const string Change_BaseRoute = "changes";

    public const string Change_SearchRoute = "changes/search";

    public const string TruckTicketVoidReason_IdRouteTemplate = "tt-voidreason/{id}";

    public const string TruckTicketVoidReason_BaseRoute = "tt-voidreason";

    public const string TruckTicketVoidReason_SearchRoute = "tt-voidreason/search";

    public const string TruckTicketHoldReason_IdRouteTemplate = "tt-holdreason/{id}";

    public const string TruckTicketHoldReason_BaseRoute = "tt-holdreason";

    public const string TruckTicketHoldReason_SearchRoute = "tt-holdreason/search";

    public const string InvoiceConfiguration_IdRouteTemplate = "invoiceconfiguration/{id}";

    public const string InvoiceConfiguration_BaseRoute = "invoiceconfiguration";

    public const string InvoiceConfiguration_SearchRoute = "invoiceconfiguration/search";

    public const string InvoiceConfiguration_Invalid_BillingConfiguration = "invoiceconfiguration/invalid-billing-configurations";

    public const string InvoiceConfiguration_Clone = "invoiceconfiguration/clone-invoice-configurations";

    public const string Substance_IdRouteTemplate = "substances/{id}";

    public const string Substance_BaseRoute = "substances";

    public const string Substance_SearchRoute = "substances/search";

    public const string MaterialApprovalPdf = "material-approval/{id}/pdf";

    public static class Parameters
    {
        public const string id = nameof(id);

        public const string AttachmentId = nameof(AttachmentId);
    }

    public static class NewAccount
    {
        private const string Base = "newaccounts";

        public const string AttachmentDownload = Base + $"/attachment/{Parameters.Id}/{Parameters.Attachmentid}";

        public static class Parameters
        {
            public const string Id = "{id}";

            public const string Attachmentid = "{attachmentid}";
        }
    }

    public static class TruckTickets
    {
        public const string Base = "truck-tickets";

        public const string TicketDownload = Base + $"/{Parameters.Key}/pdfs/ticket-complete";

        public const string FstDailyWorkTicketDownload = Base + "/pdfs/fst-daily-work-ticket";

        public const string FstDailyWorkTicketDownloadXlsx = Base + "/xlsx/fst-daily-work-ticket";

        public const string LandfillTicketDownload = Base + "/pdfs/landfill-ticket-complete";

        public const string LandfillTicketDownloadXlsx = Base + "/xlsx/landfill-ticket-complete";

        public const string LoadSummaryTicketDownload = Base + "/pdfs/load-summary-ticket";

        public const string SalesLineBulkSave = Base + "/{id}/sales-line";

        public const string TicketAndSalesPersistence = Base + "/transactions";

        public const string AttachmentDownload = Base + $"/attachments/{Parameters.Id}/{Parameters.AttachmentId}";

        public const string AttachmentUpload = Base + $"/attachments/{Parameters.Id}";

        public const string SplitTruckTicket = Base + $"/{Parameters.Key}/split-ticket";

        public const string ConfirmCustomerOnTruckTickets = Base + "/confirm-customer-tickets";

        public const string AttachmentMarkUploaded = Base + $"/attachments/uploaded/{Parameters.Id}/{Parameters.AttachmentId}";

        public const string AttachmentRemove = Base + $"/attachments/remove/{Parameters.Key}/{Parameters.AttachmentId}";

        public const string ProducerReportDownload = Base + "/pdfs/producer-report";

        public const string EvaluateInvoiceThreshold = Base + "/evaluate-invoice-threshold";

        public static class Parameters
        {
            public const string Id = "{id}";

            public const string Pk = "{pk}";

            public const string Key = $"{Id}@{Pk}";

            public const string AttachmentId = "{attachmentId}";
        }
    }

    public static class SalesManagement
    {
        public const string Base = "sales-management";

        public const string Facility = Base + $"/{Params.FacilityId}";

        public static class Params
        {
            public const string FacilityId = "{facilityId}";
        }
    }

    public static class FacilityService
    {
        public const string Base = "facility-serivces";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class FacilityServiceSubstanceIndex
    {
        public const string Base = "facility-serivce-substances";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class TruckTicketWellClassification
    {
        public const string Base = "truck-ticket-well-classifications";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class TruckTicketTareWeight
    {
        public const string Base = "truck-ticket-tare-weight";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class AccountContactReferenceIndex
    {
        public const string Base = "account-contact-reference";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class AccountContactIndex
    {
        public const string Base = "account-contact-index";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class LoadConfirmation
    {
        public const string Base = "load-confirmations";

        public const string Id = Base + "/{id}";

        public const string Facility = Base + $"/{Params.FacilityId}";

        public const string Search = Base + "/search";

        public const string BulkAction = Base + "/bulk-action";

        public const string Preview = Base + $"/{Params.Key}/preview-latest";

        public const string Download = Base + $"/{Params.Key}/download-latest";

        public const string AttachmentDownload = Base + $"/attachments/{Params.Key}/{Params.AttachmentId}";

        public const string AttachmentRemove = Base + $"/attachments/remove/{Params.Key}/{Params.AttachmentId}";

        public const string AttachmentUpload = Base + $"/attachments/{Params.Key}";

        public const string FetchMany = Base + "/fetch-many";

        public static class Params
        {
            public const string Id = "{id}";

            public const string Pk = "{pk}";

            public const string Key = $"{Id}@{Pk}";

            public const string FacilityId = "{facilityId}";

            public const string AttachmentId = "{attachmentId}";

            public const string Path = "{path}";
        }
    }

    public static class InvoiceConfigurationPermutationsIndex
    {
        public const string Base = "invoice-configuration-permutations";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class LandfillSampling
    {
        public const string Base = "landfill-samplings";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";

        public const string CheckStatus = Base + "/check-status";
    }

    public static class LandfillSamplingRule
    {
        public const string Base = "landfill-sampling-rules";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class SalesLine
    {
        public const string Base = "sales-lines";

        public const string Id = Base + "/{id:guid}";

        public const string Search = Base + "/search";

        public const string Preview = Base + "/preview";

        public const string PreviewLoadConfirmation = Base + "/preview-lc";

        public const string SendAdHocLoadConfirmation = Base + "/send-adhoc-lc";

        public const string Price = Base + "/price";

        public const string PriceRefresh = Base + "/price-refresh";

        public const string Bulk = Base + "/bulk";

        public const string Remove = Base + "/remove";
    }

    public static class TradeAgreementUploads
    {
        public const string Base = "trade-agreement-uploads";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";

        public const string UploadUris = Base + "/upload-uris";
    }

    public static class Invoice
    {
        public const string Base = "invoices";

        public const string Id = Base + $"/{Parameters.Id}";

        public const string Search = Base + "/search";

        public const string Preview = Base + "/preview";

        public const string AttachmentDownload = Base + $"/attachments/{Parameters.InvoiceKey}/{Parameters.AttachmentId}";

        public const string AttachmentUpload = Base + $"/attachments/{Parameters.InvoiceKey}";

        public const string AttachmentMarkUploaded = Base + $"/attachments/uploaded/{Parameters.InvoiceKey}/{Parameters.AttachmentId}";

        public const string PostInvoiceAction = Base + "/postinvoiceaction";

        public const string VoidInvoice = Base + "/void";

        public const string ReverseInvoice = Base + "/reverse";

        public const string AdvancedEmail = Base + "/advanced-email";

        public const string UpdateCollectionInfo = Base + "/update-collection-info";

        public const string Facility = Base + $"/{Parameters.FacilityId}";

        public const string PublishSalesOrder = Base + $"/{Parameters.InvoiceKey}/publish-sales-order";

        public static class Parameters
        {
            public const string Id = "{id}";

            public const string Pk = "{pk}";

            public const string InvoiceKey = $"{Id}@{Pk}";

            public const string AttachmentId = "{attachmentId}";

            public const string FacilityId = "{facilityId}";
        }
    }

    public static class VolumeChange
    {
        public const string Base = "volume-changes";

        public const string Id = Base + "/{id:guid}";

        public const string Search = Base + "/search";
    }

    public static class EmailTemplates
    {
        public const string Base = "email-templates";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";

        public const string AdhocAttachmentUpload = Base + "/adhoc-attachmments";

        public const string Delivery = Base + "/delivery";
    }

    public static class EmailTemplateEvents
    {
        public const string Base = "email-template-events";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }

    public static class Acknowledgement
    {
        public const string Base = "acknowledgement";

        public const string Id = Base + "/{id}";

        public const string Search = Base + "/search";
    }
}
