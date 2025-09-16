using System.Net.Http;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.Services;

using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Tests.Services.Account;

[TestClass]
public class UserProfileServiceTests
{
    private class DefaultScope : ServiceTestScope<IUserProfileService>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new TestUserProfileService(UniqueServiceName,
                                                           LoggerMock.Object,
                                                           HttpClientFactoryMock.Object);
        }

        public Mock<ILogger<UserProfileService>> LoggerMock { get; } = new();

        public Permission TestPermissionModel { get; } = new();

        public UserProfile TestUserProfile { get; } = new();
    }

    [Service("TestService", Service.Resources.userProfiles)]
    private class TestUserProfileService : UserProfileService
    {
        public TestUserProfileService(string serviceName,
                                      ILogger<UserProfileService> logger,
                                      IHttpClientFactory httpClientFactory) : base(logger,
                                                                                   httpClientFactory)
        {
            HttpServiceName = serviceName;
        }
    }
}
