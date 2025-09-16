using System.Collections.Generic;
using System.Linq;

using Trident.Search;

namespace SE.TruckTicketing.Client.Utilities;

public static class AxiomFilterBuilderExtensions
{
    public static AxiomFilter AsRangeAxiomFilter<T>(this (T start, T end) range, string filterPath)
    {
        var (start, end) = range;

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();
        var index = 0;

        if (start is not null)
        {
            query = ((GroupStart)query).AddAxiom(new()
            {
                Field = filterPath,
                Key = $"{filterPath}{++index}",
                Operator = CompareOperators.gte,
                Value = start,
            });
        }

        if (start is not null && end is not null)
        {
            query = ((AxiomTokenizer)query).And();
        }

        if (end is not null)
        {
            if (query is ILogicalOperator and)
            {
                query = and.AddAxiom(new()
                {
                    Field = filterPath,
                    Key = $"{filterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = end,
                });
            }
            else
            {
                query = ((GroupStart)query).AddAxiom(new()
                {
                    Field = filterPath,
                    Key = $"{filterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = end,
                });
            }
        }

        return ((AxiomTokenizer)query)
              .EndGroup()
              .Build();
    }

    public static AxiomFilter AsInclusionAxiomFilter<T>(this ICollection<T> values,
                                                        string filterPath,
                                                        CompareOperators compareOperator = CompareOperators.contains)
    {
        if (!values.Any())
        {
            return null;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var value in values)
        {
            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = compareOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = compareOperator,
                    Value = value,
                });
            }
        }

        return ((AxiomTokenizer)query).EndGroup().Build();
    }
}
