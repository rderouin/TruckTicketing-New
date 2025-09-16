using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Models;

namespace SE.Integrations.Domain.Processors;

public abstract class BaseEntityProcessor<T> : IEntityProcessor<T>
{
    public abstract Task Process(EntityEnvelopeModel<T> entityModel);

    public Task Process(string message)
    {
        // parse the model
        var envelopeModel = JsonConvert.DeserializeObject<EntityEnvelopeModel<T>>(message);
        
        if (envelopeModel!.EnterpriseId == Guid.Empty)
        {
            throw new("Message does not have a valid EnterpriseId");
        }
        
        // process the message
        return Process(envelopeModel!);
    }
}
