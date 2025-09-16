using System.Collections.Generic;

using SE.Shared.Domain.Entities.MaterialApproval;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class MaterialApprovalItem : TicketJournalItem
{
    public MaterialApprovalItem() { }

    public MaterialApprovalItem(MaterialApprovalEntity truckTicket) : base(truckTicket)
    {
        ReportName = "MaterialApprovalLF";
    }

    public string LegalEntity { get; set; }

    public string MaterialApprovalNumber { get; set; }

    public string SiteId { get; set; }

    public string LabIdNumber { get; set; }

    public string GeneratorName { get; set; }

    public string RepresentativeName { get; set; }

    public string GeneratorPhone { get; set; }

    public string GeneratorEmail { get; set; }

    public string ThirdPartyName { get; set; }
    public string ThirdPartyRepresentativeName { get; set; }
    public string ThirdPartyPhone { get; set; }

    public string ThirdPartyEmail { get; set; }

    public string BillingCustomerName { get; set; }

    public string BillingCustomerPhone { get; set; }

    public string BillingCustomerEmail { get; set; }

    public string BillingContactName { get; set; }

    public string BillingContactPhone { get; set; }

    public string BillingContactEmail { get; set; }

    public string BillingContactAddress { get; set; }

    public string AFE { get; set; }

    public string PO { get; set; }

    public string EDICode { get; set; }

    public string TruckingCompanyName { get; set; }

    public List<string> SignatoryNames { get; set; }

    public string TenormHaulerNumber { get; set; }

    public string SourceLocation { get; set; }

    public string DownholeSurface { get; set; }

    public string SubstanceName { get; set; }

    public string RigNumber { get; set; }

    public bool Hazardous { get; set; }

    public string AnalyticalExpiryDate { get; set; }

    public string DisposalUnit { get; set; }

    public string SecureRepName { get; set; }

    public string SecureRepTitle { get; set; }

    public string SecureRepSignature { get; set; }

    public override string DataSourceName => nameof(MaterialApprovalItem);

    public override TicketTypes TicketType => TicketTypes.WorkTicket;

    public string FacilityLocationCode { get; set; }
}

public class SignatoryItem
{
    public string SignatoryName { get; set; }

    public string SignatoryPhone { get; set; }

    public string SignatoryEmail { get; set; }
}
