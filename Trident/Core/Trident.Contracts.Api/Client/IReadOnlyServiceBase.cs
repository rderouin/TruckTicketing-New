using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using Trident.Api.Search;

namespace Trident.Contracts.Api.Client;

public interface IReadOnlyServiceBase<TModel, TId> : IHttpNamedServiceBase
    where TModel : class, IModelBase<TId>
{
    Task<SearchResultsModel<TModel, SearchCriteriaModel>> Search(SearchCriteriaModel criteria, bool useCache = false, Action<ICacheEntry> configureCacheOptions = null, bool refreshCache = false);

    Task<SearchResultsModel<TLookup, SearchCriteriaModel>> SearchLookups<TLookup>(SearchCriteriaModel criteria);

    Task<TModel> GetById(TId id, bool useCache = false, Action<ICacheEntry> configureCacheOptions = null);

    Task<IEnumerable<TModel>> GetAll();

    Task<Response<SearchResultsModel<TModel, SearchCriteriaModel>>> SearchResponse(SearchCriteriaModel criteria);

    Task<Response<SearchResultsModel<TLookup, SearchCriteriaModel>>> SearchLookupsResponse<TLookup>(SearchCriteriaModel criteria);

    Task<Response<TModel>> GetByIdResponse(TId id);

    Task<Response<SearchResultsModel<TModel, SearchCriteriaModel>>> GetAllResponse();
}
