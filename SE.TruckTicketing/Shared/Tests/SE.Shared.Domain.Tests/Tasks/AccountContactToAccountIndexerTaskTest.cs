using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Account.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;

using Trident.Business;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class AccountContactToAccountIndexerTaskTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<AccountEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetHasNoContact()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<AccountEntity>();
        var context = new BusinessContext<AccountEntity>(entity, entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetHasContactRecords()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<AccountEntity>();
        entity.Contacts = new();
        var context = new BusinessContext<AccountEntity>(entity, entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_NoUpdateInTargetContacts()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = GenFu.GenFu.New<AccountEntity>();
        var contacts = GenFu.GenFu.ListOf<AccountContactEntity>(5);
        entity.Contacts = new(contacts);
        var context = new BusinessContext<AccountEntity>(entity, entity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotAddNewIndex_WithMergingExistingIndices_IfContactIsDeleted()
    {
        // arrange
        var scope = new DefaultScope();
        //original context
        var entity = GenFu.GenFu.New<AccountEntity>();
        var accountContacts = GenFu.GenFu.ListOf<AccountContactEntity>(10);
        entity.Contacts = new(accountContacts);
        var newAccountContact = GenFu.GenFu.New<AccountContactEntity>();
        newAccountContact.IsDeleted = true;
        var targetEntity = entity.Clone();
        targetEntity.Contacts.Add(newAccountContact);
        var context = new BusinessContext<AccountEntity>(targetEntity, entity);
        var existingIndexes = GenFu.GenFu.ListOf<AccountContactIndexEntity>(accountContacts.Count);
        scope.SetUpExistingIndexesWithExistingContacts(existingIndexes, accountContacts, entity);
        var existingIndexForAccountContacts = existingIndexes.First(x => x.Id == accountContacts.First(c => !c.IsDeleted).Id);

        scope.IndexManagerMock.SetupEntities(existingIndexes);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedAccountContactIds = scope.PersistedIndices?.Select(index => index.Id);
        persistedAccountContactIds.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldAddNewIndex_WithMergingExistingIndices_IfContactNotDeleted()
    {
        // arrange
        var scope = new DefaultScope();
        //original
        var entity = GenFu.GenFu.New<AccountEntity>();
        var accountContacts = GenFu.GenFu.ListOf<AccountContactEntity>(5);
        entity.Contacts = new(accountContacts);

        //target
        var targetEntity = entity.Clone();
        var newAccountContact = GenFu.GenFu.New<AccountContactEntity>();
        newAccountContact.IsDeleted = false;
        targetEntity.Contacts.Add(newAccountContact);

        //context
        var context = new BusinessContext<AccountEntity>(targetEntity, entity);

        //setup existing indices
        var existingIndexes = GenFu.GenFu.ListOf<AccountContactIndexEntity>(accountContacts.Count);
        scope.SetUpExistingIndexesWithExistingContacts(existingIndexes, accountContacts, entity);

        scope.IndexManagerMock.SetupEntities(existingIndexes);
        var addedUpdatedContactIds = scope.InstanceUnderTest.ContactAddedOrUpdated(context).Select(x => x.Id).ToList();

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedAccountContactIds = scope.PersistedIndices.Select(index => index.Id);
        persistedAccountContactIds.Should().BeEquivalentTo(addedUpdatedContactIds);
        scope.PersistedIndices.Should().Contain(index => index.Id == newAccountContact.Id);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldDeleteIndex_WhenAccountContactIsDeletedForExistingIndex()
    {
        // arrange
        var scope = new DefaultScope();
        //original
        var entity = GenFu.GenFu.New<AccountEntity>();
        var accountContacts = GenFu.GenFu.ListOf<AccountContactEntity>(5);
        entity.Contacts = new(accountContacts);
        entity.Contacts.ForEach(x => x.IsDeleted = false);
        //target
        var targetEntity = entity.Clone();
        targetEntity.Contacts[0].IsDeleted = true;

        //context
        var context = new BusinessContext<AccountEntity>(targetEntity, entity);

        //setup existing indices
        var existingIndexes = GenFu.GenFu.ListOf<AccountContactIndexEntity>(accountContacts.Count);
        scope.SetUpExistingIndexesWithExistingContacts(existingIndexes, accountContacts, entity);

        scope.IndexManagerMock.SetupEntities(existingIndexes);
        var addedUpdatedContactIds = scope.InstanceUnderTest.ContactAddedOrUpdated(context).Select(x => x.Id).ToList();

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexManagerMock.Verify(num => num.Delete(It.IsAny<AccountContactIndexEntity>(), false));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotDuplicatedIndex_WhenMergingNewWithExistingIndexes()
    {
        // arrange
        var scope = new DefaultScope();
        //original
        var entity = GenFu.GenFu.New<AccountEntity>();
        var accountContacts = GenFu.GenFu.ListOf<AccountContactEntity>(5);
        entity.Contacts = accountContacts;
        entity.Contacts[0].IsDeleted = false;
        entity.Contacts.First(x => !x.IsDeleted).ContactFunctions = new()
        {
            Key = Guid.NewGuid(),
            List = new() { AccountContactFunctions.GeneratorRepresentative.ToString() },
        };

        //target
        var targetEntity = entity.Clone();
        targetEntity.Contacts.First(x => !x.IsDeleted).ContactFunctions = new()
        {
            Key = Guid.NewGuid(),
            List = new() { AccountContactFunctions.General.ToString() },
        };

        //context
        var context = new BusinessContext<AccountEntity>(targetEntity, entity);

        var existingIndexes = GenFu.GenFu.ListOf<AccountContactIndexEntity>(accountContacts.Count);
        scope.SetUpExistingIndexesWithExistingContacts(existingIndexes, accountContacts, entity);

        scope.IndexManagerMock.SetupEntities(existingIndexes);
        var addedUpdatedContactIds = scope.InstanceUnderTest.ContactAddedOrUpdated(context).Select(x => x.Id).ToList();
        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedAccountContactIds = scope.PersistedIndices.Select(index => index.Id);
        persistedAccountContactIds.Should().BeEquivalentTo(addedUpdatedContactIds);
        scope.PersistedIndices.Should().Contain(index => addedUpdatedContactIds.Contains(index.Id));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldCreateNewIndices_WhenNewAccountCreate_ContactAdded()
    {
        // arrange
        var scope = new DefaultScope();
        //target
        var entity = GenFu.GenFu.New<AccountEntity>();
        var accountContacts = GenFu.GenFu.ListOf<AccountContactEntity>(5);
        accountContacts.ForEach(x => x.IsDeleted = false);
        entity.Contacts = accountContacts;
        entity.Contacts.First(x => !x.IsDeleted).ContactFunctions = new()
        {
            Key = Guid.NewGuid(),
            List = new() { AccountContactFunctions.GeneratorRepresentative.ToString() },
        };

        //context
        var context = new BusinessContext<AccountEntity>(entity);

        var addedUpdatedContactIds = scope.InstanceUnderTest.ContactAddedOrUpdated(context).Select(x => x.Id).ToList();
        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        var persistedAccountContactIds = scope.PersistedIndices.Select(index => index.Id);
        persistedAccountContactIds.Should().BeEquivalentTo(addedUpdatedContactIds);
        scope.PersistedIndices.Should().Contain(index => addedUpdatedContactIds.Contains(index.Id));
    }

    private class DefaultScope : TestScope<AccountContactToAccountIndexerTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexManagerMock.Object);

            IndexManagerMock.Setup(manager => manager.BulkSave(It.IsAny<IEnumerable<AccountContactIndexEntity>>()))
                            .Callback((IEnumerable<AccountContactIndexEntity> entities) => PersistedIndices = entities);

            IndexManagerMock.Setup(manager => manager.Delete(It.IsAny<AccountContactIndexEntity>(), It.IsAny<bool>()))
                            .ReturnsAsync(true);
        }

        public Mock<IManager<Guid, AccountContactIndexEntity>> IndexManagerMock { get; } = new();

        public IEnumerable<AccountContactIndexEntity> PersistedIndices { get; set; }

        public AccountContactIndexEntity DeletedIndex { get; set; }

        public void SetUpExistingIndexesWithExistingContacts(List<AccountContactIndexEntity> existingIndexes, List<AccountContactEntity> accountContacts, AccountEntity account)
        {
            for (var i = 0; i < accountContacts.Count; i++)
            {
                existingIndexes[i].Id = accountContacts[i].Id;
            }

            existingIndexes.ForEach(x => x.AccountId = account.Id);
        }
    }
}
