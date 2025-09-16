using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.BillingService.Domain.Entities.InvoiceExchange;

public class InvoiceExchangeRepository : CosmosEFCoreSearchRepositoryBase<InvoiceExchangeEntity>
{
    public InvoiceExchangeRepository(ISearchResultsBuilder resultsBuilder,
                                     ISearchQueryBuilder queryBuilder,
                                     IAbstractContextFactory abstractContextFactory,
                                     IQueryableHelper queryableHelper) :
        base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    public IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        // ensure a proper type
        if (source is not IQueryable<InvoiceExchangeEntity> typedSource)
        {
            return source;
        }

        // exclude deleted by default
        typedSource = typedSource.Where(e => e.IsDeleted == false);

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(e => e.PlatformCode.ToLower().Contains(value) ||
                                                 e.BusinessStreamName.ToLower().Contains(value) ||
                                                 e.LegalEntityName.ToLower().Contains(value) ||
                                                 e.BillingAccountName.ToLower().Contains(value) ||
                                                 e.BillingAccountNumber.ToLower().Contains(value) ||
                                                 e.BillingAccountDunsNumber.ToLower().Contains(value));
        }

        return (IQueryable<T>)typedSource;
    }
}
