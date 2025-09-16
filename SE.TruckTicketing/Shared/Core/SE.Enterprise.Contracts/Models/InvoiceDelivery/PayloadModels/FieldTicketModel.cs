using System;
using System.Collections.Generic;

using Newtonsoft.Json;

// ReSharper disable StringLiteralTypo

namespace SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;

public class FieldTicketModel
{
    [JsonProperty("Platform")]
    public string Platform { get; set; }

    [JsonProperty("BillingCustomerId")]
    public Guid BillingCustomerId { get; set; }

    [JsonProperty("InvoiceAccount")]
    public string InvoiceAccount { get; set; }

    [JsonProperty("InvoiceId")]
    public string InvoiceId { get; set; }

    [JsonProperty("InvoiceDate")]
    public DateTime InvoiceDate { get; set; }

    [JsonProperty("DataAreaId")]
    public string DataAreaId { get; set; }

    [JsonProperty("EdiDate")]
    public DateTime EdiDate { get; set; }

    [JsonProperty("Email")]
    public string Email { get; set; }

    [JsonProperty("EmailDelivery")]
    public FieldTicketEmailModel EmailDelivery { get; set; }

    [JsonProperty("CurrencyCode")]
    public string CurrencyCode { get; set; }

    [JsonProperty("InvoiceTotal")]
    public double InvoiceTotal { get; set; }

    [JsonProperty("TotalLineItems")]
    public int TotalLineItems { get; set; }

    [JsonProperty("BillToDuns")]
    public string BillToDuns { get; set; }

    [JsonProperty("RemitToDuns")]
    public string RemitToDuns { get; set; }

    [JsonProperty("Comments")]
    public string Comments { get; set; }

    [JsonProperty("DefaultBuyerDepartment")]
    public string DefaultBuyerDepartment { get; set; }

    [JsonProperty("ServicePeriodStartHeader")]
    public DateTime ServicePeriodStartHeader { get; set; }

    [JsonProperty("SummaryIndicator")]
    public int SummaryIndicator { get; set; }

    [JsonProperty("FirstDescription")]
    public string FirstDescription { get; set; }

    [JsonProperty("FirstWell")]
    public string FirstWell { get; set; }

    [JsonProperty("HeaderCountryCode")]
    public string HeaderCountryCode { get; set; }

    [JsonProperty("InvoiceTypeCode")]
    public string InvoiceTypeCode { get; set; }

    [JsonProperty("LineItems")]
    public List<FieldTicketLineModel> LineItems { get; set; }
}

public class FieldTicketEmailModel
{
    [JsonProperty("To")]
    public string To { get; set; }

    [JsonProperty("CC")]
    public string Cc { get; set; }

    [JsonProperty("BCC")]
    public string Bcc { get; set; }

    [JsonProperty("ReplyTo")]
    public string ReplyTo { get; set; }

    [JsonProperty("Subject")]
    public string Subject { get; set; }

    [JsonProperty("Body")]
    public string Body { get; set; }
}

public class FieldTicketLineModel
{
    [JsonProperty("ItemNumber")]
    public string ItemNumber { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("Quantity")]
    public double Quantity { get; set; }

    [JsonProperty("UnitsOfMeasure")]
    public string UnitsOfMeasure { get; set; }

    [JsonProperty("Rate")]
    public double Rate { get; set; }

    [JsonProperty("ServicePeriodEndLine")]
    public DateTime ServicePeriodEndLine { get; set; }

    [JsonProperty("ServicePeriodStartLine")]
    public DateTime ServicePeriodStartLine { get; set; }

    [JsonProperty("TaxAmount")]
    public double TaxAmount { get; set; }

    [JsonProperty("Tax")]
    public List<FieldTicketTaxModel> Tax { get; set; }

    [JsonProperty("DiscountAmount")]
    public double DiscountAmount { get; set; }

    [JsonProperty("DiscountPercent")]
    public double DiscountPercent { get; set; }

    [JsonProperty("BOLNumber")]
    public string BolNumber { get; set; }

    [JsonProperty("LineNumber")]
    public int LineNumber { get; set; }

    [JsonProperty("TicketNumber")]
    public string TicketNumber { get; set; }

    [JsonProperty("SourceLocation")]
    public string SourceLocation { get; set; }
    
    [JsonProperty("SourceLocationType")]
    public string SourceLocationType { get; set; }

    [JsonProperty("TotalAmount")]
    public double TotalAmount { get; set; }

    [JsonProperty("TicketDate")]
    public DateTime TicketDate { get; set; }

    [JsonProperty("ApproverRequestor")]
    public string ApproverRequestor { get; set; }

    [JsonProperty("InventSite")]
    public string InventSite { get; set; }

    [JsonProperty("PackingGroup")]
    public string PackingGroup { get; set; }

    [JsonProperty("Edi")]
    public FieldTicketLineEdiModel Edi { get; set; }
}

public class FieldTicketTaxModel
{
    [JsonProperty("TaxType")]
    public string TaxType { get; set; }

    [JsonProperty("TypeCode")]
    public string TypeCode { get; set; }

