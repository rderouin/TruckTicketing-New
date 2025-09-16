using System;

using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.BillingService.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.InvoiceConfigurationPermutationsIndex.Id)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.InvoiceConfigurationPermutationsIndex.Search)]
public partial class InvoiceConfigurationPermutations : HttpFunctionApiBase<InvoiceConfigurationPermutationsIndex, InvoiceConfigurationPermutationsIndexEntity, Guid>
{
    public InvoiceConfigurationPermutations(ILog log,
                                            IMapperRegistry mapper,
                                            IManager<Guid, InvoiceConfigurationPermutationsIndexEntity> manager)
        : base(log, mapper, manager)
    {
    }
}
