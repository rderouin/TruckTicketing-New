using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Mail;
using SE.BillingService.Domain.InvoiceDelivery.Shared;
using SE.Shared.Common;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Encoders.Mail;

[TestClass]
public class MailMessageInvoiceDeliveryMessageEncoderTests
{
    [TestMethod]
    public async Task MailMessageInvoiceDeliveryMessageEncoder_EncodeMessage()
    {
        // arrange
        var scope = new DefaultScope();
        var target = JObject.FromObject(new
        {
            From = "from@example.com",
            To = "to1@example.com,to2@example.com",
            Cc = "cc@example.com",
            Bcc = "bcc@example.com",
            ReplyTo = "replyto@example.com",
            Subject = "subject text",
            Body = "body text",
        });

        var settings = new InvoiceExchangeMessageAdapterSettingsEntity
        {
            Id = Guid.NewGuid(),
            AcceptsAttachments = true,
            EmbedAttachments = false,
            MaxAttachmentSizeInMegabytes = 5,
        };

        var context = new InvoiceDeliveryContext
        {
            Request = new()
            {
                Blobs = new()
                {
                    new()
                    {
                        ContainerName = "test-container",
                        BlobPath = "test-path/my-blob",
                        ContentType = "text/plain",
                        Filename = "test.txt",
                    },
                },
            },
            Medium = target,
            Config = new() { InvoiceDeliveryConfiguration = new() { MessageAdapterSettings = settings } },
        };

        var randomBlob = new MemoryStream(Encoding.UTF8.GetBytes("random blob data"));

        scope.Storage.Setup(s => s.Download(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(randomBlob);

        // act
        var encodedInvoice = await scope.InstanceUnderTest.EncodeMessage(context);

        // assert
        encodedInvoice.Parts.Count.Should().Be(1);
        using var reader = new StreamReader(encodedInvoice.Parts[0].DataStream);
        var data = await reader.ReadToEndAsync();
        var surrogate = JsonConvert.DeserializeObject<MailMessageSurrogate>(data)!;
        surrogate.Should().BeEquivalentTo(new MailMessageSurrogate
        {
            To = "to1@example.com,to2@example.com",
            Cc = "cc@example.com",
            Bcc = "bcc@example.com",
            ReplyTo = "replyto@example.com",
            Subject = "subject text",
            Body = "body text",
            Attachments = new()
            {
                new()
                {
                    MediaType = "text/plain",
                    Name = "test.txt",
                },
            },
        }, opt => opt.Excluding(s => s.Attachments[0].Data));
    }

    private class DefaultScope : TestScope<MailMessageInvoiceDeliveryMessageEncoder>
    {
        public DefaultScope()
        {
            AppSettings.Setup(s => s.GetSection<FeatureToggles>(It.IsAny<string>())).Returns(new FeatureToggles());
            InstanceUnderTest = new(Storage.Object, FileCompressorResolver.Object, Log.Object, AppSettings.Object);
        }

        public Mock<IInvoiceAttachmentsBlobStorage> Storage { get; } = new();

        public Mock<IFileCompressorResolver> FileCompressorResolver { get; } = new();

        public Mock<ILog> Log { get; } = new();

        public Mock<IAppSettings> AppSettings { get; } = new();
    }
}
