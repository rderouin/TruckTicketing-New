using Trident.Domain;

namespace SE.TruckTicketing.Client.Utilities;

public static class FilterExtensions
{
    public static string AsPrimitiveCollectionFilterKey(this string key)
    {
        return $"{key}.{nameof(PrimitiveCollection<object>.Raw)}";
    }
}
