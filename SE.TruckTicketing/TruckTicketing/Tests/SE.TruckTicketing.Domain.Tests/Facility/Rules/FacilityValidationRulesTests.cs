using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Facilities.Rules;
using SE.TruckTicketing.Contracts;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.TruckTicketing.Domain.Tests.Facility.Rules;

[TestClass]
public class FacilityValidationRulesTests
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    [TestMethod]
    [TestCategory("Unit")]
    public void SequenceValidationBusinessProfileRule_Inherits()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ValidationRuleBase<BusinessContext<FacilityEntity>>));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityEntityValidationBusinessProfileRule_Run_Exist_ExpectPass()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.SiteId = FacilityConstants.SiteId.DVFST;
        scope.TestContext.Target.Name = FacilityConstants.Name.CanadianFacility;
        scope.TestContext.Target.Type = FacilityType.Lf;
        scope.TestContext.Target.LegalEntity = FacilityConstants.LegalEntity.Canada;
        scope.TestContext.Target.Province = StateProvince.AB;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityEntityValidationBusinessProfileRule_Run_RequiredField_SiteId_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();
        scope.TestContext.Target.SiteId = string.Empty;
        scope.TestContext.Target.Name = FacilityConstants.Name.CanadianFacility;
        scope.TestContext.Target.Type = FacilityType.Lf;
        scope.TestContext.Target.LegalEntity = FacilityConstants.LegalEntity.Canada;
        scope.TestContext.Target.Province = StateProvince.AB;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.Facility_SiteId));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityEntityValidationBusinessProfileRule_Run_RequiredField_Name_ExpectError()
    {
        // arrange
        var scope = new DefaultScope();
        var errors = new List<ValidationResult>();

        scope.TestContext.Target.SiteId = FacilityConstants.SiteId.DVFST;
        scope.TestContext.Target.Name = string.Empty;
        scope.TestContext.Target.Type = FacilityType.Lf;
        scope.TestContext.Target.LegalEntity = FacilityConstants.LegalEntity.Canada;
        scope.TestContext.Target.Province = StateProvince.AB;
        // act
        await scope.InstanceUnderTest.Run(scope.TestContext, errors);
        // assert
        errors.Cast<ValidationResult<TTErrorCodes>>()
              .Select(result => result.ErrorCode)
              .Should()
              .ContainSingle(nameof(TTErrorCodes.Facility_Name));
    }

    private class DefaultScope : TestScope<ValidationRuleBase<BusinessContext<FacilityEntity>>>
    {
        public DefaultScope()
        {
            TestContext = GetTestContext();
            TestContext.Operation = Operation.Insert;
            InstanceUnderTest = new FacilityBasicValidationRule();
        }

        public BusinessContext<FacilityEntity> TestContext { get; }

        private BusinessContext<FacilityEntity> GetTestContext()
        {
            var target = new FacilityEntity();
            var original = new FacilityEntity();
            return new(target, original);
        }
    }

    private static class FacilityConstants
    {
        public static class SiteId
        {
            public const string DVFST = nameof(DVFST);
        }

        public static class LegalEntity
        {
            public const string Canada = nameof(Canada);
        }

        public static class Name
        {
            public const string CanadianFacility = nameof(CanadianFacility);
        }
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}
