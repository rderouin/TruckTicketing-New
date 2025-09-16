using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.BillingService.Domain.InvoiceDelivery.Transport;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Azure.KeyVault;

using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Transport;

[TestClass]
public class InvoiceDeliveryTransportStrategyTests
{
    [TestMethod]
    public async Task InvoiceDeliveryTransportStrategy_Send_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid().ToString();
        var certData = new X509Certificate2(typeof(HttpInvoiceDeliveryTransportTests).Assembly.GetResource("test-cert.pfx", "TestData"));
        scope.KeyVault.Setup(kv => kv.GetSecret(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(id);
        scope.KeyVault.Setup(kv => kv.GetCertificate(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(certData);
        using var context = new InvoiceDeliveryContext
        {
            Config = new()
            {
                Id = Guid.NewGuid(),
                InvoiceDeliveryConfiguration = new()
                {
                    MessageAdapterSettings = new(),
                    TransportSettings = new()
                    {
                        TransportType = InvoiceDeliveryTransportType.Http,
                        DestinationEndpointUri = "http://localhost",
                        HttpHeaders = new()
                        {
                            ["TestHeader"] = "{{secret:MySecret}}",
                        },
                        Certificate = "{{certificate:MyCert}}",
                    },
                },
            },
            Request = new(),
            EncodedInvoice = new()
            {
                Parts = new()
                {
                    new()
                    {
                        DataStream = new MemoryStream(new byte[] { 0x30 }),
                        ContentType = "application/json",
                    },
                },
            },
        };

        // act
        await scope.InstanceUnderTest.Send(context);

        // assert
        scope.TransportMock.Verify(t => t.Send(It.IsAny<EncodedInvoicePart>(),
                                               It.IsAny<InvoiceDeliveryTransportInstructions>()),
                                   Times.Once);

        scope.KeyVault.Verify(kv => kv.GetSecret(It.Is<Uri>(u => u == scope.KeyVaultUri),
                                                 It.Is<string>(s => s == "InvoiceDelivery-MySecret"),
                                                 It.IsAny<string>(),
                                                 It.IsAny<CancellationToken>()),
                              Times.Once);

        scope.KeyVault.Verify(kv => kv.GetCertificate(It.Is<Uri>(u => u == scope.KeyVaultUri),
                                                      It.Is<string>(s => s == "InvoiceDelivery-MyCert"),
                                                      It.IsAny<string>(), It.IsAny<CancellationToken>()),
                              Times.Once);
    }

    private class DefaultScope : TestScope<InvoiceDeliveryTransportStrategy>
    {
        public DefaultScope()
        {
            TransportMock.SetupGet(t => t.TransportType).Returns(InvoiceDeliveryTransportType.Http);
            Transports = new() { TransportMock.Object };
            InstanceUnderTest = new(Transports, Log.Object, KeyVault.Object, KeyVaultUri, BlobStorage.Object);
        }

        public Uri KeyVaultUri { get; } = new("https://mykeyvault.vault.azure.net/");

        public List<IInvoiceDeliveryTransport> Transports { get; } = new();

        public Mock<IInvoiceDeliveryTransport> TransportMock { get; } = new();

        public Mock<ILog> Log { get; } = new();

        public Mock<IKeyVault> KeyVault { get; } = new();

        public Mock<IInvoiceDeliveryTransportBlobStorage> BlobStorage { get; } = new();
    }
}
