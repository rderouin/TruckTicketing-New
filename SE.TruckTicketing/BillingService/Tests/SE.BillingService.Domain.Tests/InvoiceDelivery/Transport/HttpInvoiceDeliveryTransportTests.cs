using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Transport;

using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Transport;

[TestClass]
public class HttpInvoiceDeliveryTransportTests
{
    [TestMethod]
    public async Task HttpInvoiceDeliveryTransport_Send_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        using var stream = new MemoryStream(new byte[] { 0x30 });
        var context = new InvoiceDeliveryContext
        {
            Config = new()
            {
                Id = Guid.NewGuid(),
            },
            EncodedInvoice = new()
            {
                Parts = new()
                {
                    new()
                    {
                        DataStream = stream,
                        ContentType = "application/json",
                    },
                },
            },
        };

        var instructions = new InvoiceDeliveryTransportInstructions
        {
            DestinationUri = new("http://localhost"),
            ClientId = "username",
            ClientSecret = "password",
            HttpVerb = HttpVerb.Post,
            HttpHeaders = new()
            {
                ["TestHeader"] = "TestValue",
                ["SecretHeader"] = "SecretValue",
            },
        };

        // act
        await scope.InstanceUnderTest.Send(context.EncodedInvoice.Parts.First(), instructions);

        // assert
        scope.InstanceUnderTest.PlaceholderHttpClient.Handler.IsCalled.Should().BeTrue();
        scope.InstanceUnderTest.PlaceholderHttpClient.Handler.LastRequestMessage.Headers.NonValidated["TestHeader"].First().Should().Be("TestValue");
        scope.InstanceUnderTest.PlaceholderHttpClient.Handler.LastRequestMessage.Headers.NonValidated["SecretHeader"].First().Should().Be("SecretValue");
        scope.InstanceUnderTest.PlaceholderHttpClient.Handler.LastRequestMessage.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
        var testStream = new MemoryStream();
        await scope.InstanceUnderTest.PlaceholderHttpClient.Handler.LastRequestMessage.Content.CopyToAsync(testStream);
        var data = testStream.ToArray();
        data.Length.Should().Be(1);
        data[0].Should().Be(0x30);
    }

    [TestMethod]
    public void HttpInvoiceDeliveryTransport_CreateHttpClient_FactoryOnly()
    {
        // arrange
        var scope = new UnsafeScope();
        var cert = new X509Certificate2(typeof(HttpInvoiceDeliveryTransportTests).Assembly.GetResource("test-cert.pfx", "TestData"));

        // act
        var client = scope.InstanceUnderTest.CreateHttpClient(cert);

        // assert
        client.Should().NotBeNull();
        var fieldInfo = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        var handler = fieldInfo?.GetValue(client) as HttpClientHandler;
        ((X509Certificate2)handler.ClientCertificates[0]).Thumbprint.Should().Be(new X509Certificate2(cert).Thumbprint);
    }

    private class DefaultScope : TestScope<HttpInvoiceDeliveryTransportSafe>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Log.Object);
        }

        public Mock<ILog> Log { get; } = new();
    }

    private class UnsafeScope : TestScope<HttpInvoiceDeliveryTransport>
    {
        public UnsafeScope()
        {
            InstanceUnderTest = new(Log.Object);
        }

        public Mock<ILog> Log { get; } = new();
    }

    private class HttpInvoiceDeliveryTransportSafe : HttpInvoiceDeliveryTransport
    {
        public HttpInvoiceDeliveryTransportSafe(ILog logger) : base(logger)
        {
        }

        public PlaceholderHttpClient PlaceholderHttpClient { get; } = new();

        public override HttpClient CreateHttpClient(X509Certificate2 cert)
        {
            return PlaceholderHttpClient;
        }
    }
}
