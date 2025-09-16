using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.Role;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Models.Accounts;

using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class RoleFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task RoleFunctions_CreateRole_OkResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new Role
        {
            Id = id,
            Deleted = false,
            Name = "Admin",
            Permissions = new(),
            PermissionDisplay = "Admin",
        });

        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new RoleEntity { Id = id };

        //Setup
        scope.RoleManagerMock.Setup(x => x.Save(It.IsAny<RoleEntity>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.RoleCreate(mockRequest);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<Role>();

        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    private class DefaultScope : HttpTestScope<RoleFunctions>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new[] { new ApiMapperProfile() });

            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    RoleManagerMock.Object);
        }

        public Mock<ILog> LogMock { get; } = new();

        public IMapperRegistry MapperRegistry { get; }

        public Mock<IManager<Guid, RoleEntity>> RoleManagerMock { get; } = new();
    }
}
