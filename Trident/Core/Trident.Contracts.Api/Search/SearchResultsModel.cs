using System.Collections.Generic;
using System.Linq;

namespace Trident.Api.Search;

/// <summary>
///     Class SearchResults.
/// </summary>
/// <typeparam name="T">Entity Type</typeparam>
/// <typeparam name="C">Entity Search Critiera Type</typeparam>
public class SearchResultsModel<T, C> where C : SearchCriteriaModel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchResults{T, C}" /> class.
    /// </summary>
    /// <param name="results">The results.</param>
    public SearchResultsModel(IEnumerable<T> results) : this()
    {
        Results = results;
        var resultCount = results.Count();
        Info.PageSize = resultCount == 0 ? 10 : resultCount;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SearchResults{T, C}" /> class.
    /// </summary>
    public SearchResultsModel()
    {
        Info = new()
        {
            PageSize = 10,
        };

        Results = new List<T>();
    }

    /// <summary>
    ///     Gets or sets the results.
    /// </summary>
    /// <value>The results.</value>
    public IEnumerable<T> Results { get; set; }

    /// <summary>
    ///     Gets or sets the information.
    /// </summary>
    /// <value>The information.</value>
    public SearchResultInfoModel<C> Info { get; set; }
}