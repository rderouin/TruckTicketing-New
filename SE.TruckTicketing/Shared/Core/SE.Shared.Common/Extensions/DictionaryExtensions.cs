using System.Collections.Generic;
using System.Linq;

namespace SE.Shared.Common.Extensions;

public static class DictionaryExtensions
{
    public static Dictionary<TK, TV> Clone<TK, TV>(this IDictionary<TK, TV> dictionary)
    {
        return dictionary.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public static void CopyPairsTo<TK, TV>(this IDictionary<TK, TV> from, IDictionary<TK, TV> to)
    {
        foreach (var pair in from)
        {
            to[pair.Key] = pair.Value;
        }
    }
}
