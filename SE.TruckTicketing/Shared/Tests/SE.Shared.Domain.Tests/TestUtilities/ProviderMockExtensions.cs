using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Moq;

using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Domain;

namespace SE.Shared.Domain.Tests.TestUtilities;

public static class ProviderMockExtensions
{
    public static void SetupExists<TId, TEntity>(this Mock<IProvider<TId, TEntity>> mock, IEnumerable<TEntity> entities)
        where TEntity : EntityBase<TId>
    {
        mock.Setup(x => x.Exists(It.IsAny<Expression<Func<TEntity, bool>>>(),
                                 It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> filter, bool _) => entities.Any(filter.Compile()));
    }

    public static void SetupEntities<TId, TEntity>(this Mock<IProvider<TId, TEntity>> mock, IEnumerable<TEntity> entities)
        where TEntity : EntityBase<TId>
    {
        mock.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(),
                              It.IsAny<Func<IQueryable<TEntity>,
                                  IOrderedQueryable<TEntity>>>(),
                              It.IsAny<IEnumerable<string>>(),
                              It.IsAny<bool>(),
                              It.IsAny<bool>(),
                              It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> _,
                           List<string> __,
                           bool ___,
                           bool ____,
                           bool _____) => entities.Where(filter.Compile()));

        mock.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(),
                              It.IsAny<string>(),
                              It.IsAny<Func<IQueryable<TEntity>,
                                  IOrderedQueryable<TEntity>>>(),
                              It.IsAny<IEnumerable<string>>(),
                              It.IsAny<bool>(),
                              It.IsAny<bool>(),
                              It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                           string ______,
                           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> _,
                           List<string> __,
                           bool ___,
                           bool ____,
                           bool _____) => entities.Where(filter.Compile()));

        mock.Setup(x => x.Exists(It.IsAny<Expression<Func<TEntity, bool>>>(),
                                 It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                           bool _____) => entities.FirstOrDefault(filter.Compile()) != default);

        mock.Setup(x => x.GetByIds(It.IsAny<IEnumerable<TId>>(),
                                   It.IsAny<bool>(),
                                   It.IsAny<bool>(),
                                   It.IsAny<bool>()))
            .ReturnsAsync((IEnumerable<TId> ids, bool _, bool __, bool ___) => entities.Where(entity => ids.Contains(entity.Id)));

        mock.Setup(x => x.GetByIds(It.IsAny<IEnumerable<CompositeKey<TId>>>(),
                                   It.IsAny<bool>(),
                                   It.IsAny<bool>(),
                                   It.IsAny<bool>()))
            .ReturnsAsync((IEnumerable<TId> ids, bool _, bool __, bool ___) => entities.Where(entity => ids.Contains(entity.Id)));

        mock.Setup(x => x.GetById(It.IsAny<TId>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>()))
            .ReturnsAsync((TId id, bool _, bool __, bool ___) => entities.FirstOrDefault(entity => entity.Id.Equals(id)));

        mock.Setup(x => x.GetById(It.IsAny<CompositeKey<object>>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>(),
                                  It.IsAny<bool>()))
            .ReturnsAsync((TId id, bool _, bool __, bool ___) => entities.FirstOrDefault(entity => entity.Id.Equals(id)));
    }

    public static void SetupEntities<TId, TEntity>(this Mock<IManager<TId, TEntity>> mock, IEnumerable<TEntity> entities)
        where TEntity : EntityBase<TId>
    {
        mock.Setup(x => x.Get(It.IsAny<Expression<Func<TEntity, bool>>>(),
                              It.IsAny<Func<IQueryable<TEntity>,
                                  IOrderedQueryable<TEntity>>>(),
                              It.IsAny<List<string>>(),
                              It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> filter,
                           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> _,
                           List<string> __,
                           bool ___) => entities.Where(filter.Compile()));

        mock.Setup(x => x.GetById(It.IsAny<TId>(),
                                  It.IsAny<bool>()))
            .ReturnsAsync((TId id, bool _) => entities.FirstOrDefault(entity => entity.Id.Equals(id)));

        mock.Setup(x => x.GetById(It.IsAny<CompositeKey<object>>(),
                                  It.IsAny<bool>()))
            .ReturnsAsync((CompositeKey<object> key, bool _) => entities.FirstOrDefault(entity => entity.Id.Equals(key.Id)));
    }
}
