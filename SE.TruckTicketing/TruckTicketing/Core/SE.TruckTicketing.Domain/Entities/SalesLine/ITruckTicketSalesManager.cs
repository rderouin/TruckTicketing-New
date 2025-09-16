using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Contracts;
using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public interface ITruckTicketSalesManager
{
    Task PersistTruckTicketAndSalesLines(TruckTicketEntity truckTicket, List<SalesLineEntity> salesLines);
}

public class TruckTicketSalesManager : ITruckTicketSalesManager
{
    private readonly IProvider<Guid, AccountEntity> _accountProvider;

    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly ITruckTicketInvoiceService _invoiceService;

    private readonly ITruckTicketLoadConfirmationService _loadConfirmationService;

    private readonly ILogger<TruckTicketSalesManager> _logger;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IManager<Guid, SalesLineEntity> _salesLineManager;

    private readonly ISalesLinesPublisher _salesLinesPublisher;

    private readonly IManager<Guid, TruckTicketEntity> _truckTicketManager;

    public TruckTicketSalesManager(IManager<Guid, TruckTicketEntity> truckTicketManager,
                                   IManager<Guid, SalesLineEntity> salesLineManager,
                                   IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                   IProvider<Guid, AccountEntity> accountProvider,
                                   ISalesLinesPublisher salesLinesPublisher,
                                   ITruckTicketInvoiceService invoiceService,
                                   ITruckTicketLoadConfirmationService loadConfirmationService,
                                   ILogger<TruckTicketSalesManager> logger,
                                   IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider)
    {
        _truckTicketManager = truckTicketManager;
        _salesLineManager = salesLineManager;
        _billingConfigurationProvider = billingConfigurationProvider;
        _accountProvider = accountProvider;
        _salesLinesPublisher = salesLinesPublisher;
        _invoiceService = invoiceService;
        _loadConfirmationService = loadConfirmationService;
        _logger = logger;
        _materialApprovalProvider = materialApprovalProvider;
    }

    public async Task PersistTruckTicketAndSalesLines(TruckTicketEntity truckTicket, List<SalesLineEntity> salesLines)
    {
        TryTransitioningTicketToOpenStatus(truckTicket);

        var additionalServicesQuantity = 0;

        foreach (var salesLine in salesLines)
        {
            salesLine.ApplyFoRounding();

            if (salesLine.IsAdditionalService)
            {
                salesLine.TruckTicketEffectiveDate = truckTicket.EffectiveDate;
                additionalServicesQuantity++;
            }
        }

        truckTicket.AdditionalServicesQty = additionalServicesQuantity;

        truckTicket.SalesLineIds = new() { List = salesLines.Select(salesLine => salesLine.Id.ToString()).ToList() };

        var existingTicket = await _truckTicketManager.GetById(truckTicket.Key); // PK - OK

        if (!truckTicket.MaterialApprovalId.Equals(Guid.Empty))
        {
            var existingMaterialApproval = await _materialApprovalProvider.GetById(truckTicket.MaterialApprovalId);
            UpdateTicketDowNonDow(truckTicket, existingMaterialApproval);
        }

        await _truckTicketManager.Save(truckTicket, true);

        var shouldUpdateSalesLines = !(truckTicket.Status is TruckTicketStatus.Void ||
                                       salesLines.Count == 0 ||
                                       !truckTicket.TicketNumber.HasText());

        if (shouldUpdateSalesLines)
        {
            UpdateSalesLineAttachments(truckTicket, salesLines);
            await TryApproveSalesLines(truckTicket, existingTicket, salesLines);
            await BulkUpdateSalesLines(truckTicket, existingTicket, salesLines);
            await UpdateCustomerLastTransactionTimestamp(truckTicket);
        }

        await _truckTicketManager.SaveDeferred();

        if (shouldUpdateSalesLines)
        {
            await _salesLinesPublisher.PublishSalesLines(salesLines);
        }
    }

    private async Task UpdateCustomerLastTransactionTimestamp(TruckTicketEntity truckTicket)
    {
        var account = await _accountProvider.GetById(truckTicket.BillingCustomerId);
        if (account is not null)
        {
            account.LastTransactionDate = DateTimeOffset.UtcNow;
            await _accountProvider.Update(account, true);
        }
    }

    private static void TryTransitioningTicketToOpenStatus(TruckTicketEntity truckTicket)
    {
        if (truckTicket.Status is TruckTicketStatus.Stub or TruckTicketStatus.New)
        {
            truckTicket.Status = TruckTicketStatus.Open;
        }
    }

    private static void UpdateTicketDowNonDow(TruckTicketEntity truckTicket, MaterialApprovalEntity materialApproval)
    {
        if (materialApproval != null)
        {
            TruckTicketSalesManagerHelper.SetHazardousNonHazardous(materialApproval, truckTicket);
        }
    }

