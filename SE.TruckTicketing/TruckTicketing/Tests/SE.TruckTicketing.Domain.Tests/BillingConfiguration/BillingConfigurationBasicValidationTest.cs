using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.BillingConfiguration.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class BillingConfigurationBasicValidationTest : TestScope<BillingConfigurationBasicValidationRules>
{
    [TestMethod]
    [TestCategory("Unit")]
    public void BillingConfigurationBasicValidationRules_CanBeInstantiated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        // assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldPass_ValidStartDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_WhenBillingConfigurationNameIsNullOrEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.Name = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_Name_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_WhenBillingConfigurationStartDateIsNullOrEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.StartDate = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_StartDate_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_WhenBillingConfigurationStartDateAfterEndDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.StartDate = DateTimeOffset.Now.AddDays(5);
        context.Target.EndDate = DateTimeOffset.Now;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_EndDate_GreaterThan_StartDate));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldPass_ValidMatchPredicateStartDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().StartDate = DateTimeOffset.Now.AddDays(1);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_WhenBillingConfigurationStartDateAfterMatchPredicateStartDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.StartDate = DateTimeOffset.Now.AddDays(5);
        context.Target.MatchCriteria.FirstOrDefault().StartDate = DateTimeOffset.Now;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Dates_WithIn));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_WhenBillingConfigurationStartEndDateOutsideMatchPredicateStartDateEndDate()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.StartDate = DateTimeOffset.Now;
        context.Target.EndDate = DateTimeOffset.Now.AddDays(5);
        context.Target.MatchCriteria.FirstOrDefault().StartDate = DateTimeOffset.Now.AddDays(6);
        context.Target.MatchCriteria.FirstOrDefault().EndDate = DateTimeOffset.Now.AddDays(10);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Dates_WithIn));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_MatchPredicateWellClassificationValueState_Invalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().WellClassificationState = MatchPredicateValueState.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_WellClassificationState));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_MatchPredicateSubstanceValueState_Invalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().SubstanceValueState = MatchPredicateValueState.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_SubstanceValueState));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_MatchPredicateSourceLocationValueState_Invalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().SourceLocationValueState = MatchPredicateValueState.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_SourceLocationValueState));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_MatchPredicateServiceTypeValueState_Invalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().ServiceTypeValueState = MatchPredicateValueState.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_ServiceTypeValueState));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task BillingConfigurationBasicValidationRules_ShouldFail_MatchPredicateStreamValueState_Invalid()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidBillingConfigurationEntity();
        context.Target.MatchCriteria.FirstOrDefault().StreamValueState = MatchPredicateValueState.Unspecified;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.BillingConfiguration_MatchCriteria_Invalid_StreamValueState));
    }

    private class DefaultScope : TestScope<BillingConfigurationBasicValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<BillingConfigurationEntity> CreateContextWithValidBillingConfigurationEntity()
        {
            return new(new()
            {
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
                Name = "Auto Generated Billing Configuration",
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
            });
        }
    }
}
