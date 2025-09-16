using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.UI.Contracts.Services;
using SE.TruckTicketing.UI.Services;

using Trident.Contracts.Api.Client;
using Trident.Testing.TestScopes;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Tests.Services;

[TestClass]
public class UserProfileServiceTests
{
    [TestMethod]
    public async Task UserProfileService_GetSignatureUploadUrl_NewUrl()
    {
        // arrange
        var scope = new DefaultScope();
        scope.InstanceUnderTest.MockedResponse = new() { Model = new() { Uri = "https://example.org/" } };

        // act
        var url = await scope.InstanceUnderTest.GetSignatureUploadUrl();

        // assert
        url.Should().Be("https://example.org/");
    }

    [TestMethod]
    public async Task UserProfileService_GetSignatureDownloadUrl_NewUrl()
    {
        // arrange
        var scope = new DefaultScope();
        scope.InstanceUnderTest.MockedResponse = new() { Model = new() { Uri = "https://example.org/" } };

        // act
        var url = await scope.InstanceUnderTest.GetSignatureDownloadUrl();

        // assert
        url.Should().Be("https://example.org/");
    }

    public class DefaultScope : TestScope<UserProfileServiceSurrogate>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Logger.Object, HttpClientFactory.Object);
        }

        public Mock<ILogger<UserProfileService>> Logger { get; set; } = new();

        public Mock<IHttpClientFactory> HttpClientFactory { get; set; } = new();
    }

    [Service(Service.SETruckTicketingApi, Service.Resources.userProfiles)]
    public class UserProfileServiceSurrogate : UserProfileService
    {
        public UserProfileServiceSurrogate(ILogger<UserProfileService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
        {
        }

        public Response<UriDto> MockedResponse { get; set; }

        protected override Task<Response<T>> SendRequest<T>(string method, string route, object data = null)
        {
            var json = JsonConvert.SerializeObject(MockedResponse);
            var response = JsonConvert.DeserializeObject<Response<T>>(json);
            return Task.FromResult(response);
        }
    }
}
