using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Invoices;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

public class InvoiceConfigurationInvoiceContactUpdateTask : WorkflowTaskBase<BusinessContext<InvoiceConfigurationEntity>>
{
    private readonly IProvider<Guid, InvoiceEntity> _invoiceProvider;

    private readonly ISalesOrderPublisher _salesOrderPublisher;

    public InvoiceConfigurationInvoiceContactUpdateTask(IProvider<Guid, InvoiceEntity> provider, ISalesOrderPublisher salesOrderPublisher)
    {
        _invoiceProvider = provider;
        _salesOrderPublisher = salesOrderPublisher;
    }

    public override int RunOrder => 15;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override async Task<bool> Run(BusinessContext<InvoiceConfigurationEntity> context)
    {
        var excludedInvoiceStatuses = new List<InvoiceStatus>
        {
            InvoiceStatus.Void,
            InvoiceStatus.Posted,
            InvoiceStatus.Unknown,
        };

        var invoicesForUpdatedInvoiceConfiguration = await _invoiceProvider.Get(invoice => invoice.InvoiceConfigurationId == context.Target.Id &&
                                                                                           !excludedInvoiceStatuses.Contains(invoice.Status));

        var invoices = invoicesForUpdatedInvoiceConfiguration?.ToList() ?? new List<InvoiceEntity>();
        if (!invoices.Any())
        {
            return true;
        }

        foreach (var invoice in invoices)
        {
            invoice.BillingContactId = context.Target.BillingContactId;
            invoice.BillingContactName = context.Target.BillingContactName;
            await _invoiceProvider.Update(invoice, true);
        }

        await _invoiceProvider.SaveDeferred();

        foreach (var invoice in invoices)
        {
            await _salesOrderPublisher.PublishSalesOrder(invoice.Key);
        }

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceConfigurationEntity> context)
    {
        var isBillingContactUpdated = (context.Original == null && context.Target.BillingContactId is { } or { }) || context.Original?.BillingContactId != context.Target.BillingContactId;
        return Task.FromResult(isBillingContactUpdated);
    }
}
