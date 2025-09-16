using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CsvHelper;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Csv;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Encoders.Csv;

[TestClass]
public class CsvInvoiceDeliveryMessageEncoderTests
{
    [TestMethod]
    public async Task CsvInvoiceDeliveryMessageEncoder_EncodeMessage_Basic()
    {
        // arrange
        var scope = new DefaultScope();
        var target = JObject.FromObject(new
        {
            Rows = new[]
            {
                new
                {
                    Column1 = "test-value 1",
                    Column2 = 123,
                },
                new
                {
                    Column1 = "test-value 2",
                    Column2 = 456,
                },
            },
        });

        var settings = new InvoiceExchangeMessageAdapterSettingsEntity
        {
            Id = Guid.NewGuid(),
            DestinationApiDefinitionUri = "https://example.org/",
            IncludeHeaderRow = true,
            AcceptsAttachments = false,
            EmbedAttachments = false,
        };

        var context = new InvoiceDeliveryContext
        {
            Request = new(),
            Medium = target,
            Config = new() { InvoiceDeliveryConfiguration = new() { MessageAdapterSettings = settings } },
        };

        // act
        var encodedInvoice = await scope.InstanceUnderTest.EncodeMessage(context);

        // assert
        encodedInvoice.Parts.Count.Should().Be(1);
        using var reader = new StreamReader(encodedInvoice.Parts[0].DataStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<MyCsv>().ToList();
        rows.Count.Should().Be(2);
        rows[1].Column1.Should().Be("test-value 2");
        rows[1].Column2.Should().Be(456);
    }

    private class DefaultScope : TestScope<CsvInvoiceDeliveryMessageEncoder>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Storage.Object, FileCompressorResolver.Object, Log.Object, AppSettings.Object);
        }

        public Mock<IInvoiceAttachmentsBlobStorage> Storage { get; } = new();

        public Mock<IFileCompressorResolver> FileCompressorResolver { get; } = new();

        public Mock<ILog> Log { get; } = new();

        public Mock<IAppSettings> AppSettings { get; } = new();
    }

    private class MyCsv
    {
        public string Column1 { get; set; }

        public int Column2 { get; set; }
    }
}