    [JsonProperty("Country")]
    public string Country { get; set; }

    [JsonProperty("Province")]
    public string Province { get; set; }

    [JsonProperty("Amount")]
    public double Amount { get; set; }

    [JsonProperty("Description")]
    public string Description { get; set; }

    [JsonProperty("Rate")]
    public double Rate { get; set; }
}

public class FieldTicketLineEdiModel
{
    [JsonProperty("Account")]
    public string Account { get; set; }

    [JsonProperty("AccountQty")]
    public string AccountQty { get; set; }

    [JsonProperty("AFE")]
    public string AFE { get; set; }

    [JsonProperty("AFEOwner")]
    public string AFEOwner { get; set; }

    [JsonProperty("Alpha")]
    public string Alpha { get; set; }

    [JsonProperty("ApproverCoding")]
    public string ApproverCoding { get; set; }

    [JsonProperty("AssetNumber")]
    public string AssetNumber { get; set; }

    [JsonProperty("Attnto")]
    public string Attnto { get; set; }

    [JsonProperty("AttnToName")]
    public string AttnToName { get; set; }

    [JsonProperty("BuyerCode")]
    public string BuyerCode { get; set; }

    [JsonProperty("BuyerDepartment")]
    public string BuyerDepartment { get; set; }

    [JsonProperty("CatalogSerialNo")]
    public string CatalogSerialNo { get; set; }

    [JsonProperty("CCType")]
    public string CCType { get; set; }

    [JsonProperty("ChargeTo")]
    public string ChargeTo { get; set; }

    [JsonProperty("ChargeToName")]
    public string ChargeToName { get; set; }

    [JsonProperty("Company")]
    public string Company { get; set; }

    [JsonProperty("CompanyRep")]
    public string CompanyRep { get; set; }

    [JsonProperty("Contract")]
    public string Contract { get; set; }

    [JsonProperty("ContractLineNumber")]
    public string ContractLineNumber { get; set; }

    [JsonProperty("CostCenter")]
    public string CostCenter { get; set; }

    [JsonProperty("CostCenterName")]
    public string CostCenterName { get; set; }

    [JsonProperty("FieldLocation")]
    public string FieldLocation { get; set; }

    [JsonProperty("GLAccount")]
    public string GLAccount { get; set; }

    [JsonProperty("Joints")]
    public string Joints { get; set; }

    [JsonProperty("Location")]
    public string Location { get; set; }

    [JsonProperty("LocationComments")]
    public string LocationComments { get; set; }

    [JsonProperty("MajorCode")]
    public string MajorCode { get; set; }

    [JsonProperty("MatGrp")]
    public string MatGrp { get; set; }

    [JsonProperty("MaterialCode")]
    public string MaterialCode { get; set; }

    [JsonProperty("MGDesc")]
    public string MGDesc { get; set; }

    [JsonProperty("MinorCode")]
    public string MinorCode { get; set; }

    [JsonProperty("NetworkActivity")]
    public string NetworkActivity { get; set; }

    [JsonProperty("ObjSubna")]
    public string ObjSubna { get; set; }

    [JsonProperty("OPNumber")]
    public string OPNumber { get; set; }

    [JsonProperty("OperationCat")]
    public string OperationCat { get; set; }

    [JsonProperty("OperatorCoding")]
    public string OperatorCoding { get; set; }

    [JsonProperty("OrderNumber")]
    public string OrderNumber { get; set; }

    [JsonProperty("POApprover")]
    public string POApprover { get; set; }

    [JsonProperty("POLine")]
    public string POLine { get; set; }

    [JsonProperty("PONumber")]
    public string PONumber { get; set; }

    [JsonProperty("ProfitCentre")]
    public string ProfitCentre { get; set; }

    [JsonProperty("Project")]
    public string Project { get; set; }

    [JsonProperty("ProjectPONo")]
    public string ProjectPONo { get; set; }

    [JsonProperty("ReportCentre")]
    public string ReportCentre { get; set; }

    [JsonProperty("Requisitioner")]
    public string Requisitioner { get; set; }

    [JsonProperty("SerialNumber")]
    public string SerialNumber { get; set; }

    [JsonProperty("Signatory")]
    public string Signatory { get; set; }

    [JsonProperty("SOLine")]
    public string SOLine { get; set; }

    [JsonProperty("SONumber")]
    public string SONumber { get; set; }

    [JsonProperty("StkCond")]
    public string StkCond { get; set; }

    [JsonProperty("Sub")]
    public string Sub { get; set; }

    [JsonProperty("SubCode")]
    public string SubCode { get; set; }

    [JsonProperty("Subfeature")]
    public string Subfeature { get; set; }

    [JsonProperty("WBS")]
    public string WBS { get; set; }

    [JsonProperty("WellFacility")]
    public string WellFacility { get; set; }

    [JsonProperty("WONumber")]
    public string WONumber { get; set; }

    [JsonProperty("WorkOrderedBy")]
    public string WorkOrderedBy { get; set; }

    [JsonProperty("EDIZip4")]
    public string EDIZip4 { get; set; }
}
