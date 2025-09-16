using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Trident.Domain;

using TypeExtensions = Trident.TypeExtensions;

namespace SE.Shared.Domain.Entities.Changes;

public static class ChangeObserverExtensions
{
    private static readonly ConcurrentDictionary<Type, Type> _entityIdTypeCache = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo> _methodCache = new();

    public static async Task<object> FetchOriginalDynamic(this DbContext dbContext, object current)
    {
        // no current object = no original from the database, e.g. an entity is added
        if (current == null)
        {
            return null;
        }

        // make the generic method
        var typeOfEntity = current.GetType();
        var typeOfId = _entityIdTypeCache.GetOrAdd(typeOfEntity, GetIdType);
        var method = _methodCache.GetOrAdd(typeOfEntity, ConstructGenericMethod);

        // fetch the original object
        var result = method.Invoke(null, new[] { dbContext, current });
        if (result is Task task)
        {
            // await the task 
            await task;

            // get the result
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var property = taskType.GetProperty(nameof(Task<object>.Result))!;
                var value = property.GetValue(task);
                return value;
            }
        }

        throw new InvalidOperationException($"Invalid generic implementation. Unable to call the {nameof(FetchOriginal)} method.");

        Type GetIdType(Type type)
        {
            // check every type in the hierarchy
            while (type != null)
            {
                // if the desired type found
                if (type.Name == typeof(DocumentDbEntityBase<object>).Name)
                {
                    // fetch the type argument which represents the type of ID of the entity
                    return type.GetGenericArguments().FirstOrDefault();
                }

                type = type.BaseType;
            }

            // exhausted options
            return null;
        }

        MethodInfo ConstructGenericMethod(Type _)
        {
            var methodTemplate = typeof(ChangeObserverExtensions).GetMethod(nameof(FetchOriginal))!;
            var genericMethod = methodTemplate.MakeGenericMethod(typeOfEntity, typeOfId);
            return genericMethod;
        }
    }

    public static async Task<TEntity> FetchOriginal<TEntity, TId>(this DbContext dbContext, TEntity current)
        where TEntity : DocumentDbEntityBase<TId>
    {
        var idExpression = TypeExtensions.CreateTypedCompareExpression<TEntity>(nameof(Entity.Id), current.Key.Id);
        return await dbContext.Set<TEntity>()
                              .AsNoTracking()
                              .WithPartitionKey(current.Key.PartitionKey)
                              .FirstOrDefaultAsync(idExpression);
    }
}
