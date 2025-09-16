namespace Trident.SourceGeneration.EntityStack;

internal readonly struct EntityStackClassToGenerate
{
    public readonly EntityStackClassType Type;

    public readonly string FileName;

    public readonly string ClassName;

    public readonly string NamespaceName;

    public readonly string EntityClassName;

    public readonly string EntityIdType;

    public EntityStackClassToGenerate(string fileName, string namespaceName, string className, EntityStackClassType type, string entityClassName, string entityIdType)
    {
        ClassName = className;
        FileName = fileName;
        Type = type;
        NamespaceName = namespaceName;
        EntityClassName = entityClassName;
        EntityIdType = entityIdType;
    }
}
