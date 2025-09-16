using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Entities.UserProfile.Rules;

using Trident.Business;
using Trident.Testing.TestScopes;
using Trident.Validation;

namespace SE.Shared.Domain.Tests.Entities.UserProfile;

[TestClass]
public class UserProfileBasicValidationRuleTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void UserProfileBasicValidationRule_CanBeInstantiated()
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
    public async Task UserProfileBasicValidationRule_ShouldPass_ValidUserProfileEntity()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults.Should().BeEmpty();
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task UserProfileBasicValidationRule_ShouldFail_WhenDisplayNameIsNullOrEmpty(string displayName)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.Target.DisplayName = displayName;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_DisplayNameRequired));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task UserProfileBasicValidationRule_ShouldFail_WhenEmailIsNullOrEmpty(string email)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.Target.Email = email;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_EmailRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UserProfileBasicValidationRule_ShouldFail_WhenRolesIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.Target.Roles = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_RolesRequired));
    }

    [DataTestMethod]
    [TestCategory("Unit")]
    [DataRow("")]
    [DataRow(null)]
    public async Task UserProfileBasicValidationRule_ShouldFail_WhenExternalAuthIdIsNullOrEmpty(string externalAuthId)
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.Target.ExternalAuthId = externalAuthId;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_EmailRequired));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UserProfileBasicValidationRule_ShouldFail_WhenSpeficicFacilitiesAccessListIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithValidUserProfileEntity();
        context.Target.SpecificFacilityAccessAssignments = null;
        var validationResults = new List<ValidationResult>();

        // act
        await scope.InstanceUnderTest.Run(context, validationResults);

        // assert
        validationResults
           .Cast<ValidationResult<ErrorCodes>>()
           .Select(result => result.ErrorCode)
           .Should()
           .ContainSingle(nameof(ErrorCodes.UserProfile_SpecifcFacilityAccessListRequired));
    }

    private class DefaultScope : TestScope<UserProfileBasicValidationRules>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<UserProfileEntity> CreateContextWithValidUserProfileEntity()
        {
            return new(new()
            {
                Email = "jjackson@secure-energy.com",
                GivenName = "Jack",
                Surname = "Jackson",
                DisplayName = "Jack Jackson",
                ExternalAuthId = "12345",
                LocalAuthId = "",
            });
        }
    }
}
