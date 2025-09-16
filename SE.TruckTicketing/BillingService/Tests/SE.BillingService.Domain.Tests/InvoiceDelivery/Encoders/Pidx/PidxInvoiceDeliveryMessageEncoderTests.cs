using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.FieldTicketv1_62;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_62;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.v1_62;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Encoders.Pidx;

[TestClass]
public class PidxInvoiceDeliveryMessageEncoderTests
{
    [TestMethod]
    public async Task PidxInvoiceDeliveryMessageEncoder_EncodeMessage_Simple()
    {
        // arrange
        var scope = new DefaultScope();
        var j = new JObject
        {
            ["InvoiceProperties"] = new JObject
            {
                ["InvoiceNumber"] = "INV000123",
            },
        };

        var context = new InvoiceDeliveryContext
        {
            Request = new()
            {
                MessageType = MessageType.InvoiceRequest.ToString(),
            },
            Medium = j,
            Config = new()
            {
                InvoiceDeliveryConfiguration = new()
                {
                    MessageAdapterSettings = new(),
                    MessageAdapterType = MessageAdapterType.Pidx,
                    MessageAdapterVersion = 1.62m,
                },
            },
        };

        // act
        using var encodeMessage = await scope.InstanceUnderTest.EncodeMessage(context);

        // assert
        var deserializeInvoice = DeserializeInvoice(encodeMessage.Parts[0].DataStream);
        deserializeInvoice.InvoiceProperties.InvoiceNumber.Should().Be("INV000123");
    }

    [TestMethod]
    public async Task PidxInvoiceDeliveryMessageEncoder_EncodeMessage_FieldTickets()
    {
        // arrange
        var scope = new DefaultScope();
        var j = new JObject
        {
            ["FieldTicketProperties"] = new JObject
            {
                ["FieldTicketNumber"] = "FT000123",
            },
        };

        var context = new InvoiceDeliveryContext
        {
            Request = new()
            {
                MessageType = "FieldTicketRequest",
            },
            Medium = j,
            Config = new()
            {
                FieldTicketsDeliveryConfiguration = new()
                {
                    MessageAdapterSettings = new(),
                    MessageAdapterVersion = 1.62m,
                },
            },
        };

        // act
        using var encodeMessage = await scope.InstanceUnderTest.EncodeMessage(context);

        // assert
        var deserializeInvoice = DeserializeFieldTickets(encodeMessage.Parts[0].DataStream);
        deserializeInvoice.FieldTicketProperties.FieldTicketNumber.Should().Be("FT000123");
    }

    private static Invoice DeserializeInvoice(Stream dataStream)
    {
        var xmlSerializer = new XmlSerializer(typeof(Invoice));
        var pidxInvoice = (Invoice)xmlSerializer.Deserialize(dataStream);
        return pidxInvoice;
    }

    private static FieldTicket DeserializeFieldTickets(Stream dataStream)
    {
        var xmlSerializer = new XmlSerializer(typeof(FieldTicket));
        var pidxInvoice = (FieldTicket)xmlSerializer.Deserialize(dataStream);
        return pidxInvoice;
    }

    private class DefaultScope : TestScope<PidxInvoiceDeliveryMessageEncoder>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(Storage.Object, FileCompressorResolver.Object, new[] { new Pidx162Adapter() }, Log.Object, AppSettings.Object);
        }

        public Mock<IInvoiceAttachmentsBlobStorage> Storage { get; } = new();

        public Mock<IFileCompressorResolver> FileCompressorResolver { get; } = new();

        public Mock<ILog> Log { get; } = new();

        public Mock<IAppSettings> AppSettings { get; } = new();
    }
}
