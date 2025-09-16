using System;

namespace Trident.Contracts.Api.Client;

public interface IGuidModelBase : IModelBase<Guid>
{
}

public interface IModelBase<TId>
{
    TId Id { get; set; }
}
