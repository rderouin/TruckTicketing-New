using AutoMapper;

using SE.Shared.Domain;

namespace SE.BillingService.Api.Configuration;

public static class MappingExpressionExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreTTEntityBaseMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
        where TDestination : TTEntityBase
    {
        expression.ForMember(entity => entity.DocumentType, options => options.Ignore());
        expression.ForMember(entity => entity.EntityType, options => options.Ignore());

        return expression;
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
