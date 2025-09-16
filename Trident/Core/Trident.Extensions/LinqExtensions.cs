using System;
using System.Collections.Generic;
using System.Linq;

namespace Trident.Extensions;

public static class LinqExtensions
{
    public static ICollection<TItem> MergeBy<TItem, TKey>(this ICollection<TItem> items1,
                                                          ICollection<TItem> items2,
                                                          Func<TItem, TKey> keySelector)
    {
        var comparer = EqualityComparer<TKey>.Default;
        var items1Lookup = (items1 ?? Array.Empty<TItem>()).ToLookup(keySelector, comparer);
        var items2Lookup = (items2 ?? Array.Empty<TItem>()).ToLookup(keySelector, comparer);

        var keys = new HashSet<TKey>(items1Lookup.Select(p => p.Key), comparer);
        keys.UnionWith(items2Lookup.Select(p => p.Key));

        var join = from key in keys
                   from item1 in items1Lookup[key].DefaultIfEmpty(default)
                   from item2 in items2Lookup[key].DefaultIfEmpty(default)
                   select item1 ?? item2;

        return join.Where(item => item != null).ToList();
    }
}
