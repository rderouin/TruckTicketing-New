using System.Collections.Generic;
using System.Threading.Tasks;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IServiceBase<TModel, TId> : IReadOnlyServiceBase<TModel, TId>
    where TModel : class, IModelBase<TId>
{
    Task<Response<TModel>> Update(TModel model);

    Task<Response<TModel>> Delete(TModel model);

    Task<Response<TModel>> Create(TModel model);

    Task<Response<TModel>> Patch(TId id, TModel model);

    Task<Response<TModel>> Patch(TId id, IDictionary<string, object> model);
}
