using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Domain.Integrations.OpenInvoice;
using SE.BillingService.Domain.Tests.InvoiceDelivery.Transport;
using SE.Shared.Common.Extensions;
using SE.TridentContrib.Extensions.Azure.KeyVault;

using Trident.Contracts.Configuration;
using Trident.Testing.TestScopes;

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable PossibleNullReferenceException

namespace SE.BillingService.Domain.Tests.Integrations.OpenInvoice;

[TestClass]
public class OpenInvoiceServiceTests
{
    [TestMethod]
    public async Task OpenInvoiceServices_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        scope.ConfigureDefaultMocks();

        // act
        var receipts = await scope.InstanceUnderTest.QueryReceiptsAsync(new[] { "JCFST017384N-WT", "JCFST47785-SP", "SADLF050763-LF", "JCFST017327A-WT", "WNSWD012710-WT" });

        // assert
        receipts.Receipts.Should().HaveCount(5);
        receipts.Receipts.Should().Contain(r => r.ReceiptNumber == "JCFST017384N-WT" && r.GetEnumStatus() == OpenInvoiceReceiptStatus.Submitted);
        receipts.Receipts.Should().Contain(r => r.ReceiptNumber == "JCFST47785-SP" && r.GetEnumStatus() == OpenInvoiceReceiptStatus.Approved);
        receipts.Receipts.Should().Contain(r => r.ReceiptNumber == "SADLF050763-LF" && r.GetEnumStatus() == OpenInvoiceReceiptStatus.Disputed);
        receipts.Receipts.Should().Contain(r => r.ReceiptNumber == "JCFST017327A-WT" && r.GetEnumStatus() == OpenInvoiceReceiptStatus.Cancelled);
        receipts.Receipts.Should().Contain(r => r.ReceiptNumber == "WNSWD012710-WT" && r.GetEnumStatus() == OpenInvoiceReceiptStatus.Saved);
    }

    private class DefaultScope : TestScope<OpenInvoiceServiceSafe>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(KeyVault.Object, AppSettings.Object);
        }

        public Mock<IKeyVault> KeyVault { get; } = new();

        public Mock<IAppSettings> AppSettings { get; } = new();

        public void ConfigureDefaultMocks(string certPath = null, string certPass = null)
        {
            // load the test cert
            X509Certificate2 cert;
            if (certPath != null)
            {
                var certData = File.ReadAllBytes(certPath);
                cert = certPass.HasText() ? new(certData, certPass) : new(certData);
            }
            else
            {
                cert = new(typeof(HttpInvoiceDeliveryTransportTests).Assembly.GetResource("test-cert.pfx", "TestData"));
            }

            // key vault
            KeyVault.Setup(s => s.GetCertificate(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(cert);

            // settings
            AppSettings.SetupGet(s => s[It.Is<string>(e => e == "AppSettings:OpenInvoiceHostUri")]).Returns("https://onboard-api.openinvoice.com");
            AppSettings.SetupGet(s => s[It.Is<string>(e => e == "Values:InvoiceDeliveryKeyVaultName")]).Returns("zcac-kv-dev-tt-dyn");
        }
    }

    private class OpenInvoiceServiceSafe : OpenInvoiceService
    {
        private static readonly string ResponseText;

        static OpenInvoiceServiceSafe()
        {
            var resData = typeof(OpenInvoiceServiceTests).Assembly.GetResource("oi-statuses-response.json", "TestData");
            ResponseText = Encoding.UTF8.GetString(resData);
        }

        public OpenInvoiceServiceSafe(IKeyVault keyVault, IAppSettings appSettings) : base(keyVault, appSettings)
        {
        }

        public PlaceholderHttpClient PlaceholderHttpClient { get; } = new(new StringContent(ResponseText));

        protected override HttpClient CreateHttpClient(X509Certificate2 certificate)
        {
            return PlaceholderHttpClient;
        }
    }
}
