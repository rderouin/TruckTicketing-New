using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.Services;
using SE.TruckTicketing.UI.ViewModels;

using Trident.Mapper;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Tests.Services.Account;

[TestClass]
public class RoleServiceTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task GetPermissionList_Should_Invoke_SendRequest_With_The_Expected_Request()
    {
        // arrange
        var scope = new DefaultScope();
        var accountType = string.Empty;
        scope.ExpectedUri = $"{scope.BaseAddress}/permissions/";
        scope.ExpectedRouteMethod = "get";
        scope.ExpectedRequestContent = JsonConvert.SerializeObject(scope.TestPermissionModel);
        scope.TestHttpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(scope.TestPermissionModel));

        // act
        var result = await scope.InstanceUnderTest.GetPermissionList();

        // assert
        Assert.IsInstanceOfType(result, typeof(List<PermissionViewModel>));
        Assert.IsNotNull(result);
    }

    private class DefaultScope : ServiceTestScope<IRoleService>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new TestRoleService(UniqueServiceName,
                                                    LoggerMock.Object,
                                                    HttpClientFactoryMock.Object,
                                                    MapperMock.Object);
        }

        public Mock<IMapperRegistry> MapperMock { get; } = new();

        public Mock<ILogger<RoleService>> LoggerMock { get; } = new();

        public Permission TestPermissionModel { get; } = new();

        public Role TestRole { get; } = new();
    }

    [Service("TestService", Service.Resources.roles)]
    private class TestRoleService : RoleService
    {
        public TestRoleService(string serviceName,
                               ILogger<RoleService> logger,
                               IHttpClientFactory httpClientFactory,
                               IMapperRegistry mapper) : base(logger,
                                                              httpClientFactory, mapper)
        {
            HttpServiceName = serviceName;
        }
    }
}
