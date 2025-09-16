using Trident.Domain;

namespace SE.Shared.Domain;

public static class FilterExtensions
{
    public static string AsPrimitiveCollectionFilterKey(this string key)
    {
        return $"{key}.{nameof(PrimitiveCollection<object>.Raw)}";
    }
}
