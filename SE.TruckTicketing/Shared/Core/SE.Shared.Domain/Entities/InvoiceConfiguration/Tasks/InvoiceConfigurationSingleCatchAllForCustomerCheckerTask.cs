using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration.Tasks;

public class InvoiceConfigurationSingleCatchAllForCustomerCheckerTask : WorkflowTaskBase<BusinessContext<InvoiceConfigurationEntity>>
{
    public const string ResultKey = nameof(InvoiceConfigurationSingleCatchAllForCustomerCheckerTask) + nameof(ResultKey);

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigurationProvider;

    public InvoiceConfigurationSingleCatchAllForCustomerCheckerTask(IProvider<Guid, InvoiceConfigurationEntity> provider)
    {
        _invoiceConfigurationProvider = provider;
    }

    public override int RunOrder => 15;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<InvoiceConfigurationEntity> context)
    {
        var currentCatchAllInvoiceConfigurationForCustomer = await _invoiceConfigurationProvider.Get(config =>
                                                                                                         config.Id != context.Target.Id &&
                                                                                                         config.CustomerId == context.Target.CustomerId &&
                                                                                                         config.CatchAll);

        var catchAllInvoiceConfigurationForCustomer = currentCatchAllInvoiceConfigurationForCustomer.ToList();

        context.ContextBag.TryAdd(ResultKey, catchAllInvoiceConfigurationForCustomer.Any());

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceConfigurationEntity> context)
    {
        return Task.FromResult(context.Target.CustomerId != default && context.Target.CatchAll);
    }
}
