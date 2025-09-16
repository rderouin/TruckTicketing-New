using System;

namespace Trident.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ContainerAttribute : Attribute
    {
        // This is a positional argument
        public ContainerAttribute(string name,
                                  string partitionKey,
                                  string partitionKeyValue,
                                  PartitionKeyType partitionKeyType)
        {
            Name = name;
            PartitionKey = partitionKey;
            PartitionKeyValue = partitionKeyValue;
            PartitionKeyType = partitionKeyType;
        }

        public string Name { get; }

        public string PartitionKey { get; }

        public string PartitionKeyValue { get; }

        public PartitionKeyType PartitionKeyType { get; }
    }
}