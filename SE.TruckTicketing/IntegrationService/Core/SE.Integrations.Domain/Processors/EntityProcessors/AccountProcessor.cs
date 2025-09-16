using System;
using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Account;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Mapper;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor("Account")]
public class AccountProcessor : BaseEntityProcessor<Account>
{
    private readonly IManager<Guid, AccountEntity> _manager;

    private readonly IMapperRegistry _mapperRegistry;

    public AccountProcessor(IMapperRegistry mapperRegistry,
                            IManager<Guid, AccountEntity> manager)
    {
        _mapperRegistry = mapperRegistry;
        _manager = manager;
    }

    public override async Task Process(EntityEnvelopeModel<Account> entityModel)
    {
        var newEntity = _mapperRegistry.Map<AccountEntity>(entityModel.Payload);

        await _manager.Save(newEntity);
    }
}
