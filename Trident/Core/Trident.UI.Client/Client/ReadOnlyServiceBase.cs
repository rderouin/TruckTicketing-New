using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Trident.Contracts.Api.Client;
using Trident.Contracts.Enums;
using Trident.Extensions;

using ApiSearch = Trident.Api.Search;

namespace Trident.UI.Client;

public abstract class ReadOnlyServiceBase<TThis, TModel, TId> : HttpNamedServiceBase<TThis>, IReadOnlyServiceBase<TModel, TId>
    where TModel : class, IModelBase<TId>
    where TThis : IServiceProxy
{
    private readonly IMemoryCache _memoryCache;

    protected ReadOnlyServiceBase(ILogger<TThis> logger,
                                  IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
        var serviceAttr = GetType().GetCustomAttribute<ServiceAttribute>();
        serviceAttr.GuardIsNotNull("ServiceAttribute");
        ResourceName = serviceAttr.ResourceName ?? ResourceName;
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    protected virtual string SearchRoute => $"{ResourceName}/search";

    protected virtual string GetByIdRoute => $"{ResourceName}/{{id}}";

    protected virtual string SearchMethod => HttpMethod.Post.Method;

    protected virtual string GetByIdMethod => HttpMethod.Get.Method;

    protected virtual string ResourceName { get; } = typeof(TModel).Name.ToLower();

    public async Task<Response<ApiSearch.SearchResultsModel<TLookup, ApiSearch.SearchCriteriaModel>>> SearchLookupsResponse<TLookup>(ApiSearch.SearchCriteriaModel criteria)
    {
        var response = await SendRequest<ApiSearch.SearchResultsModel<TLookup, ApiSearch.SearchCriteriaModel>>(SearchMethod, SearchRoute, criteria);
        return response;
    }

    public async Task<Response<ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>>> SearchResponse(ApiSearch.SearchCriteriaModel criteria)
    {
        var response = await SendRequest<ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>>(SearchMethod, SearchRoute, criteria);
        return response;
    }

    public async Task<Response<ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>>> GetAllResponse()
    {
        var criteria = new ApiSearch.SearchCriteriaModel
        {
            CurrentPage = 0,
            PageSize = int.MaxValue,
            Keywords = "",
            OrderBy = "",
            SortOrder = SortOrder.Asc,
        };

        var response = await SearchResponse(criteria);
        return response;
    }

    public async Task<Response<TModel>> GetByIdResponse(TId id)
    {
        var response = await SendRequest<TModel>(GetByIdMethod, GetByIdRoute.Replace("{id}", id.ToString()));
        return response;
    }

    public async Task<TModel> GetById(TId id, bool useCache = false, Action<ICacheEntry> configureCacheOptions = null)
    {
        var cacheKey = $"{GetType().Name}|{id?.ToString() ?? ""}";
        Console.WriteLine($"GetById IsUseCache {useCache}");
        Console.WriteLine($"GetById Cache Key {cacheKey}");
        configureCacheOptions ??= ConfigureDefaultCacheEntryOptions;

        if (DisableCache)
        {
            useCache = false;
        }

        if (useCache is false)
        {
            _memoryCache.Remove(cacheKey);
            var response = await GetByIdResponse(id);
            return response?.Model;
        }

        async Task<object> CacheFactory(ICacheEntry cacheEntry)
        {
            var response = await GetByIdResponse(id);
            var model = response?.Model;

            if (model is not null)
            {
                configureCacheOptions(cacheEntry);
                return model;
            }

            cacheEntry.Dispose();
            return default;
        }

        return await _memoryCache.GetOrCreateAsync(cacheKey, CacheFactory) as TModel;
    }

    public async Task<ApiSearch.SearchResultsModel<TLookup, ApiSearch.SearchCriteriaModel>> SearchLookups<TLookup>(ApiSearch.SearchCriteriaModel criteria)
    {
        var response = await SendRequest<ApiSearch.SearchResultsModel<TLookup, ApiSearch.SearchCriteriaModel>>(SearchMethod, SearchRoute, criteria);
        return response?.Model ?? default;
    }

    public async Task<IEnumerable<TModel>> GetAll()
    {
        var response = await GetAllResponse();
        return response?.Model?.Results ?? default;
    }

    public async Task<ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>> Search(ApiSearch.SearchCriteriaModel criteria,
                                                                                                  bool useCache = false,
                                                                                                  Action<ICacheEntry> configureCacheOptions = null,
                                                                                                  bool refreshCache = false)
    {
        using var sHa256 = SHA256.Create();
        var searchCriteriaHash = Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(criteria))));
        var cacheKey = $"{GetType().Name}|{searchCriteriaHash ?? ""}";
        Console.WriteLine($"Search IsUseCache {useCache}");
        Console.WriteLine($"Search Cache Key {cacheKey}");
        configureCacheOptions ??= ConfigureDefaultCacheEntryOptions;

        if (DisableCache)
        {
            useCache = false;
        }

        if (useCache is false)
        {
            _memoryCache.Remove(cacheKey);
            var response = await SearchResponse(criteria);
            return response?.Model ?? default;
        }

        if (refreshCache)
        {
            _memoryCache.Remove(cacheKey);
            return await _memoryCache.GetOrCreateAsync(cacheKey, CacheFactory) as ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>;
        }

        async Task<object> CacheFactory(ICacheEntry cacheEntry)
        {
            var response = await SearchResponse(criteria);
            var model = response?.Model;

            if (model is not null)
            {
                configureCacheOptions(cacheEntry);
                return model;
            }

            cacheEntry.Dispose();
            return default;
        }

        return await _memoryCache.GetOrCreateAsync(cacheKey, CacheFactory) as ApiSearch.SearchResultsModel<TModel, ApiSearch.SearchCriteriaModel>;
    }

    private static void ConfigureDefaultCacheEntryOptions(ICacheEntry cacheEntry)
    {
        cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
    }
}
