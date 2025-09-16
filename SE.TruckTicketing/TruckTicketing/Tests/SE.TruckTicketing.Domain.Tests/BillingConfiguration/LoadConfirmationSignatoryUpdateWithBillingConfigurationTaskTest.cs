using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Extensions;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;

using Trident.Business;
using Trident.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class LoadConfirmationSignatoryUpdateWithBillingConfigurationTaskTest : TestScope<LoadConfirmationSignatoryUpdateWithBillingConfigurationTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenOperationIsUpdate()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = new();
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfBillingConfigurationOperation_OtherThanUpdate()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = new();
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Insert;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_BillingConfiguration_SignatoriesUpdated_ShouldRunUpdate_LoadConfirmationSignatories()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(2);
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        var operationStage = scope.InstanceUnderTest.Stage;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_BillingConfiguration_NoSignatoriesUpdate_ShouldNotRunUpdate_LoadConfirmationSignatories()
    {
        // arrange
        var scope = new DefaultScope();
        var signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = signatories;
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = signatories;
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        var operationStage = scope.InstanceUnderTest.Stage;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_BillingConfiguration_SignatoriesAdded_ShouldRunUpdate_LoadConfirmationSignatories()
    {
        // arrange
        var scope = new DefaultScope();
        var signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = null;
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = signatories;
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        var operationStage = scope.InstanceUnderTest.Stage;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_BillingConfiguration_SignatoriesRemovedInTarget_ShouldRunUpdate_LoadConfirmationSignatories()
    {
        // arrange
        var scope = new DefaultScope();
        var signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = signatories;
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = null;
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        var operationStage = scope.InstanceUnderTest.Stage;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeTrue();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_BillingConfiguration_NoSignatories_ShouldRunNoUpdate_LoadConfirmationSignatories()
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = null;
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = null;
        var context = scope.CreateBillingConfigurationContext(target, original);
        context.Operation = Operation.Update;
        var operationStage = scope.InstanceUnderTest.Stage;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.PostValidation);
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Unknown)]
    public async Task Task_BillingConfiguration_SignatoriesAdded_LoadConfirmationSignatories_Updated(LoadConfirmationStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = new();
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        target.Signatories.ForEach(x => x.IsAuthorized = true);
        var context = scope.CreateBillingConfigurationContext(target, original);
        var operationStage = scope.InstanceUnderTest.Stage;

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x =>
                                  {
                                      x.BillingConfigurationId = context.Target.Id;
                                      x.Status = status;
                                      x.SignatoryNames = null;
                                      x.Signatories = null;
                                  });

        scope.SetupExistingLoadConfirmations(loadConfirmations.ToArray());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(loadConfirmations.All(x => x.SignatoryNames.HasText()));
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.Open)]
    [DataRow(LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation)]
    [DataRow(LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.Unknown)]
    public async Task Task_BillingConfiguration_SignatoriesRemoved_LoadConfirmationSignatories_Updated(LoadConfirmationStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(2);
        target.Signatories.ForEach(x => x.IsAuthorized = true);
        var context = scope.CreateBillingConfigurationContext(target, original);
        var operationStage = scope.InstanceUnderTest.Stage;

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x =>
                                  {
                                      x.BillingConfigurationId = context.Target.Id;
                                      x.Status = status;
                                      x.SignatoryNames = null;
                                      x.Signatories = null;
                                  });

        scope.SetupExistingLoadConfirmations(loadConfirmations.ToArray());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(loadConfirmations.All(x => x.SignatoryNames.HasText()));
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void)]
    public async Task Task_BillingConfiguration_SignatoriesAdded_PostedVoidLoadConfirmation_NoDataToUpdate(LoadConfirmationStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = new();
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var context = scope.CreateBillingConfigurationContext(target, original);
        var operationStage = scope.InstanceUnderTest.Stage;

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x =>
                                  {
                                      x.BillingConfigurationId = context.Target.Id;
                                      x.Status = status;
                                      x.SignatoryNames = null;
                                      x.Signatories = null;
                                  });

        scope.SetupExistingLoadConfirmations(loadConfirmations.ToArray());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(loadConfirmations.All(x => x.SignatoryNames.HasText()));
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Void)]
    public async Task Task_BillingConfiguration_SignatoriesRemoved_PostedVoidLoadConfirmation_NoDataToUpdate(LoadConfirmationStatus status)
    {
        // arrange
        var scope = new DefaultScope();
        var original = scope.DefaultBillingConfiguration;
        original.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(5);
        var target = scope.DefaultBillingConfiguration.Clone();
        target.Signatories = GenFu.GenFu.ListOf<SignatoryContactEntity>(2);
        var context = scope.CreateBillingConfigurationContext(target, original);
        var operationStage = scope.InstanceUnderTest.Stage;

        var loadConfirmations = GenFu.GenFu.ListOf<LoadConfirmationEntity>(5);
        loadConfirmations.ForEach(x =>
                                  {
                                      x.BillingConfigurationId = context.Target.Id;
                                      x.Status = status;
                                      x.SignatoryNames = null;
                                      x.Signatories = null;
                                  });

        scope.SetupExistingLoadConfirmations(loadConfirmations.ToArray());

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(loadConfirmations.All(x => x.SignatoryNames.HasText()));
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    private class DefaultScope : TestScope<LoadConfirmationSignatoryUpdateWithBillingConfigurationTask>
    {
        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                BillingConfigurationEnabled = true,
                BillingContactAddress = "599 Harry Square",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Dr. Eduardo Lesch",
                BillingCustomerAccountId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.NewGuid(),
                CustomerGeneratorName = "Kemmer, Maggio and Reynolds",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = DateTimeOffset.Now,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = true,
                LastComment = "new comment added",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "07958 Althea Ford",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Barbara McClure II",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Panth Shah",
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
                        EmailAddress = "Noble60@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Jenna Schroeder",
                    },
                },
                Signatories = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        IsAuthorized = true,
                        Address = "6557 Cortez Field",
                        Email = "Janae_Corkery95@gmail.com",
                        FirstName = "Johnnie Kunde Sr.",
                        LastName = null,
                        PhoneNumber = "510-297-3998",
                    },
                },
                MatchCriteria = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Landfill,
                        StreamValueState = MatchPredicateValueState.Value,
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

        private readonly Mock<IManager<Guid, LoadConfirmationEntity>> LoadConfirmationManagerMock = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationManagerMock.Object);
        }

        public void SetupExistingLoadConfirmations(params LoadConfirmationEntity[] entities)
        {
            LoadConfirmationManagerMock.SetupEntities(entities);
        }

        public BusinessContext<BillingConfigurationEntity> CreateBillingConfigurationContext(BillingConfigurationEntity target, BillingConfigurationEntity original = null)
        {
            return new(target, original);
        }
    }
}
