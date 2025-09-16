using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Tasks;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Extensions;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationGenerateMatchPredicateHashTaskTest : TestScope<GenerateMatchPredicateHashTask>
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
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaIdFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaStartDateFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaEndDateFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaSourceIdentifierFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaSubstanceNameFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaServiceTypeFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ExcludeMatchCriteriaExistingHashFromHash()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_GenerateHashForMatchPredicate_ValidHashGenerated()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        var existingDefaultBillingConfigurationForSameCustomer = context.Target.Clone();
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().EndDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().StartDate = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SourceIdentifier = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().SubstanceName = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().ServiceType = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Hash = null;
        existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First().Id = default;

        var hash = scope.GenerateHash(existingDefaultBillingConfigurationForSameCustomer.MatchCriteria.First());
        context.Target.Id = Guid.NewGuid();
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(string.Equals(context.Target.MatchCriteria.First().Hash, hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_NoHashGeneratedForMatchPredicate_WhenMatchPredicateIsNotEnabled()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.Id = Guid.NewGuid();
        context.Target.MatchCriteria.First().Hash = null;
        context.Target.MatchCriteria.First().IsEnabled = false;
        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(string.IsNullOrEmpty(context.Target.MatchCriteria.First().Hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_HashGeneratedForMatchPredicate_WhenMatchPredicateHasInvalidStartAndEndDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.Id = Guid.NewGuid();
        context.Target.MatchCriteria.First().Hash = null;
        context.Target.MatchCriteria.First().IsEnabled = true;
        context.Target.MatchCriteria.First().StartDate = new DateTime(2025, 1, 17, 18, 11, 30);
        context.Target.MatchCriteria.First().EndDate = new DateTime(2027, 1, 17, 18, 11, 30);

        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsTrue(!string.IsNullOrEmpty(context.Target.MatchCriteria.First().Hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    public async Task Task_HashGeneratedForMatchPredicate_WhenMatchPredicateHasNoStartAndEndDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateBillingConfigurationContext();
        context.Target.Id = Guid.NewGuid();
        context.Target.MatchCriteria.First().Hash = null;
        context.Target.MatchCriteria.First().IsEnabled = true;
        context.Target.MatchCriteria.First().StartDate = null;
        context.Target.MatchCriteria.First().EndDate = null;

        var operationStage = scope.InstanceUnderTest.Stage;

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        Assert.IsFalse(string.IsNullOrEmpty(context.Target.MatchCriteria.First().Hash));
        operationStage.Should().Be(OperationStage.BeforeInsert | OperationStage.BeforeUpdate);
    }

    private class DefaultScope : TestScope<GenerateMatchPredicateHashTask>
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
                        ServiceType = "Test ServiceType",
                        ServiceTypeId = Guid.NewGuid(),
                        ServiceTypeValueState = MatchPredicateValueState.Value,
                        SourceIdentifier = "aaaa-aaaa-aaaa",
                        SourceLocationId = Guid.NewGuid(),
                        SourceLocationValueState = MatchPredicateValueState.Value,
                        SubstanceId = Guid.NewGuid(),
                        SubstanceName = "Test Sunstance",
                        SubstanceValueState = MatchPredicateValueState.Value,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = new DateTime(2020, 1, 17, 18, 11, 30),
                        EndDate = new DateTime(2025, 1, 17, 18, 11, 30),
                        Hash = "0037093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                    },
                },
            };

        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public string GenerateHash(MatchPredicateEntity matchPredicateClone)
        {
            using var sHa256 = SHA256.Create();
            return Convert.ToHexString(sHa256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(matchPredicateClone))));
        }

        public BusinessContext<BillingConfigurationEntity> CreateBillingConfigurationContext()
        {
            return new(DefaultBillingConfiguration);
        }
    }
}
