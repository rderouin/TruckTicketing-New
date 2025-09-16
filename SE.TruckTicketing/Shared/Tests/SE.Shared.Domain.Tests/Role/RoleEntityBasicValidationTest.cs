using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Trident.Validation;

using SE.Shared.Domain.Entities.Role;
using SE.Shared.Domain.Entities.Role.Rules;

using Trident.Business;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Role;

[TestClass]
public class RoleEntityBasicValidationTest
{
    [TestMethod]
    public void RoleEntityBasicValidationRule_CanBeInstantiated()
    {
        //arrange
        var scope = new DefaultScope();

        //act
        var runOrder = scope.InstanceUnderTest.RunOrder;

        //assert
        runOrder.Should().BePositive();
    }

    [TestMethod]
    public async Task RoleEntityBasicValidationRule_ShouldPass_ValidRoleEntity()
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithRoleEntity();
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RoleEntityBasicValidationRule_ShouldFaIL_InValidRoleEntity()
    {
        //arrange
        var scope = new DefaultScope();
        var context = scope.CreateContextWithRoleEntity();
        context.Target.Name = string.Empty;
        var validationResults = new List<ValidationResult>();

        //act
        await scope.InstanceUnderTest.Run(context, validationResults);

        //assert
        validationResults.Should().NotBeEmpty();
    }

    private class DefaultScope : TestScope<RoleEntityBasicValidation>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }

        public BusinessContext<RoleEntity> CreateContextWithRoleEntity()
        {
            return new(new()
            {
                Name = "Jerry",
                Deleted = false,
                PermissionDisplay = "display",
                Permissions = new()
            });
        }
    }
}
