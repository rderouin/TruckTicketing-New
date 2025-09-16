namespace Trident.Contracts.Api.Client;

public interface IHaveCompositeKey<T>
{
    T Id { get; }

    string DocumentType { get; }

    CompositeKey<T> Key { get; }
}
