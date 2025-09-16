using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Navigation;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Tests.Services.Account;

[TestClass]
public class NavigationConfigurationServiceTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void NavigationConfigurationService_Accepts_Expected_Constructor_Args_And_Returns_An_NavigationConfigurationService_Instance()
    {
        // arrange & act
        var scope = new DefaultScope();

        // assert
        Assert.IsNotNull(scope.InstanceUnderTest);
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(NavigationConfigurationService));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public void NavigationConfigurationService_GetAuthFilteredNavigationConfiguration()
    {
        // arrange & act
        var scope = new DefaultScope();
        // arrange

        scope.ExpectedUri = $"{scope.BaseAddress}/navigation/search";
        scope.ExpectedRouteMethod = "post";
        scope.ExpectedRequestContent = JsonConvert.SerializeObject(scope.TestNavigationModelList);
        scope.TestHttpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(scope.TestNavigationModelList));

        //act
        var result = scope.InstanceUnderTest.GetAuthFilteredNavigationConfiguration("TruckTicketing", scope.User);
        // assert
        Assert.IsNotNull(scope.InstanceUnderTest);
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(NavigationConfigurationService));
        Assert.IsInstanceOfType(result.Result, typeof(NavigationModel));
    }

    private class DefaultScope : ServiceTestScope<INavigationConfigurationService>
    {
        public readonly List<NavigationModel> TestNavigationModelList = new();

        public DefaultScope()
        {
            InstanceUnderTest = new TestNavigationConfigurationService(UniqueServiceName,
                                                                       LoggerMock.Object,
                                                                       HttpClientFactoryMock.Object);

            TestNavigationModelList.Add(TestNavigationModel);
        }

        public NavigationModel TestNavigationModel { get; } = new() { NavigationItems = new() };

        public ClaimsPrincipal User { get; } = new(new ClaimsIdentity(new Claim[] { new(ClaimTypes.Name, "truckticketing.user") }, "SomeAuthType"));

        public Mock<ILogger<NavigationConfigurationService>> LoggerMock { get; } = new();

        private List<NavigationModel> GetTestModel()
        {
            return new()
            {
                new()
                {
                    NavigationItems = new(),
                },
            };
        }

        [Service("TestService", Service.Resources.navigation)]
        private class TestNavigationConfigurationService : NavigationConfigurationService
        {
            public TestNavigationConfigurationService(string serviceName,
                                                      ILogger<NavigationConfigurationService> logger,
                                                      IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
            {
                HttpServiceName = serviceName;
            }
        }
    }
}
