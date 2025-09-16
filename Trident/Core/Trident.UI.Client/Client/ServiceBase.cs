using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Trident.Contracts.Api.Client;

namespace Trident.UI.Client;

public abstract class ServiceBase<TThis, TModel, TId> : ReadOnlyServiceBase<TThis, TModel, TId>,
                                                        IServiceProxyBase<TModel, TId>
    where TModel : class, IModelBase<TId>
    where TThis : IServiceProxy
{
    protected ServiceBase(ILogger<TThis> logger,
                          IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    protected virtual string UpdateRoute => $"{ResourceName}/{{id}}";

    protected virtual string CreateRoute => $"{ResourceName}";

    protected virtual string DeleteRoute => $"{ResourceName}/{{id}}";

    protected virtual string PatchRoute => $"{ResourceName}/{{id}}";

    protected virtual string UpdateMethod => HttpMethod.Put.Method;

    protected virtual string CreateMethod => HttpMethod.Post.Method;

    protected virtual string DeleteMethod => HttpMethod.Delete.Method;

    protected virtual string PatchMethod => HttpMethod.Patch.Method;

    public async Task<Response<TModel>> Update(TModel model)
    {
        var response = await SendRequest<TModel>(UpdateMethod, UpdateRoute.Replace("{id}", model.Id.ToString()), model);
        return response;
    }

    public virtual async Task<Response<TModel>> Delete(TModel model)
    {
        var response = await SendRequest<TModel>(DeleteMethod, DeleteRoute.Replace("{id}", model.Id.ToString()), model);
        return response;
    }

    public async Task<Response<TModel>> Create(TModel model)
    {
        if (model.Id is Guid guid && guid == Guid.Empty)
        {
            model.GetType().GetProperty(nameof(model.Id))!.SetValue(model, Guid.NewGuid());
        }

        var response = await SendRequest<TModel>(CreateMethod, CreateRoute, model);
        return response;
    }

    public async Task<Response<TModel>> Patch(TId id, IDictionary<string, object> patches)
    {
        var response = await SendRequest<TModel>(PatchMethod, PatchRoute.Replace("{id}", id.ToString()), patches);
        return response;
    }

    public async Task<Response<TModel>> Patch(TId id, TModel model)
    {
        var response = await SendRequest<TModel>(PatchMethod, PatchRoute.Replace("{id}", id.ToString()), model);
        return response;
    }
}
