using System.Collections.Generic;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.Client.Extensions;

public static class ApplyFilterOnSearchCriteria
{
    public static SearchCriteriaModel FilterByCollection<T>(this SearchCriteriaModel criteria, List<T> data, string filterPath, CompareOperators comparerOperator)
    {
        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var value in data)
        {
            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = comparerOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = comparerOperator,
                    Value = value,
                });
            }

            criteria.Filters[filterPath] = ((AxiomTokenizer)query).EndGroup().Build();
        }

        return criteria;
    }
}
