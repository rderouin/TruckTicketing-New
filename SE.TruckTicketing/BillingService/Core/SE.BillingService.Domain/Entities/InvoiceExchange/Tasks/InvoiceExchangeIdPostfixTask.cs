using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Trident.Business;
using Trident.Domain;
using Trident.Workflow;

namespace SE.BillingService.Domain.Entities.InvoiceExchange.Tasks;

public class InvoiceExchangeIdPostfix : WorkflowTaskBase<BusinessContext<InvoiceExchangeEntity>>
{
    public override int RunOrder => 100;

    public override OperationStage Stage => OperationStage.PostValidation;

    public override Task<bool> Run(BusinessContext<InvoiceExchangeEntity> context)
    {
        var entity = context.Target;

        // entities to be fixed
        var list = new List<OwnedEntityBase<Guid>>
        {
            entity.InvoiceDeliveryConfiguration,
            entity.InvoiceDeliveryConfiguration.MessageAdapterSettings,
            entity.InvoiceDeliveryConfiguration.TransportSettings,
            entity.InvoiceDeliveryConfiguration.AttachmentSettings,
            entity.FieldTicketsDeliveryConfiguration,
            entity.FieldTicketsDeliveryConfiguration.MessageAdapterSettings,
            entity.FieldTicketsDeliveryConfiguration.TransportSettings,
            entity.FieldTicketsDeliveryConfiguration.AttachmentSettings,
        };

        // fix IDs
        foreach (var item in list.Where(item => item.Id == Guid.Empty))
        {
            item.Id = Guid.NewGuid();
        }

        return Task.FromResult(true);
    }

    public override Task<bool> ShouldRun(BusinessContext<InvoiceExchangeEntity> context)
    {
        return Task.FromResult(context.Target != null);
    }
}
