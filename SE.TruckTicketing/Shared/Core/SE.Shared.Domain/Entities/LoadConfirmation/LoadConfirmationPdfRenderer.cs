using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Reporting.NETCore;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Pdf;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

using Trident.Caching;
using Trident.Data.Contracts;

using Stream = System.IO.Stream;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public class LoadConfirmationPdfRenderer : ILoadConfirmationPdfRenderer
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly ITruckTicketUploadBlobStorage _blobStorage;

    private readonly ICachingManager _cachingManager;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigurationProvider;

    private readonly IPdfMerger _pdfMerger;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public LoadConfirmationPdfRenderer(ICachingManager cachingManager,
                                       IPdfMerger pdfMerger,
                                       IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                       IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigurationProvider,
                                       IProvider<Guid, FacilityEntity> facilityProvider,
                                       IProvider<Guid, AccountEntity> accountProvider,
                                       IProvider<Guid, SalesLineEntity> salesLineProvider,
                                       IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                       ITruckTicketUploadBlobStorage blobStorage)
    {
        _cachingManager = cachingManager;
        _pdfMerger = pdfMerger;
        _billingConfigurationProvider = billingConfigurationProvider;
        _invoiceConfigurationProvider = invoiceConfigurationProvider;
        _facilityProvider = facilityProvider;
        _accountProvider = accountProvider;
        _salesLineProvider = salesLineProvider;
        _truckTicketProvider = truckTicketProvider;
        _blobStorage = blobStorage;
    }

    public async Task<byte[]> RenderLoadConfirmationPdf(LoadConfirmationEntity loadConfirmation)
    {
        // collect data
        var dataSource = await LoadDataSource(loadConfirmation);

        // render the report
        var report = await RenderReportWithAttachmentsAsync(dataSource);

        // PDF with attachments
        return report;
    }

    public async Task<byte[]> RenderAdHocLoadConfirmationPdf(LoadConfirmationAdhocModel adhocModel)
    {
        // fetch sales lines
        var salesLines = (await _salesLineProvider.GetByIds(adhocModel.SalesLineKeys)).ToList(); // PK - OK

        // collect data
        var dataSource = await LoadDataSource(salesLines, adhocModel.AttachmentType);
        if (dataSource == null)
        {
            return null;
        }

        // render the report
        var report = await RenderReportWithAttachmentsAsync(dataSource);

        // PDF with attachments
        return report;
    }

    private Task<byte[]> RenderReportWithAttachmentsAsync(LoadConfirmationDataSource dataSource)
    {
        // the PDF merging handler
        var pdfMergingHandler = _pdfMerger.StartMerging();

        // render the main document if the datasource is given and not blank
        if (!dataSource.IsBlank)
        {
            // render the Load Confirmation
            var report = RenderReport(dataSource, "PDF");
            pdfMergingHandler.Append(report);
        }

        // append all provided attachments
        foreach (var attachment in dataSource.Attachments)
        {
            pdfMergingHandler.Append(attachment);
        }

        // PDF
        var mergedReport = pdfMergingHandler.ToByteArray();
        return Task.FromResult(mergedReport);
    }

    private Stream GetReportDefinition(LoadConfirmationDataSource dataSource)
    {
        // try getting cached version
        var rdl = _cachingManager.Get<byte[]>(dataSource.TemplateKey);
        if (rdl == null)
        {
            // read the raw data
            rdl = File.ReadAllBytes($"ReportDefinitions/{dataSource.TemplateKey}.rdl");

            // cache it
            _cachingManager.Set(dataSource.TemplateKey, rdl);
        }

        // a stream containing the report
        return new MemoryStream(rdl);
    }

    private byte[] RenderReport(LoadConfirmationDataSource dataSource, string format)
    {
        // load the definition
        var report = new LocalReport();
        report.LoadReportDefinition(GetReportDefinition(dataSource));

        // set parameters
        var parameters = dataSource.Parameters.Select(p => new ReportParameter(p.Key, p.Value)).ToList();
        report.SetParameters(parameters);

        // attach data sources
        foreach (var dataSet in dataSource.DataSets)
        {
            report.DataSources.Add(new(dataSet.Key, dataSet.Value));
        }

        // render the report
        return report.Render(format);
    }

    private async Task<LoadConfirmationDataSource> LoadDataSource(LoadConfirmationEntity loadConfirmation)
    {
        // load extra data for this report
        var billingConfiguration = await _billingConfigurationProvider.GetById(loadConfirmation.BillingConfigurationId);
        var facility = await _facilityProvider.GetById(loadConfirmation.FacilityId);
        var account = await _accountProvider.GetById(loadConfirmation.BillingCustomerId);

        // load sales lines
        var salesLines = (await _salesLineProvider.Get(e => e.LoadConfirmationId == loadConfirmation.Id)).ToList(); // PK - XP for SL by LC ID

        return await LoadConfirmationDataSource.Create(loadConfirmation, billingConfiguration, facility, account, salesLines, _blobStorage, null);
    }

    private async Task<LoadConfirmationDataSource> LoadDataSource(List<SalesLineEntity> salesLines, AttachmentIndicatorType attachmentType)
    {
        // no SLs = no report
        if (salesLines.Count < 1)
        {
            return null;
        }

        // fetch corresponding truck tickets
        var truckTicketIds = salesLines.Select(sl => sl.TruckTicketId).ToHashSet();
        var truckTickets = (await _truckTicketProvider.GetByIds(truckTicketIds)).ToList(); // PK - TODO: ENTITY or INDEX

        // must have a single facility
        var facilityIds = salesLines.Select(sl => sl.FacilityId).ToHashSet();
        if (facilityIds.Count != 1)
        {
            return null;
        }

        // must have a single customer
        var customerIds = salesLines.Select(sl => sl.CustomerId).ToHashSet();
        if (customerIds.Count != 1)
        {
            return null;
        }

        // must have a single billing configuration
        var billingConfigurationIds = truckTickets.Select(tt => tt.BillingConfigurationId).ToHashSet();
        if (billingConfigurationIds.Count != 1)
        {
            return null;
        }

        var facilityId = facilityIds.First();
        var customerId = customerIds.First();
        var billingConfigurationId = billingConfigurationIds.First();

        // load extra data for this report
        var billingConfiguration = await _billingConfigurationProvider.GetById(billingConfigurationId);
        var facility = await _facilityProvider.GetById(facilityId);
        var account = await _accountProvider.GetById(customerId);

        // mimic an LC
        var lc = new LoadConfirmationEntity();
        lc.UpdateEffectiveDateRange(salesLines);

        // create a DS
        return await LoadConfirmationDataSource.Create(lc, billingConfiguration, facility, account, salesLines, _blobStorage, attachmentType);
    }
}
