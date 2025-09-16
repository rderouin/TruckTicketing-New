using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
using SE.Shared.Domain.Product;
using SE.TridentContrib.Extensions.Azure.Functions;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.IoC;
using Trident.Logging;
using Trident.Search;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.FacilityService;

public class FacilityServiceSubstanceIndexManager : ManagerBase<Guid, FacilityServiceSubstanceIndexEntity>
{
    private readonly IManager<Guid, ProductEntity> _productManager;

    private readonly IIoCServiceLocator _serviceLocator;

    public FacilityServiceSubstanceIndexManager(ILog logger,
                                                IProvider<Guid, FacilityServiceSubstanceIndexEntity> provider,
                                                IManager<Guid, ProductEntity> productManager,
                                                IIoCServiceLocator serviceLocator,
                                                IValidationManager<FacilityServiceSubstanceIndexEntity> validationManager = null,
                                                IWorkflowManager<FacilityServiceSubstanceIndexEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _productManager = productManager;
        _serviceLocator = serviceLocator;
    }

    public override async Task<FacilityServiceSubstanceIndexEntity> GetById(object id, bool loadChildren = false)
    {
        var entity = await base.GetById(id, loadChildren);
        await UpgradeIndexesIfRequired(new[] { entity });
        return entity;
    }

    public override async Task<SearchResults<FacilityServiceSubstanceIndexEntity, SearchCriteria>> Search(SearchCriteria criteria, bool loadChildren = false)
    {
        var searchResults = await base.Search(criteria, loadChildren);
        await UpgradeIndexesIfRequired(searchResults.Results);
        return searchResults;
    }

    private async Task UpgradeIndexesIfRequired(IEnumerable<FacilityServiceSubstanceIndexEntity> indexes)
    {
        var upgradedIndexes = new List<FacilityServiceSubstanceIndexEntity>();

        foreach (var index in indexes)
        {
            if (index.IsLatestVersion())
            {
                continue;
            }

            // v2 upgrade
            var product = await _productManager.GetById(index.TotalProductId);
            index.TotalProductCategories = product?.Categories?.Clone().With(c => c.Key = Guid.NewGuid()) ?? new();
            index.IndexVersion = 2;

            // queue for saving
            upgradedIndexes.Add(index);
        }

        if (upgradedIndexes.Any())
        {
            // create a new scope to isolate changes
            using var scope = _serviceLocator.CreateChildLifetimeScope();

            // init app-insights logger
            var logger = scope.Get<ILog>();
            var functionContextAccessor = new FunctionContextAccessor();
            logger.SetCallContext(functionContextAccessor.FunctionContext);

            // save in the separate context since this upgrade method can be called from anywhere, including read/mixed methods
            var indexManager = scope.Get<IManager<Guid, FacilityServiceSubstanceIndexEntity>>();

            // save the upgraded index
            await indexManager.BulkSave(upgradedIndexes);
        }
    }
}
