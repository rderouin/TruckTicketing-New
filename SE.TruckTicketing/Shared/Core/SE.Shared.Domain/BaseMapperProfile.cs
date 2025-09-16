using System;

using AutoMapper;

using SE.Shared.Domain.Configuration;
using SE.TruckTicketing.Contracts.Models;

using Trident.Contracts.Api;
using Trident.Domain;

namespace SE.Shared.Domain;

public abstract class BaseMapperProfile : Profile
{
    public IMappingExpression<TSource, TDestination> CreateAuditableEntityMap<TSource, TDestination>()
        where TSource : GuidApiModelBase
        where TDestination : TTAuditableEntityBase
    {
        return CreateMap<TSource, TDestination>()
              .IgnoreTTEntityBaseMembers()
              .IgnoreTTAuditableEntityBaseMembers();
    }

    public IMappingExpression<TSource, TDestination> CreateEntityMap<TSource, TDestination>()
        where TSource : GuidApiModelBase
        where TDestination : TTEntityBase
    {
        return CreateMap<TSource, TDestination>()
           .IgnoreTTEntityBaseMembers();
    }

    public IMappingExpression<TSource, TDestination> CreateOwnedEntityMap<TSource, TDestination>()
        where TSource : ApiModelBase<Guid>
        where TDestination : OwnedEntityBase<Guid>
    {
        return CreateMap<TSource, TDestination>();
    }

    public IMappingExpression<TSource, TDestination> CreateOwnedLookupEntityMap<TSource, TDestination>()
        where TSource : ApiModelBase<Guid>
        where TDestination : OwnedLookupEntityBase<Guid>
    {
        return CreateMap<TSource, TDestination>();
    }

    public IMappingExpression<TSource, TDestination> CreateBasicMap<TSource, TDestination>()
    {
        return CreateMap<TSource, TDestination>();
    }
}
