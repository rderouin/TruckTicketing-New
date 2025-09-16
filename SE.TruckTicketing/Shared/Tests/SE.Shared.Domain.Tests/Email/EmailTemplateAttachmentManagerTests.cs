using Moq;

using SE.Shared.Domain.EmailTemplates;

using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Email;

[TestClass]
public class EmailTemplateAttachmentManagerTests
{
    [TestMethod]
    public void GetUploadUrl_Should_Generate_CorrectFileName()
    {
        // arrange
        
        // act
        
        // assert
    }
    
    public class DefaultScope : TestScope<IEmailTemplateAttachmentManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new EmailTemplateAttachmentManager(AttachmentBlobStorageMock.Object);
        }

        public Mock<IEmailTemplateAttachmentBlobStorage> AttachmentBlobStorageMock { get; } = new();
    }
}