    private async Task TryApproveSalesLines(TruckTicketEntity truckTicket, TruckTicketEntity existingTicket, List<SalesLineEntity> salesLines)
    {
        _logger.LogInformation("Running Sales Line Approval Process for {TicketNumber}", truckTicket.TicketNumber);

        var ticketWasJustApproved = existingTicket?.Status is TruckTicketStatus.Open or TruckTicketStatus.Hold && truckTicket.Status is TruckTicketStatus.Approved;

        if (!ticketWasJustApproved)
        {
            return;
        }

        var billingConfiguration = await _billingConfigurationProvider.GetById(truckTicket.BillingConfigurationId);

        var invoice = await AssignInvoice(truckTicket, billingConfiguration, salesLines);
        await AssignLoadConfirmation(truckTicket, billingConfiguration, salesLines, invoice);

        foreach (var salesLine in salesLines)
        {
            salesLine.DowNonDow = truckTicket.DowNonDow;
            salesLine.TruckTicketEffectiveDate = truckTicket.EffectiveDate;
            salesLine.Status = SalesLineStatus.Approved;
        }
    }

    private async Task AssignLoadConfirmation(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfig, List<SalesLineEntity> salesLines, InvoiceEntity invoice)
    {
        var loadConfirmation = await _loadConfirmationService.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice, salesLines.Count, salesLines.Sum(line => line.TotalValue));
        if (loadConfirmation is null)
        {
            return;
        }

        foreach (var salesLine in salesLines)
        {
            salesLine.LoadConfirmationId = loadConfirmation.Id;
            salesLine.LoadConfirmationNumber = loadConfirmation.Number;
        }
    }

    private async Task<InvoiceEntity> AssignInvoice(TruckTicketEntity truckTicket, BillingConfigurationEntity billingConfiguration, List<SalesLineEntity> salesLines)
    {
        var invoice = await _invoiceService.GetTruckTicketInvoice(truckTicket, billingConfiguration, salesLines.Count, salesLines.Sum(line => line.TotalValue));

        foreach (var salesLine in salesLines)
        {
            salesLine.InvoiceId = invoice.Id;
            salesLine.ProformaInvoiceNumber = invoice.ProformaInvoiceNumber;
        }

        return invoice;
    }

    private async Task<int> BulkUpdateSalesLines(TruckTicketEntity truckTicket, TruckTicketEntity existingTruckTicket, List<SalesLineEntity> salesLines)
    {
        if (salesLines.Count == 0)
        {
            return 0;
        }

        await CleanUpStaleSalesLines(truckTicket, existingTruckTicket, salesLines);

        for (var index = salesLines.Count - 1; index >= 0; index--)
        {
            var salesLine = salesLines[index];
            salesLine.TruckTicketId = truckTicket.Id;
            salesLine.DowNonDow = truckTicket.DowNonDow;
            salesLine.BillingConfigurationId = truckTicket.BillingConfigurationId;
            salesLine.BillingConfigurationName = truckTicket.BillingConfigurationName;
            salesLine.TruckTicketNumber = truckTicket.TicketNumber;

            await _salesLineManager.Save(salesLine, true);
        }

        return salesLines.Count;
    }

    private async Task CleanUpStaleSalesLines(TruckTicketEntity truckTicket, TruckTicketEntity existingTruckTicket, List<SalesLineEntity> salesLines)
    {
        if (existingTruckTicket is null)
        {
            return;
        }

        var currentSalesLineIds = salesLines.Select(salesLine => salesLine.Id).ToList();

        var salesLinesToRemove = await _salesLineManager.Get(salesLine => salesLine.TruckTicketId == truckTicket.Id && // PK - XP for SL by TT ID
                                                                          !salesLine.IsReversal && !salesLine.IsReversed &&
                                                                          !currentSalesLineIds.Contains(salesLine.Id));

        foreach (var salesLine in salesLinesToRemove)
        {
            // We void instead of delete because the data lake sync physical deletes is problematic. So we void to indicate removal instead.
            salesLine.Status = SalesLineStatus.Void;
            await _salesLineManager.Update(salesLine, true);
        }
    }

    private static void UpdateSalesLineAttachments(TruckTicketEntity truckTicket, List<SalesLineEntity> salesLines)
    {
        foreach (var salesLine in salesLines)
        {
            salesLine.Attachments = truckTicket.Attachments?
                                               .OrderByDescending(attachment => attachment.AttachmentType.ToString())
                                               .Select(attachment => new SalesLineAttachmentEntity
                                                {
                                                    AttachmentType = attachment.AttachmentType,
                                                    Container = attachment.Container,
                                                    Path = attachment.Path,
                                                    File = attachment.File,
                                                    Id = attachment.Id,
                                                })
                                               .ToList() ?? new();
        }
    }
}
