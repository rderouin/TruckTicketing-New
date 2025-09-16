using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Entities.UserProfile.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Domain;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.UserProfile;

[TestClass]
public class UserProfileExternalAuthIdDataLoaderTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task WorkflowTask_CanBeInstantiated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var mustAlwaysRun = await scope.InstanceUnderTest.ShouldRun(scope.CreateContextWithValidUserProfileEntity());
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        mustAlwaysRun.Should().BeTrue();
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task WorkflowTask_ShouldSetExternalAuthUniqueFlagToTrue_WhenUserProfileExternalAuthIdIsUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        var existing = new UserProfileEntity
        {
            Id = Guid.NewGuid(),
            ExternalAuthId = context.Target.ExternalAuthId + "MakeUnique",
        };

        scope.SetExistingUserProfiles(existing);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        context.GetContextBagItemOrDefault(UserProfileBusinessContextBagKeys.UserProfileExternalAuthIdIsUnique, false)
               .Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task WorkflowTask_ShouldSetExternalAuthUniqueFlagToFalse_WhenUserProfileExternalAuthIdIsNotUnique()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        var existing = new UserProfileEntity
        {
            Id = Guid.NewGuid(),
            ExternalAuthId = context.Target.ExternalAuthId,
        };

        scope.SetExistingUserProfiles(existing);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        context.GetContextBagItemOrDefault(UserProfileBusinessContextBagKeys.UserProfileExternalAuthIdIsUnique, true)
               .Should().BeFalse();
    }

    private class DefaultScope : TestScope<UserProfileExternalAuthIdDataLoader>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(UserProfileProviderMock.Object);
        }

        public Mock<IProvider<Guid, UserProfileEntity>> UserProfileProviderMock { get; } = new();

        public void SetExistingUserProfiles(params UserProfileEntity[] userProfiles)
        {
            ConfigureProviderMockGetValues(UserProfileProviderMock, userProfiles);
        }

        public BusinessContext<UserProfileEntity> CreateContextWithValidUserProfileEntity()
        {
            return new(new()
            {
                Id = Guid.NewGuid(),
                Email = "jjackson@secure-energy.com",
                GivenName = "Jack",
                Surname = "Jackson",
                DisplayName = "Jack Jackson",
                ExternalAuthId = "12345",
                LocalAuthId = "",
            });
        }

        private void ConfigureProviderMockGetValues<TId, TEntity>(Mock<IProvider<TId, TEntity>> mock, TEntity[] entities) where TEntity : EntityBase<TId>
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

            mock.Setup(x => x.GetByIds(It.IsAny<IEnumerable<TId>>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>(),
                                       It.IsAny<bool>()))
                .ReturnsAsync((IEnumerable<TId> ids, bool _, bool __, bool ___) => entities.Where(entity => ids.Contains(entity.Id)));
        }
    }
}
