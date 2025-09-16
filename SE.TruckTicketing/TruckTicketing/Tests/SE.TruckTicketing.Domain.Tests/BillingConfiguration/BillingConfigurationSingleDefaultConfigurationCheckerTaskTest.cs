using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationSingleDefaultConfigurationCheckerTaskTest : TestScope<BillingConfigurationSingleDefaultConfigurationCheckerTask>
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldRun_OnlyWhenBillingConfigurationIsDefaultConfigurationTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        scope.InstanceUnderTest.RunOrder.Should().BePositive();
        scope.InstanceUnderTest.Stage.Should().Be(OperationStage.BeforeUpdate | OperationStage.BeforeInsert);
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task Task_ShouldNotRun_IfBillingConfigurationIsDefaultConfigurationFalse()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.IsDefaultConfiguration = false;

        // act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);
        // assert
        shouldRun.Should().BeFalse();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForSameCustomer_IsDefaultConfigurationShouldSetFalse_IfTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        //Existing Default Billing Configuration for same Customer
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.IsDefaultConfiguration = true;
        existingDefaultBillingConfigurationForSameCustomer.Id = Guid.NewGuid();

        //Existing Default Billing Configuration for different customer
        var existingDefaultBillingConfigurationForDifferentCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForDifferentCustomer.IsDefaultConfiguration = true;
        existingDefaultBillingConfigurationForDifferentCustomer.BillingCustomerAccountId = Guid.NewGuid();
        existingDefaultBillingConfigurationForDifferentCustomer.Id = Guid.NewGuid();

        BillingConfigurationEntity[] entities = { existingDefaultBillingConfigurationForSameCustomer, existingDefaultBillingConfigurationForDifferentCustomer };
        scope.SetupExistingBillingConfiguration(entities);

        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var billingConfiguration = context.Target;

        // assert
        result.Should().BeTrue();
        existingDefaultBillingConfigurationForSameCustomer.IsDefaultConfiguration.Should().BeFalse();
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_ExistingBillingConfigurationForDifferentCustomer_IsDefaultConfigurationShouldNotSetFalse_IfTrue()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        //Existing Default Billing Configuration for same Customer
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.IsDefaultConfiguration = true;
        existingDefaultBillingConfigurationForSameCustomer.Id = Guid.NewGuid();

        //Existing Default Billing Configuration for different customer
        var existingDefaultBillingConfigurationForDifferentCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForDifferentCustomer.IsDefaultConfiguration = true;
        existingDefaultBillingConfigurationForDifferentCustomer.BillingCustomerAccountId = Guid.NewGuid();
        existingDefaultBillingConfigurationForDifferentCustomer.Id = Guid.NewGuid();

        BillingConfigurationEntity[] entities = { existingDefaultBillingConfigurationForSameCustomer, existingDefaultBillingConfigurationForDifferentCustomer };
        scope.SetupExistingBillingConfiguration(entities);
        // act
        var result = await scope.InstanceUnderTest.Run(context);
        var billingConfiguration = context.Target;

        // assert
        result.Should().BeTrue();
        existingDefaultBillingConfigurationForDifferentCustomer.IsDefaultConfiguration.Should().BeTrue();
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    private class DefaultScope : TestScope<BillingConfigurationSingleDefaultConfigurationCheckerTask>
    {
        public readonly Mock<IProvider<Guid, BillingConfigurationEntity>> BillingConfigurationProviderMock = new();

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

        public DefaultScope()
        {
            InstanceUnderTest = new(BillingConfigurationProviderMock.Object);
        }

        public void SetupExistingBillingConfiguration(params BillingConfigurationEntity[] entities)
        {
            BillingConfigurationProviderMock.SetupEntities(entities);
        }

        public BusinessContext<BillingConfigurationEntity> CreateBillingConfigurationContext()
        {
            return new(DefaultBillingConfiguration);
        }
    }
}
