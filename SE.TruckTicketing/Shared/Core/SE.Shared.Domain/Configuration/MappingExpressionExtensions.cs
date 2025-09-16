using AutoMapper;

using SE.Shared.Common.Extensions;

namespace SE.Shared.Domain.Configuration;

public static class MappingExpressionExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreTTEntityBaseMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
        where TDestination : TTEntityBase
    {
        expression.ForMember(entity => entity.DocumentType, options => options.Condition(WellKnownPkCondition));
        expression.ForMember(entity => entity.EntityType, options => options.Ignore());

        return expression;

        bool WellKnownPkCondition(TSource src, TDestination dst, string srcField, string dstField)
        {
            // accept the field from the model if the entity doesn't have it initialized
            // examples:
            //   - Sales Line Entity's Document Type can be initialized upon saving it and can be passed to UI for saved entities,
            //     the newly generated value for the Document Type will come back from UI and while being mapped to the backend entity
            //     it will pass the original value from the frontend model to the backend entity
            //   - Account Entity's Document Type is a well-known value and thus doesn't require input from the frontend model,
            //     this will make the backend initialize property on the entity and thus the value coming from frontend can be discarded
            return !dstField.HasText();
        }
    }

    public static IMappingExpression<TSource, TDestination> IgnoreTTAuditableEntityBaseMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
        where TDestination : TTAuditableEntityBase
    {
        expression.ForMember(entity => entity.CreatedById, options => options.Ignore());
        expression.ForMember(entity => entity.CreatedBy, options => options.Ignore());
        expression.ForMember(entity => entity.CreatedAt, options => options.Ignore());
        expression.ForMember(entity => entity.UpdatedById, options => options.Ignore());
        expression.ForMember(entity => entity.UpdatedBy, options => options.Ignore());
        expression.ForMember(entity => entity.UpdatedAt, options => options.Ignore());

        return expression;
    }
}
