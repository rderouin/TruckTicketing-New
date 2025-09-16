using AutoMapper;

using SE.Shared.Domain;

namespace SE.TokenService.Api.Configuration;

public static class MappingExpressionExtensions
{
    public static IMappingExpression<TSource, TDestination> IgnoreTTEntityBaseMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression)
        where TDestination : TTEntityBase
    {
        expression.ForMember(entity => entity.DocumentType, options => options.Ignore());
        expression.ForMember(entity => entity.EntityType, options => options.Ignore());

        return expression;
    }
}
