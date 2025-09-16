using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Trident.Domain;

namespace SE.Shared.Domain;

public class TTEntityBase : TTEntityBase<Guid>
{
}

public class TTEntityBase<T> : DocumentDbEntityBase<T>
{
    // ReSharper disable once InconsistentNaming
    [JsonProperty("id")]
    public string _id;
}

public interface ISupportOptimisticConcurrentUpdates
{
    string VersionTag { get; set; }

    IEnumerable<object> GetFieldsToCompare();
}

public interface IHaveCompositePartitionKey
{
    void InitPartitionKey(string customPartitionKey = null);
}
