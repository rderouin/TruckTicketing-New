using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Account.Tasks;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.Account;

[TestClass]
public class AccountContactConcurrentUpdateMergerTaskTests
{
    [TestMethod]
    public async Task Task_Should_Run_When_There_Is_Concurrent_Update_Violation_And_Contacts_Are_Different()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<AccountEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();
        target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>();

        var context = new BusinessContext<AccountEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeTrue();
        scope.InstanceUnderTest.RunOrder.Should().BeNegative();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate);
    }

    [TestMethod]
    public async Task Task_Should_Not_Run_When_There_Is_Concurrent_Update_Violation_And_Contacts_Are_The_Same()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<AccountEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();

        var context = new BusinessContext<AccountEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Should_Not_Run_When_There_Is_No_Concurrent_Update_Violation()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<AccountEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>();

        var original = target.Clone();
        target.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>();

        var context = new BusinessContext<AccountEntity>(target, original);

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_Should_Merge_Contacts_With_No_Deletes()
    {
        // arrange
        var scope = new DefaultScope();

        var target = GenFu.GenFu.New<AccountEntity>();
        target.VersionTag = Guid.NewGuid().ToString();
        target.Contacts = new();

        var original = target.Clone();
        original.VersionTag = Guid.NewGuid().ToString();
        original.Contacts = GenFu.GenFu.ListOf<AccountContactEntity>(3);

        var context = new BusinessContext<AccountEntity>(target, original);

        // act
        await scope.InstanceUnderTest.Run(context);

        // assert
        target.Contacts.Select(contact => contact.Id).Should().BeEquivalentTo(original.Contacts.Select(contact => contact.Id));
    }

    private class DefaultScope : TestScope<AccountContactConcurrentUpdateMergerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }
    }
}
