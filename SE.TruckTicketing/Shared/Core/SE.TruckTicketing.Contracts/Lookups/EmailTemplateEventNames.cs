namespace SE.TruckTicketing.Contracts.Lookups;

public static class EmailTemplateEventNames
{
    public const string MaterialApprovalConfirmation = "Material Approval - Confirm Approval";

    public const string AnalyticalRenewal = "Analytical - Renewal";

    public const string AdHocLoadConfirmation = "Load Confirmation - Untracked Send";

    public const string RequestLoadConfirmationApproval = "Load Confirmation - Request LC Approval";

    public const string LoadConfirmationApprovalTampered = "Load Confirmation - Invalidated";

    public const string CreditApplicationRequestDetails = "Credit Application - Request Details";

    // NOTE: this template is not used anywhere atm 
    public const string CreditCardPaymentRequest = "Credit Card Payment - Request";

    public const string InvoicePaymentRequest = "Invoice Payment - Request";

    public const string LoadSummaryReport = "Load Summary Report - Email Send";
}
