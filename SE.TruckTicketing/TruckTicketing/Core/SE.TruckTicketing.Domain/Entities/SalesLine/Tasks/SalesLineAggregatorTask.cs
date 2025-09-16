using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.SalesLine;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.SalesLine.Tasks;

public class SalesLineAggregatorTask : WorkflowTaskBase<BusinessContext<SalesLineEntity>>
{
    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly IProvider<Guid, LoadConfirmationEntity> _loadConfirmationProvider;

    private readonly HashSet<string> _salesLineInvoiceOperationsSet;

    public SalesLineAggregatorTask(IProvider<Guid, LoadConfirmationEntity> loadConfirmationProvider,
                                   IProvider<Guid, InvoiceEntity> invoiceProvider)
    {
        _loadConfirmationProvider = loadConfirmationProvider;
        _invoiceProvider = invoiceProvider;
        _salesLineInvoiceOperationsSet = new();
    }

    public override int RunOrder => 10;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<SalesLineEntity> context)
    {
        await UpdateLoadConfirmationAggregates(context);
        await UpdateInvoiceAggregates(context);

        return true;
    }

    private async Task UpdateLoadConfirmationAggregates(BusinessContext<SalesLineEntity> context)
    {
        var isLCAmountUpdated = false;
        var originalLcId = context.Original?.LoadConfirmationId ?? Guid.Empty;
        var targetLcId = context.Target.LoadConfirmationId.GetValueOrDefault(Guid.Empty);
        var loadConfirmations = (await _loadConfirmationProvider.GetByIds(new[] { originalLcId, targetLcId }.Distinct()))
                               .Where(loadConfirmation => loadConfirmation is not null)
                               .ToDictionary(loadConfirmation => loadConfirmation.Id); // PK - TODO: ENTITY or INDEX

        if (loadConfirmations.TryGetValue(targetLcId, out var targetLoadConfirmation))
        {
            if (HasSalesLineValueChanged(context))
            {
                targetLoadConfirmation.TotalCost -= context.Original?.TotalValue ?? 0;
                targetLoadConfirmation.TotalCost += context.Target.TotalValue;
                isLCAmountUpdated = true;
            }

            if (HasLoadConfirmationAssignmentChanged(context))
            {
                if (!isLCAmountUpdated)
                {
                    targetLoadConfirmation.TotalCost += context.Target.TotalValue;
                }

                targetLoadConfirmation.SalesLineCount += 1;
            }

            await _loadConfirmationProvider.Update(targetLoadConfirmation, true);
        }

        if (loadConfirmations.TryGetValue(originalLcId, out var originalLoadConfirmation) && originalLcId != targetLcId)
        {
            originalLoadConfirmation.SalesLineCount -= 1;
            originalLoadConfirmation.TotalCost -= context.Original!.TotalValue;
            await _loadConfirmationProvider.Update(originalLoadConfirmation, true);
        }
    }

    private async Task UpdateInvoiceAggregates(BusinessContext<SalesLineEntity> context)
    {
        var isInvoiceAmountUpdated = false;
        var originalInvoiceId = context.Original?.InvoiceId ?? Guid.Empty;
        var targetInvoiceId = context.Target.InvoiceId.GetValueOrDefault(Guid.Empty);
        var invoices = (await _invoiceProvider.GetByIds(new[] { originalInvoiceId, targetInvoiceId }.Distinct()))
                      .Where(invoice => invoice is not null)
                      .ToDictionary(invoice => invoice.Id); // PK - TODO: ENTITY or INDEX

        if (invoices.TryGetValue(targetInvoiceId, out var targetInvoice))
        {
            if (HasSalesLineValueChanged(context))
            {
                targetInvoice.InvoiceAmount -= context.Original?.TotalValue ?? 0;
                targetInvoice.InvoiceAmount += context.Target.TotalValue;
                isInvoiceAmountUpdated = true;
            }

            if (HasInvoiceAssignmentChanged(context))
            {
                if (!isInvoiceAmountUpdated)
                {
                    targetInvoice.InvoiceAmount += context.Target.TotalValue;
                }

                targetInvoice.SalesLineCount += 1;

                if (UpdateTruckTicketCount(targetInvoice, context.Target, false))
                {
                    targetInvoice.TruckTicketCount = targetInvoice.TruckTicketCount.HasValue ? targetInvoice.TruckTicketCount + 1 : 1;
                }

                //Update Billing Configuration association
                if (context.Target.BillingConfigurationId != null && context.Target.BillingConfigurationId.Value != Guid.Empty)
                {
                    foreach (var associatedBillingConfig in targetInvoice.BillingConfigurations.Where(config => config.BillingConfigurationId == context.Target.BillingConfigurationId)
                                                                         .DefaultIfEmpty(new()
                                                                          {
                                                                              BillingConfigurationId = context.Target.BillingConfigurationId,
                                                                              BillingConfigurationName = context.Target.BillingConfigurationName,
                                                                          })
                                                                         .ToList())
                    {
                        associatedBillingConfig.AssociatedSalesLinesCount = associatedBillingConfig.AssociatedSalesLinesCount.HasValue
                                                                                ? associatedBillingConfig.AssociatedSalesLinesCount + 1
                                                                                : 1;

                        if (associatedBillingConfig.Id != Guid.Empty)
                        {
                            continue;
                        }

                        targetInvoice.BillingConfigurations.Add(associatedBillingConfig);
                    }
                }
            }

            await _invoiceProvider.Update(targetInvoice, true);
        }

        if (invoices.TryGetValue(originalInvoiceId, out var originalInvoice) && originalInvoiceId != targetInvoiceId)
        {
            originalInvoice.SalesLineCount -= 1;
            originalInvoice.InvoiceAmount -= context.Original!.TotalValue;
            if (UpdateTruckTicketCount(originalInvoice, context.Target, false))
            {
                originalInvoice.TruckTicketCount = originalInvoice.TruckTicketCount.HasValue ? originalInvoice.TruckTicketCount - 1 : 0;
            }

            //Update Billing Configuration association
            if (context.Target.BillingConfigurationId != null && context.Target.BillingConfigurationId.Value != Guid.Empty)
            {
                foreach (var associatedBillingConfig in originalInvoice.BillingConfigurations.Where(config => config.BillingConfigurationId == context.Target.BillingConfigurationId)
                                                                       .DefaultIfEmpty(new())
                                                                       .ToList())
                {
                    var removeAtIndex = Int32.MinValue;

                    if (associatedBillingConfig.Id == Guid.Empty)
                    {
                        continue;
                    }

                    //look for any salesline exist for this invoice & billingconfiguration combination
                    associatedBillingConfig.AssociatedSalesLinesCount = associatedBillingConfig.AssociatedSalesLinesCount.HasValue
                                                                            ? associatedBillingConfig.AssociatedSalesLinesCount - 1
                                                                            : 0;

                    removeAtIndex = originalInvoice.BillingConfigurations.IndexOf(associatedBillingConfig);

                    //Only remove Billing Configuration from the collection when No Sales Lines with Specific Billing Configuration Associated to Invoice
                    if (associatedBillingConfig.AssociatedSalesLinesCount <= 0 && removeAtIndex >= 0)
                    {
                        originalInvoice.BillingConfigurations.RemoveAt(removeAtIndex);
                    }
                }
            }

            await _invoiceProvider.Update(originalInvoice, true);
        }
    }

    private bool UpdateTruckTicketCount(InvoiceEntity invoice, SalesLineEntity salesLine, bool increment)
    {
        //Determine options key: salesLine.TruckTicketId + invoice.Id + increment
        var salesLineOperationsKey = $"{salesLine.TruckTicketId}{invoice.Id}{increment}";
        if (_salesLineInvoiceOperationsSet.Contains(salesLineOperationsKey))
        {
            return false;
        }

        _salesLineInvoiceOperationsSet.Add(salesLineOperationsKey);
        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<SalesLineEntity> context)
    {
        var shouldRun = (context.Target is { IsReversal: false, IsReversed: false } &&
                         HasInvoiceAssignmentChanged(context)) ||
                        HasLoadConfirmationAssignmentChanged(context) ||
                        HasSalesLineValueChanged(context);

        return Task.FromResult(shouldRun);
    }

    private bool HasInvoiceAssignmentChanged(BusinessContext<SalesLineEntity> context)
    {
        return context.Original?.InvoiceId != context.Target.InvoiceId;
    }

    private bool HasLoadConfirmationAssignmentChanged(BusinessContext<SalesLineEntity> context)
    {
        return context.Original?.LoadConfirmationId != context.Target.LoadConfirmationId;
    }

    private bool HasSalesLineValueChanged(BusinessContext<SalesLineEntity> context)
    {
        return context.Operation is Operation.Update && Math.Abs(context.Original.TotalValue - context.Target.TotalValue) > 0.01;
    }
}
