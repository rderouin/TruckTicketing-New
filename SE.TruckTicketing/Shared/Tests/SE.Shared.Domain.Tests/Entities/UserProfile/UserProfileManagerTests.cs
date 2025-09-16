using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.Entities.UserProfile;
using SE.Shared.Domain.Infrastructure;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.UserProfile;

[TestClass]
public class UserProfileManagerTests
{
    [TestMethod("UserProfileManager can return a new upload URL with a signed token.")]
    public async Task UserProfileManager_GetSignatureUploadUri_UrlWithToken()
    {
        // arrange
        var scope = new DefaultScope();
        var userId = Guid.NewGuid().ToString();

        // act
        var url = await scope.InstanceUnderTest.GetSignatureUploadUri(userId);

        // assert
        url.Should().Be("https://example.org/");
    }

    [TestMethod("UserProfileManager can return a new download URL with a signed token.")]
    public async Task UserProfileManager_GetSignatureDownloadUri_UrlWithToken()
    {
        // arrange
        var scope = new DefaultScope();
        var userId = Guid.NewGuid().ToString();

        // act
        var url = await scope.InstanceUnderTest.GetSignatureDownloadUri(userId);

        // assert
        url.Should().Be("https://example.org/");
        scope.SignatureUploadBlobStorage.Verify(s => s.Exists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class DefaultScope : TestScope<UserProfileManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Logger.Object, Provider.Object, SignatureUploadBlobStorage.Object, ValidationManager.Object, WorkflowManager.Object);
            SignatureUploadBlobStorage.Setup(s => s.GetUploadUri(It.IsAny<string>(), It.IsAny<string>())).Returns(new Uri("https://example.org/"));
            SignatureUploadBlobStorage.Setup(s => s.GetDownloadUri(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new Uri("https://example.org/"));
            SignatureUploadBlobStorage.Setup(s => s.Exists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        public Mock<ILog> Logger { get; } = new();

        public Mock<IProvider<Guid, UserProfileEntity>> Provider { get; } = new();

        public Mock<ISignatureUploadBlobStorage> SignatureUploadBlobStorage { get; } = new();

        public Mock<IValidationManager<UserProfileEntity>> ValidationManager { get; } = new();

        public Mock<IWorkflowManager<UserProfileEntity>> WorkflowManager { get; } = new();
    }
}
