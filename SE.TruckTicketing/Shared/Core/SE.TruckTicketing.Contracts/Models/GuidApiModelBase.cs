using System;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Contracts.Models;

public class GuidApiModelBase : ApiModelBase<Guid>, IGuidModelBase, IHaveCompositeKey<Guid>
{
    public string DocumentType { get; init; }

    [JsonIgnore]
    [NotMapped]
    public CompositeKey<Guid> Key => new(Id, DocumentType);

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}
