using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SE.Shared.Common.Extensions;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Account;

public class AccountRepository : CosmosEFCoreSearchRepositoryBase<AccountEntity>
{
    public AccountRepository(ISearchResultsBuilder resultsBuilder, ISearchQueryBuilder queryBuilder, IAbstractContextFactory abstractContextFactory, IQueryableHelper queryableHelper) :
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
        if (source is not IQueryable<AccountEntity> typedSource)
        {
            return source;
        }
        
        if (!keywords.HasText())
        {
            return (IQueryable<T>)typedSource;
        }

        var lowerKeywords = keywords!.ToLower();

        typedSource = typedSource.Where(e =>
                                            (e.IsShowAccount != null && e.IsShowAccount == true) && (
                                            (e.Name != null && e.Name.ToLower().Contains(lowerKeywords)) ||
                                             (e.NickName != null && e.NickName.ToLower().Contains(lowerKeywords)) ||
                                             (e.AccountNumber != null && e.AccountNumber.ToLower().Contains(lowerKeywords)) ||
                                             (e.LegalEntity != null && e.LegalEntity.ToLower().Contains(lowerKeywords)) ||
                                             (e.AccountPrimaryContactName != null && e.AccountPrimaryContactName.ToLower().Contains(lowerKeywords)) ||
                                             (e.AccountPrimaryContactPhoneNumber != null && e.AccountPrimaryContactPhoneNumber.ToLower().Contains(lowerKeywords)) ||
                                             (e.CustomerNumber != null && e.CustomerNumber.ToLower().Contains(lowerKeywords))));

        return (IQueryable<T>)typedSource;
    }
}
