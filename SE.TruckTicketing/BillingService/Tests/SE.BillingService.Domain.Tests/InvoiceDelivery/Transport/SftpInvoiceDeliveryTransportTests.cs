using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Renci.SshNet;

using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Transport;

using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Transport;

[TestClass]
public class SftpInvoiceDeliveryTransportTests
{
    [TestMethod]
    public async Task SftpInvoiceDeliveryTransport_Send_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        using var part = new EncodedInvoicePart
        {
            ContentType = "text/csv",
            IsAttachment = false,
            DataStream = new MemoryStream(new byte[] { 0x30 }),
        };

        var instructions = new InvoiceDeliveryTransportInstructions
        {
            DestinationUri = new("sftp://example.org/folder/file.csv"),
            ClientId = "username1",
            ClientSecret = "password1",
        };

        // act
        await scope.InstanceUnderTest.Send(part, instructions);

        // assert
        var instance = scope.ActualInstance;
        instance.CapturedConnectionInfo.Host.Should().Be("example.org");
        instance.CapturedConnectionInfo.Port.Should().Be(22);
        instance.CapturedConnectionInfo.Username.Should().Be("username1");
        instance.CapturedFullPath.Should().Be("/folder/file.csv");
    }

    private class DefaultScope : TestScope<SftpInvoiceDeliveryTransport>
    {
        public DefaultScope()
        {
            ActualInstance = new(Log.Object);
            InstanceUnderTest = ActualInstance;
        }

        public SftpInvoiceDeliveryTransportSafe ActualInstance { get; }

        public Mock<ILog> Log { get; } = new();

        public class SftpInvoiceDeliveryTransportSafe : SftpInvoiceDeliveryTransport
        {
            public SftpInvoiceDeliveryTransportSafe(ILog logger) : base(logger)
            {
            }

            public ConnectionInfo CapturedConnectionInfo { get; set; }

            public Stream CapturedStream { get; set; }

            public string CapturedFullPath { get; set; }

            protected override Task UploadFile(SftpClient client, Stream stream, string fullPath)
            {
                CapturedConnectionInfo = client.ConnectionInfo;
                CapturedStream = stream;
                CapturedFullPath = fullPath;

                // bypass the actual connect & send
                return Task.CompletedTask;
            }
        }
    }
}
