using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.Shared.Domain.Entities.AdditionalServicesConfiguration.Rules;
using SE.TruckTicketing.Contracts;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.AdditionalServicesConfiguration;

[TestClass]
public class AdditionalServicesConfigurationRuleTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void AdditionalServicesConfigurationRulee_CanBeInstantiated()
    {
        //arrange
        var scope = new DefaultScope();

        //act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        //assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationRule_ShouldPass_ValidData()
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateAdditionalServicesConfigurationContext();
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationRule_ShouldFail_WhenFacilityIdIsNullOrEmpty()
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateAdditionalServicesConfigurationContext();
        context.Target.FacilityId = default;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults
           .Cast<ValidationResult<TTErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(TTErrorCodes.AdditionalServicesConfiguration_Facility));
    }
    
    private class DefaultScope : TestScope<AdditionalServicesConfigurationValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<AdditionalServicesConfigurationEntity> CreateAdditionalServicesConfigurationContext()
        {
            return new(new()
            {
                Name = "TestName",
                FacilityId = Guid.NewGuid(),
                FacilityName = "TestFacility",
                CustomerId = Guid.NewGuid(),
                CustomerName = "TestCustomer",
                ApplyZeroDollarPrimarySalesLine = false,
            });
        }
    }
}
