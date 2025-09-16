using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationMatchPredicateUniqueConstraintCheckerTaskTest : TestScope<MatchPredicateUniqueConstraintCheckerTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenBillingConfigurationIsDefaultConfigurationFalse()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.IsDefaultConfiguration = false;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenBillingConfigurationIncludeForAutomationTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.IncludeForAutomation = true;
        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfBillingConfigurationIsDefaultConfigurationTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.IsDefaultConfiguration = true;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_OnlyWhenBillingConfigurationIncludeForAutomationFalse()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.IncludeForAutomation = false;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomerAndGenerator_DuplicateMatchCriteriaExist()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var billingConfigurationWithSameCustomerAndGenerator = context.Target.Clone();
        context.Target.Id = Guid.NewGuid();
        context.Target.Facilities = new();
        var operationStage = scope.InstanceUnderTest.Stage;

        BillingConfigurationEntity[] entities = { billingConfigurationWithSameCustomerAndGenerator };
        scope.SetupOverlappingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true)
               .Should().BeFalse();

        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomerAndGenerator_NoDuplicateMatchCriteria()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var billingConfigurationWithSameCustomerAndGenerator = scope.DefaultBillingConfiguration.Clone();

        context.Target.Id = Guid.NewGuid();
        context.Target.Facilities = new();
        context.Target.MatchCriteria = new();
        context.Target.MatchCriteria.Add(new()
        {
            Id = Guid.NewGuid(),
            Stream = Stream.Pipeline,
            StreamValueState = MatchPredicateValueState.Value,
            IsEnabled = true,
            ServiceType = null,
            ServiceTypeId = null,
            ServiceTypeValueState = MatchPredicateValueState.Any,
            SourceIdentifier = null,
            SourceLocationId = null,
            SourceLocationValueState = MatchPredicateValueState.Any,
            SubstanceId = null,
            SubstanceName = null,
            SubstanceValueState = MatchPredicateValueState.Any,
            WellClassification = WellClassifications.Production,
            WellClassificationState = MatchPredicateValueState.Value,
            StartDate = null,
            EndDate = null,
            Hash = "1b4f0e9851971998e732078544c96b36c3d01cedf7caa332359d6f1d83567014",
        });

        var operationStage = scope.InstanceUnderTest.Stage;

        BillingConfigurationEntity[] entities = { billingConfigurationWithSameCustomerAndGenerator };
        scope.SetupOverlappingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true)
               .Should().BeTrue();

        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomerAndGenerator_InactiveMatchCriteria_NoDuplicate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var billingConfigurationWithSameCustomerAndGenerator = scope.DefaultBillingConfiguration.Clone();

        context.Target.Id = Guid.NewGuid();
        context.Target.Facilities = new();
        billingConfigurationWithSameCustomerAndGenerator.MatchCriteria.ForEach(x => x.IsEnabled = false);

        var operationStage = scope.InstanceUnderTest.Stage;

        BillingConfigurationEntity[] entities = { billingConfigurationWithSameCustomerAndGenerator };
        scope.SetupOverlappingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true)
               .Should().BeTrue();

        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomerAndGenerator_MatchCriteriaStartAndEndDateValid_DuplicateExists()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var billingConfigurationWithSameCustomerAndGenerator = scope.DefaultBillingConfiguration.Clone();

        context.Target.Id = Guid.NewGuid();
        billingConfigurationWithSameCustomerAndGenerator.MatchCriteria.ForEach(x => x.StartDate = new DateTime(2022, 1, 17, 18, 11, 30));
        billingConfigurationWithSameCustomerAndGenerator.MatchCriteria.ForEach(x => x.EndDate = new DateTime(2025, 1, 17, 18, 11, 30));

        context.Target.MatchCriteria.ForEach(x => x.StartDate = new DateTime(2022, 5, 17, 18, 11, 30));
        context.Target.MatchCriteria.ForEach(x => x.EndDate = new DateTime(2022, 12, 17, 18, 11, 30));
        var operationStage = scope.InstanceUnderTest.Stage;

        BillingConfigurationEntity[] entities = { billingConfigurationWithSameCustomerAndGenerator };
        scope.SetupOverlappingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true)
               .Should().BeFalse();

        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomerAndGenerator_MatchCriteriaStartAndEndDateOutOfRange_NoDuplicateExists()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var billingConfigurationWithSameCustomerAndGenerator = scope.DefaultBillingConfiguration.Clone();

        context.Target.Id = Guid.NewGuid();
        billingConfigurationWithSameCustomerAndGenerator.MatchCriteria.ForEach(x => x.StartDate = new DateTime(2020, 1, 17, 18, 11, 30));
        billingConfigurationWithSameCustomerAndGenerator.MatchCriteria.ForEach(x => x.EndDate = new DateTime(2021, 1, 17, 18, 11, 30));

        context.Target.MatchCriteria.ForEach(x => x.StartDate = new DateTime(2022, 5, 17, 18, 11, 30));
        context.Target.MatchCriteria.ForEach(x => x.EndDate = new DateTime(2022, 12, 17, 18, 11, 30));
        var operationStage = scope.InstanceUnderTest.Stage;

        BillingConfigurationEntity[] entities = { billingConfigurationWithSameCustomerAndGenerator };
        scope.SetupOverlappingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        context.GetContextBagItemOrDefault(BillingConfigurationWorkflowContextBagKeys.MatchPredicateHashIsUnique, true)
               .Should().BeTrue();

        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    private class DefaultScope : TestScope<MatchPredicateUniqueConstraintCheckerTask>
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
                IsDefaultConfiguration = false,
                IncludeForAutomation = true,
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
                Facilities = new()
                {
                    Key = Guid.NewGuid(),
                    List = new() { Guid.NewGuid() },
                },
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
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Pipeline,
                        StreamValueState = MatchPredicateValueState.Value,
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Completions,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
                    },
                },
            };

        public readonly Mock<IMatchPredicateManager> MatchPredicateManager = new();

        public DefaultScope()
        {
            InstanceUnderTest = new(MatchPredicateManager.Object);
        }

        public void SetupOverlappingBillingConfiguration(params BillingConfigurationEntity[] entities)
        {
            MatchPredicateManager.Setup(x => x.GetOverlappingBillingConfigurations(It.IsAny<BillingConfigurationEntity>()))
                                 .ReturnsAsync(entities.ToList());
        }

        public BusinessContext<BillingConfigurationEntity> CreateBillingConfigurationContext()
        {
            return new(DefaultBillingConfiguration);
        }
    }
}
