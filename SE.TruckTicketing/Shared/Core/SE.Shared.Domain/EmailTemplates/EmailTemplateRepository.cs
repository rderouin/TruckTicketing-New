using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.EmailTemplates;

public class EmailTemplateRepository : CosmosEFCoreSearchRepositoryBase<EmailTemplateEntity>
{
    public EmailTemplateRepository(ISearchResultsBuilder resultsBuilder,
                                   ISearchQueryBuilder queryBuilder,
                                   IAbstractContextFactory abstractContextFactory,
                                   IQueryableHelper queryableHelper)
        : base(resultsBuilder, queryBuilder, abstractContextFactory, queryableHelper)
    {
    }

    [ExcludeFromCodeCoverage(Justification = "The implementation is offloaded into a separate method.")]
    protected override IQueryable<T> ApplyKeywordSearch<T>(IQueryable<T> source, string keywords)
    {
        return ApplyKeywordSearchImpl(source, keywords);
    }

    private IQueryable<T> ApplyKeywordSearchImpl<T>(IQueryable<T> source, string keywords)
    {
        // ensure a proper type
        if (source is not IQueryable<EmailTemplateEntity> typedSource)
        {
            return source;
        }

        // apply filters
        if (keywords.HasText())
        {
            var value = keywords!.ToLower();
            typedSource = typedSource.Where(x => x.Name != null && x.Name.ToLower().Contains(value));
        }

        return (IQueryable<T>)typedSource;
    }
}
