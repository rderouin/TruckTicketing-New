using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Tasks;

[TestClass]
public class BillingConfigurationBillingCustomerContactIndexerTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<BillingConfigurationEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var targetEntity = scope.ValidBillingConfiurationEntity;
        targetEntity.CustomerGeneratorId = Guid.NewGuid();
        targetEntity.GeneratorRepresentativeId = Guid.NewGuid();
        targetEntity.BillingCustomerAccountId = Guid.NewGuid();
        targetEntity.BillingContactId = Guid.NewGuid();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenBillingContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.BillingCustomerAccountId = Guid.NewGuid();
        targetEntity.BillingContactId = Guid.NewGuid();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenGeneratorRepresentativeUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.CustomerGeneratorId = Guid.NewGuid();
        targetEntity.GeneratorRepresentativeId = Guid.NewGuid();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.CustomerGeneratorId = Guid.NewGuid();
        originalEntity.GeneratorRepresentativeId = Guid.NewGuid();
        originalEntity.BillingCustomerAccountId = Guid.NewGuid();
        originalEntity.BillingContactId = Guid.NewGuid();
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenNewSignatoryAdded()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.Signatories.Add(GenFu.GenFu.New<SignatoryContactEntity>());
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenSignatoryRemoved()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.Signatories.RemoveAt(0);
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenSignatoryDisabled()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.Signatories[0].IsAuthorized = false;
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenNewEmailDeliveryContactAdded()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.EmailDeliveryContacts.Add(GenFu.GenFu.New<EmailDeliveryContactEntity>());
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenEmailDeliveryContactRemoved()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.EmailDeliveryContacts.RemoveAt(0);
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenEmailDeliveryContactDisabled()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.EmailDeliveryContacts[0].IsAuthorized = false;
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenBillingCustomerAccountContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.ValidBillingConfiurationEntity.Clone();
        entity.BillingContactId = Guid.NewGuid();
        entity.BillingCustomerAccountId = Guid.NewGuid();
        var context = new BusinessContext<BillingConfigurationEntity>(entity, scope.ValidBillingConfiurationEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerAccountMatch(entity, index, false)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldEnableExistingIndex_WhenBillingCustomerAccountContactIsUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();

        targetEntity.BillingContactId = Guid.NewGuid();
        targetEntity.BillingCustomerAccountId = Guid.NewGuid();

        var existingIndices = GenFu.GenFu.ListOf<AccountContactReferenceIndexEntity>(2);
        existingIndices.ForEach(x => x.ReferenceEntityId = targetEntity.Id);
        existingIndices[0].AccountContactId = originalEntity.BillingContactId.Value;
        existingIndices[0].AccountId = originalEntity.BillingCustomerAccountId;
        existingIndices[0].IsDisabled = false;

        existingIndices[1].AccountContactId = targetEntity.BillingContactId.Value;
        existingIndices[1].AccountId = targetEntity.BillingCustomerAccountId;
        existingIndices[1].IsDisabled = true;

        scope.IndexProviderMock.SetupEntities(existingIndices);
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Update(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerAccountMatch(targetEntity, index, false)), It.IsAny<bool>()));
        scope.IndexProviderMock.Verify(s => s.Insert(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Task_ShouldEnableExistingIndex_WhenGeneratorRepresentativeUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();

        targetEntity.CustomerGeneratorId = Guid.NewGuid();
        targetEntity.GeneratorRepresentativeId = Guid.NewGuid();

        var existingIndices = GenFu.GenFu.ListOf<AccountContactReferenceIndexEntity>(2);
        existingIndices.ForEach(x => x.ReferenceEntityId = targetEntity.Id);
        existingIndices[0].AccountContactId = originalEntity.GeneratorRepresentativeId.Value;
        existingIndices[0].AccountId = originalEntity.CustomerGeneratorId;
        existingIndices[0].IsDisabled = false;

        existingIndices[1].AccountContactId = targetEntity.GeneratorRepresentativeId.Value;
        existingIndices[1].AccountId = targetEntity.CustomerGeneratorId;
        existingIndices[1].IsDisabled = true;

        scope.IndexProviderMock.SetupEntities(existingIndices);
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Update(It.Is<AccountContactReferenceIndexEntity>(index => IsGeneratorAccountMatch(targetEntity, index, false)), It.IsAny<bool>()));
        scope.IndexProviderMock.Verify(s => s.Insert(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenGeneratorRepresentativeIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.CustomerGeneratorId = Guid.NewGuid();
        targetEntity.GeneratorRepresentativeId = Guid.NewGuid();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsGeneratorAccountMatch(targetEntity, index, false)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenAdded_EmailDeliveryContacts()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.Signatories = new();
        var targetEntity = originalEntity.Clone();
        originalEntity.EmailDeliveryContacts = new();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsEmailDeliveryContactsMatch(targetEntity, index, false)), true));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenDeleted_EmailDeliveryContacts()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.Signatories = new();
        var targetEntity = originalEntity.Clone();
        targetEntity.EmailDeliveryContacts = new();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsEmailDeliveryContactsMatch(originalEntity, index, true)), true));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenAdded_Signatories()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.EmailDeliveryContacts = new();
        var targetEntity = originalEntity.Clone();
        originalEntity.Signatories = new();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsSignatoryContactsMatch(targetEntity, index, false)), true));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenDeleted_Signatories()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.EmailDeliveryContacts = new();
        var targetEntity = originalEntity.Clone();
        targetEntity.Signatories = new();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsSignatoryContactsMatch(originalEntity, index, true)), true));
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_NoEmailDeliveryContact_AddedDeleted()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.Signatories = new();
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(s => s.Insert(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
        scope.IndexProviderMock.Verify(s => s.Update(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_NoSignatory_AddedDeleted()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.EmailDeliveryContacts = new();
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(s => s.Insert(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
        scope.IndexProviderMock.Verify(s => s.Update(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Task_ShouldNotCreateNewIndex_WhenSignatories_NotAuthorized()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidBillingConfiurationEntity;
        originalEntity.EmailDeliveryContacts = new();
        var targetEntity = originalEntity.Clone();
        targetEntity.Signatories.ForEach(x => x.IsAuthorized = false);
        var context = new BusinessContext<BillingConfigurationEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsSignatoryContactsMatch(originalEntity, index, true)), true));
    }

    #region setup

    private bool IsBillingCustomerAccountMatch(BillingConfigurationEntity entity, AccountContactReferenceIndexEntity index, bool? isDisabled = false)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.BillingCustomerAccountId == index.AccountId &&
               entity.BillingContactId == index.AccountContactId &&
               isDisabled == index.IsDisabled;
    }

    private bool IsGeneratorAccountMatch(BillingConfigurationEntity entity, AccountContactReferenceIndexEntity index, bool? isDisabled = false)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.CustomerGeneratorId == index.AccountId &&
               entity.GeneratorRepresentativeId == index.AccountContactId &&
               isDisabled == index.IsDisabled;
    }

    private bool IsEmailDeliveryContactsMatch(BillingConfigurationEntity entity, AccountContactReferenceIndexEntity index, bool? isDisabled = false)
    {
        var isMatch = false;
        foreach (var emailDeliveryContact in entity.EmailDeliveryContacts)
        {
            isMatch = isMatch || (entity.Id == index.ReferenceEntityId &&
                                  entity.BillingCustomerAccountId == index.AccountId &&
                                  emailDeliveryContact.AccountContactId == index.AccountContactId &&
                                  isDisabled == index.IsDisabled);
        }

        return isMatch;
    }

    private bool IsSignatoryContactsMatch(BillingConfigurationEntity entity, AccountContactReferenceIndexEntity index, bool? isDisabled = false)
    {
        var isMatch = false;
        foreach (var signatoryContact in entity.Signatories)
        {
            isMatch = isMatch || (entity.Id == index.ReferenceEntityId &&
                                  signatoryContact.AccountId == index.AccountId &&
                                  signatoryContact.AccountContactId == index.AccountContactId &&
                                  isDisabled == index.IsDisabled);
        }

        return isMatch;
    }

    private class DefaultScope : TestScope<BillingConfigurationBillingCustomerContactIndexer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, AccountContactReferenceIndexEntity>> IndexProviderMock { get; } = new();

        public BillingConfigurationEntity ValidBillingConfiurationEntity =>
            new()
            {
                Id = Guid.NewGuid(),
                BillingConfigurationEnabled = true,
                BillingContactAddress = "123 Maple Ridge",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Er. Steve Martin",
                BillingCustomerAccountId = Guid.Parse("d4368508-d884-4aa3-9083-ab020f569a1e"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.NewGuid(),
                CustomerGeneratorName = "Kuvalis, Herman and Langworth",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = DateTimeOffset.Now,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = true,
                LastComment = "This is a sample comment",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "345 Altosa Drive",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Third Party Contact",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Manish M",
                UpdatedById = Guid.NewGuid().ToString(),
                EDIValueData = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Invoice Number",
                        EDIFieldValueContent = null,
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Policy Name",
                        EDIFieldValueContent = null,
                    },
                },
                EmailDeliveryContacts = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        EmailAddress = "SimpleMail@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Singatory Person Name",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        EmailAddress = "SimpleMail01@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Singatory Person Name 01",
                    },
                },
                Signatories = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        IsAuthorized = true,
                        Address = "6557 Cortez Field",
                        Email = "Janae_Corkery95@gmail.com",
                        FirstName = "Johnnie Kunde Sr.I",
                        PhoneNumber = "510-297-3998",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        IsAuthorized = true,
                        Address = "111 Main St",
                        Email = "Janae_Cater95@gmail.com",
                        FirstName = "Johnnie Kunde Sr.II",
                        PhoneNumber = "510-297-3998",
                    },
                },
                MatchCriteria = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.NotSet,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.NotSet,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.NotSet,
                    },
                },
            };
    }

    #endregion
}
