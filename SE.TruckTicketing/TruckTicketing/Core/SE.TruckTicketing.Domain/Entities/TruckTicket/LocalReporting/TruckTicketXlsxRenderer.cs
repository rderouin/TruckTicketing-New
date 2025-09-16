using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.Reporting.NETCore;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.LocalReporting;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class TruckTicketXlsxRenderer : ITruckTicketXlsxRenderer
{
    private readonly IReportDefinitionResolver _reportDefinitionResolver;

    public TruckTicketXlsxRenderer(IReportDefinitionResolver reportDefinitionResolver)
    {
        _reportDefinitionResolver = reportDefinitionResolver;
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
        return localReport.Render("EXCELOPENXML");
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
            Class = truckTicket.ClassNumber,
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

        return localReport.Render("EXCELOPENXML");
    }

}
