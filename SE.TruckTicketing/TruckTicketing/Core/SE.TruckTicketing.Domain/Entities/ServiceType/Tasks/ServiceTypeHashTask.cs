using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.ServiceType;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.ServiceType.Tasks;

public class ServiceTypeHashTask : WorkflowTaskBase<BusinessContext<ServiceTypeEntity>>
{
    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    public ServiceTypeHashTask(IProvider<Guid, ServiceTypeEntity> serviceTypeProvider)
    {
        _serviceTypeProvider = serviceTypeProvider;
    }

    public override int RunOrder => 1;

    public override OperationStage Stage => OperationStage.BeforeInsert | OperationStage.BeforeUpdate;

    public override async Task<bool> Run(BusinessContext<ServiceTypeEntity> context)
    {
        //Create a target string to hash
        var hashValue = string.Empty;

        var entity = context.Target.Clone();
        entity.Id = Guid.Empty;
        entity.SearchableId = null;
        entity.Name = null;

        entity.Hash = null;

        //create hash
        using (var sHa256 = SHA256.Create())
        {
            hashValue = Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity))));
        }

        context.Target.Hash = hashValue;
        //Find any hash that matches existing - this goes in validation
        var matches = await _serviceTypeProvider.Get(x => (x.Hash == hashValue && x.Id != context.Target.Id) || (x.LegalEntityId == context.Target.LegalEntityId &&
                                                                                                                 x.Name.ToLower().Equals(context.Target.Name.ToLower())));

        context.ContextBag.Add(ServiceTypeWorkflowContextBagKeys.ServiceTypeHashIsUnique, !matches.Any());

        return true;
    }

    public override Task<bool> ShouldRun(BusinessContext<ServiceTypeEntity> context)
    {
        return Task.FromResult(true);
    }
}
