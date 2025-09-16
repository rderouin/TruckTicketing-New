using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Shared;
using SE.BillingService.Domain.InvoiceDelivery.Transport;
using SE.Shared.Domain.EmailTemplates;

using Trident.Contracts.Configuration;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Transport;

[TestClass]
public class SmtpInvoiceDeliveryTransportTests
{
    [TestMethod]
    public async Task SmtpInvoiceDeliveryTransport_CreateSmtpClient()
    {
        // arrange
        var unsafeScope = new UnsafeScope();

        // act
        var client = await unsafeScope.InstanceUnderTest.CreateSmtpClient();

        // assert
        var specificClient = (CustomSmtpClient)client;
        specificClient.Host.Should().Be("example.org");
        specificClient.Port.Should().Be(2525);
        specificClient.EnableSsl.Should().Be(true);
        var cred = (NetworkCredential)specificClient.Credentials!;
        cred.UserName.Should().Be("example_user");
        cred.Password.Should().Be("example_pass");
    }

    [TestMethod]
    public async Task SmtpInvoiceDeliveryTransport_Send()
    {
        // arrange
        var scope = new DefaultScope();
        var message = new MailMessageSurrogate
        {
            To = "to@example.org",
            Subject = "subject line",
            Body = "email text",
        };

        var part = new EncodedInvoicePart
        {
            DataStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message))),
        };

        var instructions = new InvoiceDeliveryTransportInstructions();

        // act
        await scope.InstanceUnderTest.Send(part, instructions);

        // assert
        var client = scope.InstanceUnderTest.Client;
        client.HasBeenCalled.Should().Be(true);
        client.Message.From.Should().Be("sender@example.org");
        client.Message.To.First().Should().Be("to@example.org");
        client.Message.Subject.Should().Be("subject line");
        client.Message.Body.Should().Be("email text");
    }

    private class DefaultScope : TestScope<SmtpInvoiceDeliveryTransportCustomized>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(AppSettings.Object);
            ConfigureAppSettings(AppSettings);
        }

        public Mock<IAppSettings> AppSettings { get; } = new();

        private static void ConfigureAppSettings(Mock<IAppSettings> mock)
        {
            var smtpConfig = new SmtpConfiguration
            {
                Hostname = "example.org",
                Port = 2525,
                Username = "example_user",
                Password = "example_pass",
                EnableSsl = true,
                Sender = "sender@example.org",
            };

            mock.Setup(m => m.GetSection<SmtpConfiguration>(It.IsAny<string>())).Returns(smtpConfig);
        }
    }

    private class UnsafeScope : TestScope<SmtpInvoiceDeliveryTransport>
    {
        public UnsafeScope()
        {
            InstanceUnderTest = new(AppSettings.Object);
            ConfigureAppSettings(AppSettings);
        }

        public Mock<IAppSettings> AppSettings { get; } = new();

        private static void ConfigureAppSettings(Mock<IAppSettings> mock)
        {
            var smtpConfig = new SmtpConfiguration
            {
                Hostname = "example.org",
                Port = 2525,
                Username = "example_user",
                Password = "example_pass",
                EnableSsl = true,
                Sender = "sender@example.org",
            };

            mock.Setup(m => m.GetSection<SmtpConfiguration>(It.IsAny<string>())).Returns(smtpConfig);
        }
    }

    private class SmtpInvoiceDeliveryTransportCustomized : SmtpInvoiceDeliveryTransport
    {
        public SmtpInvoiceDeliveryTransportCustomized(IAppSettings appSettings) : base(appSettings)
        {
        }

        public CustomSmtpClientForTesting Client { get; private set; }

        public override ValueTask<ICustomSmtpClient> CreateSmtpClient()
        {
            Client = new();
            return ValueTask.FromResult<ICustomSmtpClient>(Client);
        }

        public class CustomSmtpClientForTesting : ICustomSmtpClient
        {
            public bool HasBeenCalled { get; set; }

            public MailMessage Message { get; set; }

            public Task SendAsync(MailMessage message)
            {
                HasBeenCalled = true;
                Message = message;
                return Task.CompletedTask;
            }
        }
    }
}
