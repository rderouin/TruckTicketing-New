namespace SE.Enterprise.Contracts.Constants;

public static class ServiceBusConstants
{
    public const string PrivateServiceBusNamespace = nameof(PrivateServiceBusNamespace);

    public static class Queues
    {
        public const string ScannedTruckTicketProcessing = "%Queue:ScannedTruckTicketProcessing%";

        public const string LowLatencyScannedTruckTicketProcessing = "%Queue:LowLatencyScannedTruckTicketProcessing%";
    }

    public static class Topics
    {
        public const string EntityUpdates = "%Topic:EntityUpdates%";

        public const string SpartanTickets = "%Topic:SpartanTickets%";

        public const string InvoiceDelivery = "%Topic:InvoiceDelivery%";

        public const string InvoiceMerge = "%Topic:InvoiceMerge%";

        public const string ApprovalEmails = "%Topic:ApprovalEmails%";

        public const string ChangeEntities = "%Topic:Changes%";
    }

    public static class Queue
    {
        public const string MaterialApprovalLoadSummaryReport = "%Queue:MaterialApprovalLoadSummaryReport%";
    }

    public static class Sources
    {
        public const string TruckTicketing = "TT";

        public const string TruckTicketingAsyncFlows = "TT-ASYNC";
    }

    public static class Subscriptions
    {
        public const string TruckTicketing = "%Subscription:EntityUpdates%";

        public const string SpartanTickets = "%Subscription:SpartanTickets%";

        public const string InvoiceDelivery = "%Subscription:InvoiceDelivery%";

        public const string InvoiceDeliveryResponses = "%Subscription:InvoiceDeliveryResponses%";

        public const string InvoiceMergeRequests = "%Subscription:InvoiceMergeRequests%";

        public const string ApprovalEmailsInbound = "%Subscription:ApprovalEmailsInbound%";

        public const string AttachmentsInbound = "%Subscription:AttachmentsInbound%";

        public const string Sales = "%Subscription:Sales%";

        public const string ChangeProcess = "%Subscription:ChangeProcess%";

        public const string ChangeArchive = "%Subscription:ChangeArchive%";
    }

    public static class EntityMessageTypes
    {
        public const string Customer = "Customer";

        public const string Facility = "Facility";

        public const string TruckTicket = "SpartanOffLoadSummary";

        public const string ScannedAttachment = "ScannedAttachment";

        public const string MaterialApprovalLoadSummaryReport = "MaterialApprovalLoadSummaryReport";

        public const string TradeAgreement = "TradeAgreement";

        public const string LegalEntity = "LegalEntity";

        public const string Product = "Product";

        public const string AccountContact = "AccountContact";

        public const string TaxGroup = nameof(TaxGroup);

        public const string BusinessStream = nameof(BusinessStream);

        public const string LegalEntityMessage = nameof(LegalEntityMessage);

        public const string SalesLineAttachment = nameof(SalesLineAttachment);

        public const string SalesOrderAck = nameof(SalesOrderAck);

        public const string InvoiceAck = nameof(InvoiceAck);

        public const string SalesLineAck = nameof(SalesLineAck);

        public const string ProcessLoadConfirmationRequest = nameof(ProcessLoadConfirmationRequest);
    }

    public static class HttpEndPoints
    {
        public const string FOSalesOrderLogicAppHttpEndPoint = nameof(FOSalesOrderLogicAppHttpEndPoint);
    }
}

public static class BlobStorageConstants
{
    public const string DocumentStorageAccount = nameof(DocumentStorageAccount);

    public static class Paths
    {
        public const string TTScannedAttachmentsInbound = "%Path:TTScannedAttachmentsInbound%";
    }

    public static class Containers
    {
        public const string TTScannedAttachmentsInbound = "%Container:TTScannedAttachmentsInbound%";
    }
}
