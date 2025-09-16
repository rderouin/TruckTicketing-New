using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Humanizer;

using Microsoft.Reporting.NETCore;

using NetBarcode;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.LocalReporting;

using Type = NetBarcode.Type;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class TruckTicketPdfRenderer : ITruckTicketPdfRenderer
{
    private readonly IReportDefinitionResolver _reportDefinitionResolver;

    public TruckTicketPdfRenderer(IReportDefinitionResolver reportDefinitionResolver)
    {
        _reportDefinitionResolver = reportDefinitionResolver;
    }

    public byte[] RenderTruckTicketStubs(ICollection<TruckTicketEntity> tickets)
    {
        var ticket = tickets.FirstOrDefault();
        if (ticket is null)
        {
            return Array.Empty<byte>();
        }

        var isScaleTicket = ticket.TicketNumber.ToUpper().EndsWith("LF");

        return isScaleTicket ? RenderScaleTicketStubs(tickets) : RenderWorkTicketStubs(tickets);
    }

    public byte[] RenderMaterialApprovalScaleTicket(MaterialApprovalEntity materialApproval, FacilityEntity facility)
    {
        var reportItem = new ScaleTicketJournalItem
        {
            MaterialApprovalNumber = materialApproval?.MaterialApprovalNumber,
            RigNumber = materialApproval?.RigNumber,
            SecureFacilityLSD = facility?.LocationCode,
            SecureEmail = facility?.AdminEmail,
            CountryCode = materialApproval?.CountryCode ?? CountryCode.Undefined,
            FacilityName = materialApproval?.Facility,
            TruckingCompanyName = materialApproval?.TruckingCompanyName,
            ProductCharacterization = materialApproval?.SubstanceName,
            GeneratorName = materialApproval?.GeneratorName,
            SourceLocationProductReceivedFrom = materialApproval?.SourceLocationFormattedIdentifier.HasText() ?? false
                                                    ? materialApproval.SourceLocationFormattedIdentifier
                                                    : materialApproval?.SourceLocation,
        };

        return RenderPdfReport(new[] { reportItem }, true);
    }

    public byte[] RenderScaleTicket(TruckTicketEntity truckTicket, MaterialApprovalEntity materialApproval, FacilityEntity facility, string signature)
    {
        var materialApprovalSignatories = materialApproval?.ApplicantSignatories?.Where(x => x.ReceiveLoadSummary).ToList();
        var reportItem = new ScaleTicketJournalItem
        {
            MaterialApprovalNumber = truckTicket?.MaterialApprovalNumber,
            ProductCharacterization = materialApproval?.SubstanceName,
            RigNumber = materialApproval?.RigNumber,
            CountryCode = facility?.CountryCode ?? CountryCode.Undefined,
            FacilityName = truckTicket?.FacilityName,
            TruckingCompanyName = truckTicket?.TruckingCompanyName,
            SourceLocationProductReceivedFrom = truckTicket?.SourceLocationFormatted.HasText() ?? false ? truckTicket.SourceLocationFormatted : truckTicket?.SourceLocationName,
            BillOfLadingNumber = truckTicket?.BillOfLading,
            Class = truckTicket?.WellClassification.ToString(),
            ManifestNumber = truckTicket?.ManifestNumber,
            NetWeight = truckTicket?.NetWeight.ToString("N2"),
            GrossWeight = truckTicket?.GrossWeight.ToString("N2"),
            TareWeight = truckTicket?.TareWeight.ToString("N2"),
            GeneratorName = truckTicket?.GeneratorName,
            TimeIn = truckTicket?.TimeIn?.ToString("hh:mm tt"),
            TimeOut = truckTicket?.TimeOut?.ToString("hh:mm tt"),
            TnormsLevel = truckTicket?.Tnorms,
            TruckUnitNumber = truckTicket?.TruckNumber,
            SecureFacilityLSD = facility?.LocationCode,
            SecureFacilityName = facility?.Name,
            SecureEmail = facility?.AdminEmail,
            Date = truckTicket?.LoadDate?.ToString("ddd, dd MMM yyy"),
            TicketNumber = truckTicket?.TicketNumber,
            TicketNumberBarcode = new Barcode(truckTicket?.TicketNumber, Type.Code39, false, 1000, 100).GetBase64Image(),
            SecureRepSignature = signature,
            SecurePhone = facility?.InvoiceContactPhoneNumber,
            Signatory1Name = materialApprovalSignatories?.FirstOrDefault()?.SignatoryName,
            Signatory1Email = materialApprovalSignatories?.FirstOrDefault()?.Email,
            Signatory1Phone = materialApprovalSignatories?.FirstOrDefault()?.PhoneNumber,
            Signatory2Name = materialApprovalSignatories?.Skip(1).FirstOrDefault()?.SignatoryName,
            Signatory2Email = materialApprovalSignatories?.Skip(1).FirstOrDefault()?.Email,
            Signatory2Phone = materialApprovalSignatories?.Skip(1).FirstOrDefault()?.PhoneNumber,
        };

        return RenderPdfReport(new[] { reportItem }, true);
    }

    public byte[] RenderWorkTicket(TruckTicketEntity truckTicket)
    {
        var truckTicketData = new WorkTicketJournalItem(truckTicket);
        truckTicketData.TicketNumberBarcode = new Barcode(truckTicketData.TicketNumber, Type.Code39, false, 1000, 100).GetBase64Image();
        return RenderPdfReport(new[] { truckTicketData }, true);
    }

    public byte[] RenderFstDailyReport(IList<TruckTicketEntity> truckTickets, FSTWorkTicketRequest fstReportParameters)
    {
        var fstDailyReportItems = truckTickets.Where(ticket => ticket.IsServiceOnlyTicket != true).Select(truckTicket =>
                                                                                                              new FstDailyReportItem(truckTicket)
                                                                                                              {
                                                                                                                  ShipDate = truckTicket.LoadDate.Value.ToString("d", CultureInfo.InvariantCulture),
                                                                                                                  TicketNumber = truckTicket.TicketNumber,
                                                                                                                  Total = truckTicket.TotalVolume.ToString("N2"),
                                                                                                                  Oil = truckTicket.OilVolume.ToString("N2"),
                                                                                                                  OilPercent = truckTicket.OilVolumePercent.ToString("N2"),
                                                                                                                  Water = truckTicket.WaterVolume.ToString("N2"),
                                                                                                                  WaterPercent = truckTicket.WaterVolumePercent.ToString("N2"),
                                                                                                                  Solids = truckTicket.SolidVolume.ToString("N2"),
                                                                                                                  SolidsPercent = truckTicket.SolidVolumePercent.ToString("N2"),
                                                                                                                  OilDensity = truckTicket.UnloadOilDensity.ToString("N2"),
                                                                                                                  WellClass = truckTicket.WellClassification.ToString(),
                                                                                                                  Substance = truckTicket.SubstanceName,
                                                                                                                  WasteCode = truckTicket.WasteCode,
                                                                                                                  MaterialApprovalNumber = truckTicket.MaterialApprovalNumber,
                                                                                                                  Tenorm = truckTicket.Tnorms,
                                                                                                                  TruckingCompany = truckTicket.TruckingCompanyName,
                                                                                                                  BillOfLading = truckTicket.BillOfLading,
                                                                                                                  ManifestNumber = truckTicket.ManifestNumber,
                                                                                                                  DOW = truckTicket.DowNonDow.ToString(),
                                                                                                                  Destination = truckTicket.Destination,
                                                                                                                  AdditionalServicesQty = truckTicket.AdditionalServicesQty?.ToString(),
                                                                                                                  LCFrequency =
                                                                                                                      truckTicket.LoadConfirmationFrequency == null
                                                                                                                          ? "None"
                                                                                                                          : truckTicket.LoadConfirmationFrequency?.ToString(),
                                                                                                                  ServiceType = truckTicket.ServiceType,
                                                                                                                  GeneratorName = truckTicket.GeneratorName,
                                                                                                                  SourceLocation =
                                                                                                                      truckTicket.SourceLocationFormatted.HasText()
                                                                                                                          ? truckTicket.SourceLocationFormatted
                                                                                                                          : truckTicket.SourceLocationName,
                                                                                                                  CustomerName = truckTicket.CustomerName,
                                                                                                                  BillingCustomer = truckTicket.BillingCustomerName,
                                                                                                                  BillingConfigName = truckTicket.BillingConfigurationName,
                                                                                                              }).ToArray();

        if (!fstDailyReportItems.Any())
        {
            return Array.Empty<byte>();
        }

        var sampleItem = fstDailyReportItems.First();
        var localReport = new LocalReport
        {
            DataSources =
            {
                new("FSTParameters", new[]
                {
                    new
                    {
                        FacilitiesName = fstReportParameters.FacilityName,
                        fstReportParameters.LegalEntityName,
                        ServiceTypeName = fstReportParameters.ServiceTypeName.HasText() ? fstReportParameters.ServiceTypeName : "ALL",
                        fstReportParameters.FromDate,
                        fstReportParameters.ToDate,
                        TruckCount = fstDailyReportItems.Length,
                    },
                }),
                new(sampleItem.DataSourceName, fstDailyReportItems),
            },
        };

        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(sampleItem));
        return localReport.Render("PDF");
    }

    public byte[] RenderLoadSummaryTicket(IEnumerable<TruckTicketEntity> truckTickets,
                                          Dictionary<Guid, MaterialApprovalEntity> materialApprovalEntities,
                                          LoadSummaryReportRequest request,
                                          FacilityEntity facility)
    {
        var loadSummaryTicketItems = new List<LoadSummaryTicketItem>();

        foreach (var truckTicket in truckTickets)
        {
            var materialApproval = truckTicket.MaterialApprovalId != default ? materialApprovalEntities[truckTicket.MaterialApprovalId] : null;
            var ticketItem = new LoadSummaryTicketItem(truckTicket)
            {
                TransactionDate = truckTicket.LoadDate?.ToString("MM/dd/yyyy"),
                TicketNumber = truckTicket.TicketNumber,
                Substance = truckTicket.SubstanceName,
                WasteCode = materialApproval.WasteCodeName,
                MaterialApprovalNumber = truckTicket.MaterialApprovalNumber,
                TruckingCompanyName = truckTicket.TruckingCompanyName,
                BillOfLading = truckTicket.BillOfLading,
                ManifestNumber = truckTicket.ManifestNumber,
                PO = truckTicket.EdiFieldValues?.Where(x => x.EDIFieldName == "PO").Select(y => y.EDIFieldValueContent).FirstOrDefault(),
                AFE = truckTicket.EdiFieldValues?.Where(x => x.EDIFieldName == "AFE").Select(y => y.EDIFieldValueContent).FirstOrDefault(),
                BillingCustomer = truckTicket.BillingCustomerName,
                SourceLocation = truckTicket?.SourceLocationFormatted.HasText() ?? false ? truckTicket.SourceLocationFormatted : truckTicket?.SourceLocationName,
                GrossWeight = truckTicket.GrossWeight,
                NetWeight = truckTicket.NetWeight,
                TareWeight = truckTicket.TareWeight,
                GeneratorName = truckTicket.GeneratorName,
                Unit = truckTicket.TruckNumber,
                TrackingNumber = truckTicket.TrackingNumber,
                TimeIn = truckTicket.TimeIn?.ToString("hh:mm tt"),
                TimeOut = truckTicket.TimeOut?.ToString("hh:mm tt"),
            };

            loadSummaryTicketItems.Add(ticketItem);
        }

        var materialApprovalNames = String.Join(",", materialApprovalEntities.Values.Select(s => s.MaterialApprovalNumber));
        var truckingCompanyNames = request.TruckingCompanyNames.Any() ? String.Join(",", request.TruckingCompanyNames) : "None";
        var signatories = new StringBuilder();
        var names = new List<string>();

        foreach (var materialApproval in materialApprovalEntities.Values)
        {
            names.AddRange(materialApproval.ApplicantSignatories.Where(signatory => signatory.ReceiveLoadSummary).Select(a => a.SignatoryName));
        }

        signatories.AppendJoin(",", names);

        var reportParameters = new
        {
            FacilityName = String.IsNullOrEmpty(request.FacilityName) ? "None" : request.FacilityName,
            request.FromDate,
            request.ToDate,
            SourceLocation = String.IsNullOrEmpty(request.SourceLocationName) ? "None" : request.SourceLocationName,
            materialApprovalNames,
            TruckingCompany = truckingCompanyNames,
            Company = String.IsNullOrEmpty(request.LegalEntity) ? "None" : request.LegalEntity.ToUpper(),
            LocationCode = String.IsNullOrEmpty(facility?.LocationCode) ? "" : facility?.LocationCode,
            SignatoryNames = signatories.ToString(),
            ReportExecutionTime = request.ReportExecutionTime ?? DateTime.Now,
        };

        if (!loadSummaryTicketItems.Any())
        {
            return Array.Empty<byte>();
        }

        var sampleTicket = loadSummaryTicketItems.First();
        sampleTicket.CountryCode = CountryCode.Undefined;
        var localReport = new LocalReport
        {
            DataSources =
            {
                new("LoadSummaryParameters", new[] { reportParameters }),
                new(sampleTicket.DataSourceName, loadSummaryTicketItems),
            },
        };

        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(sampleTicket));
        return localReport.Render("PDF");
    }

    public byte[] RenderMaterialApprovalPdf(MaterialApprovalEntity materialApproval,
                                            Dictionary<Guid, AccountEntity> accounts,
                                            Dictionary<Guid, AccountContactEntity> accountsContacts,
                                            string signature,
                                            string facilityLocationCode,
                                            bool isFST)
    {
        var billingContact = materialApproval.BillingCustomerContactId != default ? accountsContacts[materialApproval.BillingCustomerContactId.Value] : null;
        var thirdParty = materialApproval.ThirdPartyAnalyticalCompanyContactId != default ? accountsContacts[materialApproval.ThirdPartyAnalyticalCompanyContactId.Value] : null;

        var billingCustomer = materialApproval.BillingCustomerId != default ? accounts[materialApproval.BillingCustomerId] : null;
        var generator = materialApproval.GeneratorId != default ? accounts[materialApproval.GeneratorId] : null;
        var generatorRep = materialApproval.GeneratorRepresenativeId != default ? accountsContacts[materialApproval.GeneratorRepresenativeId] : null;

        var reportItem = new MaterialApprovalItem(materialApproval)
        {
            AFE = materialApproval.AFE,
            MaterialApprovalNumber = materialApproval.MaterialApprovalNumber,
            RigNumber = materialApproval?.RigNumber,
            CountryCode = materialApproval?.CountryCode ?? CountryCode.Undefined,
            FacilityName = materialApproval?.Facility,
            Date = materialApproval?.SignatureDate.ToString(" dd MMM yyy"),
            AnalyticalExpiryDate = materialApproval.AnalyticalExpiryDate.ToString(" dd MMM yyy"),
            BillingCustomerName = materialApproval.BillingCustomerName,
            BillingCustomerEmail = billingCustomer?.AccountPrimaryContactEmail,
            BillingCustomerPhone = billingCustomer?.AccountPrimaryContactPhoneNumber,
            BillingContactAddress = materialApproval.BillingCustomerContactAddress,
            BillingContactEmail = billingContact?.Email,
            BillingContactName = $"{billingContact?.Name} {billingContact?.LastName}",
            BillingContactPhone = billingContact?.PhoneNumber,
            DisposalUnit = materialApproval.DisposalUnits,
            EDICode = materialApproval.EDICode,
            GeneratorEmail = generatorRep?.Email,
            RepresentativeName = materialApproval.GeneratorRepresentative,
            GeneratorName = materialApproval.GeneratorName,
            GeneratorPhone = generatorRep?.PhoneNumber,
            Hazardous = materialApproval.HazardousNonhazardous == HazardousClassification.Hazardous,
            LabIdNumber = materialApproval.LabIdNumber,
            LegalEntity = materialApproval.LegalEntity,
            PO = materialApproval.PO,
            SourceLocation = materialApproval.SourceLocationFormattedIdentifier,
            SecureRepName = materialApproval.SecureRepresentative,
            TruckingCompanyName = materialApproval.TruckingCompanyName,
            SubstanceName = materialApproval.SubstanceName,
            TenormHaulerNumber = materialApproval.TenormWasteHaulerPermitNumber,
            ThirdPartyName = materialApproval.ThirdPartyAnalyticalCompanyName,
            ThirdPartyRepresentativeName = $"{thirdParty?.Name} {thirdParty?.LastName}",
            ThirdPartyPhone = thirdParty?.PhoneNumber,
            ThirdPartyEmail = thirdParty?.Email,
            DownholeSurface = materialApproval.DownHoleType == DownHoleType.Undefined ? String.Empty : materialApproval.DownHoleType.ToString(),
            SiteId = materialApproval.Facility,
            SecureRepSignature = signature,
            FacilityLocationCode = facilityLocationCode,
        };

        var signatoryItems = materialApproval.ApplicantSignatories.Where(signatory => signatory.ReceiveLoadSummary).Select(signatory => new SignatoryItem
                                              {
                                                  SignatoryName = signatory.SignatoryName,
                                                  SignatoryEmail = signatory.Email,
                                                  SignatoryPhone = signatory.PhoneNumber,
                                              })
                                             .ToList();

        reportItem.CountryCode = CountryCode.Undefined;
        if (isFST)
        {
            reportItem.ReportName = "MaterialApprovalFST";
        }

        var sampleTicket = reportItem;

        var localReport = new LocalReport();
        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(sampleTicket));
        localReport.DataSources.Add(new(sampleTicket.DataSourceName, new[] { reportItem }));
        localReport.DataSources.Add(new("SignatoryItem", signatoryItems));
        return localReport.Render("PDF");
    }

    public byte[] RenderProducerReport(List<TruckTicketEntity> truckTickets, ProducerReportRequest request)
    {
        var nonLandFillFacilityItems = truckTickets.Where(truckTicket => truckTicket.FacilityType != FacilityType.Lf).Select(truckTicket => new ProducerReportItem(truckTicket)
        {
            BatteryCode = truckTicket.SourceLocationCode,
            BillOfLading = truckTicket.BillOfLading,
            NetPrice = truckTicket.SalesTotalValue?.ToString("N2"),
            Oil = truckTicket.OilVolume.ToString("N2"),
            OilDensity = truckTicket.UnloadOilDensity.ToString("N2"),
            OilPercentage = truckTicket.OilVolumePercent.ToString("N2"),
            OperatorName = truckTicket.GeneratorName,
            ServiceTypeName = truckTicket.ServiceType,
            ShipDate = truckTicket.LoadDate?.ToString("MM/dd/yy"),
            StreamCode = truckTicket.FacilityStreamRegulatoryCode,
            Solid = truckTicket.SolidVolume.ToString("N2"),
            SolidPercentage = truckTicket.SolidVolumePercent.ToString("N2"),
            SourceLocation = truckTicket.SourceLocationFormatted.HasText() ? truckTicket.SourceLocationFormatted : truckTicket.SourceLocationName,
            TicketNumber = truckTicket.TicketNumber,
            TimeIn = truckTicket.CountryCode == CountryCode.US ? "" : truckTicket.TimeIn?.ToString("hh:mm tt"),
            TimeOut = truckTicket.CountryCode == CountryCode.US ? "" : truckTicket.TimeOut?.ToString("hh:mm tt"),
            TruckCompany = truckTicket.TruckingCompanyName,
            TruckNumber = truckTicket.TicketNumber,
            TruckUnit = truckTicket.TrailerNumber,
            Total = truckTicket.TotalVolume.ToString("N2"),
            WasteCode = truckTicket.WasteCode,
            Water = truckTicket.WaterVolume.ToString("N2"),
            WaterPercentage = truckTicket.WaterVolumePercent.ToString("N2"),
        }).ToList();
        
        /* Solid and Total values are populated different for Landfill facilities. */
        var landFillFacilityItems = truckTickets.Where(truckTicket => truckTicket.FacilityType == FacilityType.Lf).Select(truckTicket => new ProducerReportItem(truckTicket)
        {
            BatteryCode = truckTicket.SourceLocationCode,
            BillOfLading = truckTicket.BillOfLading,
            NetPrice = truckTicket.SalesTotalValue?.ToString("N2"),
            Oil = truckTicket.OilVolume.ToString("N2"),
            OilDensity = truckTicket.UnloadOilDensity.ToString("N2"),
            OilPercentage = truckTicket.OilVolumePercent.ToString("N2"),
            OperatorName = truckTicket.GeneratorName,
            ServiceTypeName = truckTicket.ServiceType,
            ShipDate = truckTicket.LoadDate?.ToString("MM/dd/yy"),
            StreamCode = truckTicket.FacilityStreamRegulatoryCode,
            Solid = truckTicket.NetWeight.ToString("N2"),
            SolidPercentage = truckTicket.SolidVolumePercent.ToString("N2"),
            SourceLocation = truckTicket.SourceLocationFormatted.HasText() ? truckTicket.SourceLocationFormatted : truckTicket.SourceLocationName,
            TicketNumber = truckTicket.TicketNumber,
            TimeIn = truckTicket.TimeIn?.ToString("hh:mm tt"),
            TimeOut = truckTicket.TimeOut?.ToString("hh:mm tt"),
            TruckCompany = truckTicket.TruckingCompanyName,
            TruckNumber = truckTicket.TicketNumber,
            TruckUnit = truckTicket.TrailerNumber,
            Total = truckTicket.NetWeight.ToString("N2"),
            WasteCode = truckTicket.WasteCode,
            Water = truckTicket.WaterVolume.ToString("N2"),
            WaterPercentage = truckTicket.WaterVolumePercent.ToString("N2"),
        }).ToList();
        
        var producerReportItems = new List<ProducerReportItem>();
        producerReportItems.AddRange(nonLandFillFacilityItems);
        producerReportItems.AddRange(landFillFacilityItems);
        
        if (!producerReportItems.Any())
        {
            return Array.Empty<byte>();
        }

        var firstItem = producerReportItems.First();
        firstItem.CountryCode = CountryCode.Undefined;
        
        var facilityNames = truckTickets.Select(ticket => ticket.FacilityName).Distinct().ToArray();
        var generatorNames = truckTickets.Select(ticket => ticket.GeneratorName).Distinct().ToArray();
        var facilityLocationCodes = truckTickets.Select(ticket => ticket.FacilityLocationCode).Distinct().ToArray();
        var legalEntities = truckTickets.Select(ticket => ticket.LegalEntity).Distinct().ToArray();

        var parameters = new ProducerReportParameters
        {
            Facilities = facilityNames.Length == 1 ? facilityNames[0] : "*",
            FromDate = request.FromDate.ToString("d"),
            ToDate = request.ToDate.ToString("d"),
            Generators = generatorNames.Length == 1 ? generatorNames[0] : "*",
            PriceOnLoad = request.PriceOnLoad,
            FacilityLocationCode = facilityLocationCodes.Length == 1 ? facilityLocationCodes[0] : "*",
            LegalEntity = legalEntities.Length == 1 ? legalEntities[0] : "*",
        };

        var localReport = new LocalReport
        {
            DataSources =
            {
                new("ProducerParameters", new[] { parameters }),
                new(firstItem.DataSourceName, producerReportItems),
            },
        };

        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(firstItem));
        return localReport.Render("PDF");
    }

    public byte[] RenderLandfillDailyTicket(List<TruckTicketEntity> truckTickets, LandfillDailyReportRequest request)
    {
        var landfillReportItems = truckTickets.Where(tt => tt.IsServiceOnlyTicket != true).Select(truckTicket => new LandfillDailyTicketItem
        {
            ShipDate = truckTicket.LoadDate?.ToString("MM/dd/yyyy"),
            CellCoord = $"{truckTicket.Quadrant}/{truckTicket.Level}",
            ScaleTicket = truckTicket.TicketNumber,
            NetWeight = truckTicket.NetWeight.ToString("N2"),
            Unit = truckTicket.UnitOfMeasure,
            SubstanceName = truckTicket.SubstanceName,
            WasteCode = truckTicket.WasteCode,
            BillOfLading = truckTicket.BillOfLading,
            TruckingCompany = truckTicket.TruckingCompanyName,
            TruckUnitNumber = truckTicket.TruckNumber,
            TimeIn = truckTicket.TimeIn?.ToString("hh:mm tt"),
            TimeOut = truckTicket.TimeOut?.ToString("hh:mm tt"),
            ManifestNumber = truckTicket.ManifestNumber,
            Tenorm = truckTicket.Tnorms,
            AdditionalServiceQuantity = truckTicket.AdditionalServicesQty?.ToString(),
            Class = truckTicket.ServiceTypeClass?.Humanize() ?? string.Empty,
            GeneratorName = truckTicket.GeneratorName,
            BillingCustomer = truckTicket.BillingCustomerName,
            SourceLocation = truckTicket.SourceLocationFormatted.HasText() ? truckTicket.SourceLocationFormatted : truckTicket.SourceLocationName,
            LegalEntity = truckTicket.LegalEntity,
            MaterialApprovalNumber = truckTicket.MaterialApprovalNumber,
            BillingConfigName = truckTicket.BillingConfigurationName,
            CountryCode = truckTicket.CountryCode,
        }).ToArray();

        if (!landfillReportItems.Any())
        {
            return Array.Empty<byte>();
        }

        var sampleTicket = landfillReportItems.First();
        var IsTnormHidden = (sampleTicket.CountryCode == CountryCode.CA).ToString();

        sampleTicket.CountryCode = CountryCode.Undefined;
        var localReport = new LocalReport();
        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(sampleTicket));
        localReport.DataSources.Add(new(sampleTicket.DataSourceName, landfillReportItems));
        localReport.SetParameters(new List<ReportParameter>
        {
            new("Facilities", truckTickets.Select(truckTicket => truckTicket.FacilityName).Distinct().OrderBy(name => name).ToArray()),
            new("FromDate", request.FromDate.ToString("D")),
            new("ToDate", request.ToDate.ToString("D")),
            new("IsTenormHidden", IsTnormHidden),
        });

        return localReport.Render("PDF");
    }

    private byte[] RenderPdfReport<TReportItem>(ICollection<TReportItem> tickets, bool enableExternalImages = false)
        where TReportItem : TicketJournalItem
    {
        if (!tickets.Any())
        {
            return Array.Empty<byte>();
        }

        var sampleTicket = tickets.First();

        var localReport = new LocalReport();
        localReport.LoadReportDefinition(_reportDefinitionResolver.GetReportDefinition(sampleTicket));
        localReport.EnableExternalImages = enableExternalImages;
        localReport.DataSources.Add(new(sampleTicket.DataSourceName, tickets));
        return localReport.Render("PDF");
    }

    private byte[] RenderScaleTicketStubs(ICollection<TruckTicketEntity> tickets)
    {
        var data = tickets.Select(ticket => new ScaleTicketJournalItem(ticket)).ToList();
        data.ForEach(item => item.TicketNumberBarcode = new Barcode(item.TicketNumber, Type.Code39, false, 1000, 100).GetBase64Image());
        return RenderPdfReport(data.ToList(), true);
    }

    private byte[] RenderWorkTicketStubs(ICollection<TruckTicketEntity> tickets)
    {
        var data = tickets.Select(ticket => new WorkTicketJournalItem(ticket)).ToList();
        data.ForEach(item => item.TicketNumberBarcode = new Barcode(item.TicketNumber, Type.Code39, false, 1000, 100).GetBase64Image());
        return RenderPdfReport(data.ToList(), true);
    }
}
