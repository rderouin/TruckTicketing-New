// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Trident.Contracts.Api;

public record CompositeKey<T>
{
    public CompositeKey()
    {
    }

    public CompositeKey(T id, string partitionKey)
    {
        Id = id;
        PartitionKey = partitionKey;
    }

    public T Id { get; init; }

    public string PartitionKey { get; init; }

    public override string ToString()
    {
        return $"{Id}@{PartitionKey}";
    }

    public static implicit operator CompositeKey<object>(CompositeKey<T> key)
    {
        return new(key.Id, key.PartitionKey);
    }

    public static implicit operator CompositeKey<T>(CompositeKey<object> key)
    {
        return new((T)key.Id, key.PartitionKey);
    }
}
