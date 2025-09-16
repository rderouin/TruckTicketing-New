using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.FacilityService.Rules;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceBasicValidationRuleTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityServiceBasicValidationRule_CanBeInstantiated()
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
    public async Task FacilityServiceBasicValidationRule_ShouldPass_ServiceTypeId_Required()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidFacilityServiceEntity();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityServiceBasicValidationRule_ShouldFail_When_ServiceTypeId_IsEmpty()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidFacilityServiceEntity();
        context.Target.ServiceTypeId = Guid.Empty;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.FacilityService_ServiceTypeIdRequired));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow(0)]
    [DataRow(-1)]
    public async Task FacilityServiceBasicValidationRule_ShouldFail_When_ServiceNumber_IsNegative(Int32 serviceNumber)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidFacilityServiceEntity();
        context.Target.ServiceNumber = serviceNumber;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.FacilityService_ServiceNumberPositive));
    }

    private class DefaultScope : TestScope<FacilityServiceBasicValidationRule>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<FacilityServiceEntity> CreateContextWithValidFacilityServiceEntity()
        {
            return new(new()
            {
                ServiceNumber = 987,
                ServiceTypeId = new("9341c977-b189-4b9b-b4c1-3049182efa58"),
                FacilityServiceNumber = "ADFST-10",
                Description = "This is a test facility",
                FacilityId = new("a74a8426-8cc1-491a-adc7-597fc3151302"),
            });
        }
    }
}
