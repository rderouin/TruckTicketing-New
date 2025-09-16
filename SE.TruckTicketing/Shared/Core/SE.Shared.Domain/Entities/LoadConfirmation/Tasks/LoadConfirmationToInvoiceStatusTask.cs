using System;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.LoadConfirmation.Tasks;

/// <summary>
///     This task updates a flag on the referenced invoice to indicate if all load confirmations have approvals.
/// </summary>
public class LoadConfirmationToInvoiceStatusTask : WorkflowTaskBase<BusinessContext<LoadConfirmationEntity>>
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly ILog _log;

    public LoadConfirmationToInvoiceStatusTask(IProvider<Guid, InvoiceEntity> invoiceProvider,
                                               IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                                               IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                               ILog log)
    {
        _invoiceProvider = invoiceProvider;
        _loadConfirmationProvider = loadConfirmationProvider;
        _billingConfigurationProvider = billingConfigurationProvider;
        _log = log;
    }

    public override int RunOrder => 200;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> ShouldRun(BusinessContext<LoadConfirmationEntity> context)
    {
        // do not run updates on terminal statuses
        var terminalOriginal = context.Original?.Status is LoadConfirmationStatus.Void or LoadConfirmationStatus.Posted;
        var terminalTarget = context.Target?.Status is LoadConfirmationStatus.Void or LoadConfirmationStatus.Posted;
        var fromTerminalToTerminal = terminalOriginal && terminalTarget;

        return Task.FromResult(!fromTerminalToTerminal);
    }

    public override async Task<bool> Run(BusinessContext<LoadConfirmationEntity> context)
    {
        // NOTE:
        // - any LC operation might lead to the flag change on the invoice
        // - deleting or voiding and LC should cause full re-evaluation of the flag on an invoice
        // - if the current LC doesn't have a flag, no need to check other LCs
        // - if the current LC has conditions met, then check other LCs for same approval

        // which invoice to update:
        // - for inserts and updates, this is on the target object
        // - for deletes, this is on the original object
        var targetInvoiceId = context.Target?.InvoiceId ?? context.Original?.InvoiceId ?? default(Guid);
        if (targetInvoiceId == default)
        {
            // no invoice id ?
            return true;
        }

        // fetch the invoice and update the flag if changed
        var invoice = await _invoiceProvider.GetById(targetInvoiceId);
        if (invoice == null)
        {
            return true;
        }

        // find the customer
        var billingConfiguration = await _billingConfigurationProvider.GetById(invoice.BillingConfigurationId);

        // figure out the new flag
        var allApproved = !billingConfiguration.IsSignatureRequired || await GetAllLoadConfirmationsApprovalFlag(context, targetInvoiceId);
        if (allApproved != invoice.HasAllLoadConfirmationApprovals)
        {
            // update with the new flag and persist
            invoice.HasAllLoadConfirmationApprovals = allApproved;
            await _invoiceProvider.Update(invoice, true);
        }

        // all good
        return true;
    }

    private async Task<bool> GetAllLoadConfirmationsApprovalFlag(BusinessContext<LoadConfirmationEntity> context, Guid targetInvoiceId)
    {
        // upon deleting an LC, it does no longer participate in the approval check, i.e. automatic approval granted from this LC
        // for inserts and updates, check the approval status for the LC
        var currentLcHasApproval = context.Operation == Operation.Delete || HasApproval(context.Target);

        // if the current LC is not approved, no need to check the rest of the LCs
        if (currentLcHasApproval == false)
        {
            // not approved
            return false;
        }

        // fetch all related LCs
        var currentLcId = context.Target?.Id ?? context.Original?.Id;
        var allLcsForInvoice = await _loadConfirmationProvider.Get(lc => lc.InvoiceId == targetInvoiceId &&
                                                                         lc.Id != currentLcId &&
                                                                         lc.Status != LoadConfirmationStatus.Void &&
                                                                         lc.Status != LoadConfirmationStatus.Posted);

        // check all related LCs for approvals, if all have approvals, then the invoice is good to be submitted
        return allLcsForInvoice.All(HasApproval);
    }

    private bool HasApproval(LoadConfirmationEntity lc)
    {
        // theoretically, this condition should never be met
        if (lc == null)
        {
            _log.Warning(messageTemplate: "A load confirmation is not provided for the approval check.");
            return false;
        }

        // a load confirmation in Void or Posted statuses are skipped from the check and considered as good
        if (lc.Status is LoadConfirmationStatus.Void or LoadConfirmationStatus.Posted)
        {
            return true;
        }

        // the Waiting For Invoice status is the last status before submitting an Invoice
        // and hence we need to check if it has an attachment that is going to be included in the invoice
        if (lc.Status is LoadConfirmationStatus.WaitingForInvoice)
        {
            return lc.Attachments.Any(a => a.IsIncludedInInvoice == true);
        }

        // all the remaining load confirmations automatically not approved
        return false;
    }
}
