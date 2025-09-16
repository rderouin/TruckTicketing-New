using Trident.Contracts.Api.Client;

namespace Trident.Contracts.Api;

public abstract class ApiModelBase<TId> : IModelBase<TId>
{
    public TId Id { get; set; }
}
