using System;
using System.Net.Mail;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using SE.Shared.Domain.EmailTemplates;

using Trident.IoC;
using Trident.Testing.TestScopes;

namespace SE.Shared.Domain.Tests.Email;

[TestClass]
public class EmailTemplateSenderTests
{
    [TestMethod]
    public async Task Processor_ShouldSetSuccessfulMessage_WhenSendIsCalled()
    {
        // arrange
        var scope = new DefaultScope();
        var request = GenFu.GenFu.New<EmailTemplateDeliveryRequest>();

        // act
        await scope.InstanceUnderTest.SendEmail(request);

        // assert
        scope.EmailProcessorMock.Verify(processor => processor.AfterSend(It.Is<EmailTemplateProcessingContext>(context => context.IsMessageSent)));
    }

    [TestMethod]
    public async Task Processor_ShouldSetException_WhenSendFails()
    {
        // arrange
        var scope = new DefaultScope();
        var request = GenFu.GenFu.New<EmailTemplateDeliveryRequest>();
        scope.SmtpClientProvider.Setup(provider => provider.Send(It.IsAny<MailMessage>()))
             .Throws<Exception>();

        // act
        await scope.InstanceUnderTest.SendEmail(request);

        // assert
        scope.EmailProcessorMock.Verify(processor => processor.AfterSend(It.Is<EmailTemplateProcessingContext>(context => !context.IsMessageSent)));
        scope.EmailProcessorMock.Verify(processor => processor.AfterSend(It.Is<EmailTemplateProcessingContext>(context => context.Exception != null)));
    }

    public class DefaultScope : TestScope<IEmailTemplateSender>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new EmailTemplateSender(ServiceLocatorMock.Object,
                                                        SmtpClientProvider.Object,
                                                        LoggerMock.Object);

            ServiceLocatorMock.Setup(locator => locator.GetNamed<IEmailTemplateProcessor>(It.IsAny<string>()))
                              .Returns(EmailProcessorMock.Object);
        }

        public Mock<IIoCServiceLocator> ServiceLocatorMock { get; } = new();

        public Mock<ISmtpClientProvider> SmtpClientProvider { get; } = new();

        public Mock<ILogger<EmailTemplateSender>> LoggerMock { get; } = new();

        public Mock<IEmailTemplateProcessor> EmailProcessorMock { get; } = new();
    }
}
