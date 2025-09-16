using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.InvoiceConfiguration;

[TestClass]
public class InvoiceConfigurationBasicValidationTest : TestScope<InvoiceConfigurationBasicValidationRules>
{
    [TestMethod]
    [TestCategory("Unit")]
    public void InvoiceConfigurationBasicValidationRules_CanBeInstantiated()
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
    public async Task InvoiceConfigurationBasicValidationRules_ShouldPass_ValidName()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.Name = scope.GenerateRandomAlphanumericString(10);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldPass_ValidDescription()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.Description = scope.GenerateRandomAlphanumericString(55);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldFail_IfNameLengthIsGreaterThan150Characters()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.Name = scope.GenerateRandomAlphanumericString(160);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.InvoiceConfiguration_Name_Length));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldFail_ValidNameRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.Name = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.InvoiceConfiguration_Name_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldFail_IfDescriptionLengthIsGreaterThan500Characters()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.Description = scope.GenerateRandomAlphanumericString(510);
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.InvoiceConfiguration_Description_Length));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldFail_ValidCustomerRequired()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.CustomerId = default;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.InvoiceConfiguration_Customer_Required));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldFail_DisableSelectAll_NoFacilitySelected()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.AllFacilities = false;
        context.Target.Facilities = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .Contain(TTErrorCodes.InvoiceConfiguration_Facility_Required);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationBasicValidationRules_ShouldPass_EnableSelectAll_NoFacilitySelected()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidInvoiceConfigurationEntity();
        context.Target.AllFacilities = true;
        context.Target.Facilities = new();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .NotContain(TTErrorCodes.InvoiceConfiguration_Facility_Required);
    }

    private class DefaultScope : TestScope<InvoiceConfigurationBasicValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<InvoiceConfigurationEntity> CreateContextWithValidInvoiceConfigurationEntity()
        {
            return new(new()
            {
                Id = Guid.NewGuid(),
                BusinessUnitId = "AA-100050",
                AllFacilities = false,
                AllServiceTypes = false,
                AllSourceLocations = false,
                AllSubstances = false,
                AllWellClassifications = false,
                CatchAll = false,
                CustomerId = Guid.NewGuid(),
                CustomerName = "QQ Generator/Customer 01",
                Description = "This is test invoice configuration",
                IncludeInternalDocumentAttachment = true,
                IncludeExternalDocumentAttachment = true,
                IsSplitByFacility = false,
                IsSplitByServiceType = false,
                IsSplitBySourceLocation = false,
                IsSplitBySubstance = false,
                IsSplitByWellClassification = false,
                Name = "TT Petro Canada",
                Facilities = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                FacilityCode = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "PetroCanada",
                        "Combo",
                    },
                },
                ServiceTypes = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                SourceLocationIdentifier = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Id-001",
                        "Id-002",
                    },
                },
                SourceLocations = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                SplitEdiFieldDefinitions = null,
                SplittingCategories = null,
                WellClassifications = new()
                {
                    Key = Guid.NewGuid(),
                    List = new() { "Drilling" },
                },
                PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                Permutations = new()
                {
                    new()
                    {
                        Name = "TT Petro Canada",
                        SourceLocation = "Test SourceLocation A",
                        ServiceType = "Test ServiceType A",
                        WellClassification = "All",
                        Substance = "All",
                        Facility = "Facility A",
                    },
                },
                SubstancesName = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Substance01",
                        "Substance02",
                    },
                },
                ServiceTypesName = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        "Service01",
                        "Service02",
                    },
                },
                Substances = new()
                {
                    Key = Guid.NewGuid(),
                    List = new()
                    {
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                    },
                },
                CreatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                UpdatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                CreatedBy = "Panth Shah",
                UpdatedBy = "Panth Shah",
                CreatedById = Guid.NewGuid().ToString(),
                UpdatedById = Guid.NewGuid().ToString(),
            });
        }

        public string GenerateRandomAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@!#$%^&*(){}_+=";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());

            return randomString;
        }
    }
}
