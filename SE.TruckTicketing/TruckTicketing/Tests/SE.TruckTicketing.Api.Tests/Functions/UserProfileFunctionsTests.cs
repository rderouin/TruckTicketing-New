using System;
using System.Net;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.UserProfile;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Api.Models;

using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class UserProfileFunctionsTests
{
    [TestMethod]
    public async Task UserProfileFunctions_GetUploadSignatureUri_NewUri()
    {
        // arrange
        var scope = new DefaultScope();
        var requestData = scope.CreateHttpRequest(default, default);

        // act
        var responseData = await scope.InstanceUnderTest.GetUploadSignatureUri(requestData);

        // assert
        var response = responseData.ReadJsonToObject<UriDto>();
        response.Result.Uri.Should().Be("https://example.org/");
    }

    [TestMethod]
    public async Task UserProfileFunctions_GetSignatureDownloadUri_NewUri()
    {
        // arrange
        var scope = new DefaultScope();
        var requestData = scope.CreateHttpRequest(default, default);

        // act
        var responseData = await scope.InstanceUnderTest.GetSignatureDownloadUri(requestData);

        // assert
        var response = responseData.ReadJsonToObject<UriDto>();
        response.Result.Uri.Should().Be("https://example.org/");
    }

    [TestMethod]
    public async Task UserProfileFunctions_GetUploadSignatureUri_WithErrors()
    {
        // arrange
        var scope = new DefaultScope();
        var requestData = scope.CreateHttpRequest(default, default);
        scope.Manager.Setup(m => m.GetSignatureUploadUri(It.IsAny<string>())).Throws<Exception>();

        // act
        var responseData = await scope.InstanceUnderTest.GetUploadSignatureUri(requestData);

        // assert
        responseData.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var response = responseData.ReadJsonToObject<UriDto>();
        response.Result.Should().BeNull();
    }

    [TestMethod]
    public async Task UserProfileFunctions_GetSignatureDownloadUri_WithErrors()
    {
        // arrange
        var scope = new DefaultScope();
        var requestData = scope.CreateHttpRequest(default, default);
        scope.Manager.Setup(m => m.GetSignatureDownloadUri(It.IsAny<string>())).Throws<Exception>();

        // act
        var responseData = await scope.InstanceUnderTest.GetSignatureDownloadUri(requestData);

        // assert
        responseData.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var response = responseData.ReadJsonToObject<UriDto>();
        response.Result.Should().BeNull();
    }

    public class DefaultScope : HttpTestScope<UserProfileFunctions>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Logger.Object, Mapper.Object, UserContextAccessor.Object, Manager.Object);
            Manager.Setup(m => m.GetSignatureUploadUri(It.IsAny<string>())).ReturnsAsync("https://example.org/");
            Manager.Setup(m => m.GetSignatureDownloadUri(It.IsAny<string>())).ReturnsAsync("https://example.org/");
            UserContextAccessor.Setup(c => c.UserContext).Returns(new UserContext { ObjectId = Guid.NewGuid().ToString() });
        }

        public Mock<ILog> Logger { get; set; } = new();

        public Mock<IMapperRegistry> Mapper { get; set; } = new();

        public Mock<IUserContextAccessor> UserContextAccessor { get; set; } = new();

        public Mock<IUserProfileManager> Manager { get; set; } = new();
    }
}
