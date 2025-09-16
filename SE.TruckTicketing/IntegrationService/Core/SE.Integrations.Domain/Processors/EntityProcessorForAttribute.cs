using System;

namespace SE.Integrations.Domain.Processors;

public class EntityProcessorForAttribute : Attribute
{
    public EntityProcessorForAttribute(string entityType)
    {
        EntityType = entityType;
    }

    public string EntityType { get; }
}
