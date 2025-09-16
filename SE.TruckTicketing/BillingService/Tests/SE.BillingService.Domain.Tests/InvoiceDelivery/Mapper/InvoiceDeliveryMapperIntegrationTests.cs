using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Csv;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.FieldTicketv1_00;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_00;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.v1_00;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.v1_62;
using SE.BillingService.Domain.InvoiceDelivery.Mapper;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Compression;

using Trident.Contracts.Configuration;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Mapper;

[TestClass]
public class InvoiceDeliveryMapperIntegrationTests
{
    [DataTestMethod]
    [DataRow("ut1-ieg-request.json", "ut1-ieg-mappings.json", "ut1-ieg-pidx.xml", "7b491580-150c-4ef4-a4b0-4e180122d1c3")]
    [DataRow("ut2-ieg-request.json", "ut2-ieg-mappings.json", "ut2-ieg-output.csv", "52c6af30-3f01-4a7c-bfef-90cbdd497730")]
    [DataRow("ut3-ieg-request.json", "ut3-ieg-mappings.json", "ut3-ieg-pidx.xml", "7b491580-150c-4ef4-a4b0-4e180122d1c3")]
    [DataRow("ut4-ieg-request.json", "ut4-ieg-mappings.json", "ut4-ieg-pidx.xml", "3f6a4312-9139-4074-93d2-7b26262fee0c")]
    [DataRow("ut5-ieg-request.json", "ut5-ieg-mappings.json", "ut5-ieg-pidx.xml", "7b491580-150c-4ef4-a4b0-4e180122d1c3")]
    [DataRow("ut6-ieg-request.json", "ut6-ieg-mappings.json", "ut6-ieg-pidx.xml", "b5fa30e0-ab2a-4e0c-99ac-60a05bd892a2")]
    public async Task InvoiceDeliveryMapper_Map(string request, string ieg, string outcome, string configId)
    {
        // arrange
        var scope = new IntegrationScope(request, ieg, outcome);
        var context = scope.CreateInvoiceDeliveryContext(configId);

        // act
        await scope.InstanceUnderTest.Map(context);
        var output = context.DeliveryConfig.MessageAdapterType switch
                     {
                         MessageAdapterType.Pidx => ConvertToPidx100(context),
                         MessageAdapterType.Csv => await ConvertToCsv(context),
                         _ => throw new ArgumentOutOfRangeException(),
                     };

        // assert
        output.Trim().Should().Be(scope.Output.Trim());
    }

    private string ConvertToPidx100(InvoiceDeliveryContext context)
    {
        // pick the pidx adapter
        IPidxAdapter pidxAdapter = context.DeliveryConfig.MessageAdapterVersion switch
                                   {
                                       1.00m => new Pidx100Adapter(),
                                       1.62m => new Pidx162Adapter(),
                                       _ => throw new ArgumentOutOfRangeException(),
                                   };

        // pick the target type
        var targetType = (context.Request.GetMessageType(), context.DeliveryConfig.MessageAdapterVersion) switch
                         {
                             (MessageType.InvoiceRequest, 1.00m) => typeof(Invoice),
                             (MessageType.InvoiceRequest, 1.62m) => typeof(Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_62.Invoice),
                             (MessageType.FieldTicketRequest, 1.00m) => typeof(FieldTicket),
                             (MessageType.FieldTicketRequest, 1.62m) => typeof(Domain.InvoiceDelivery.Encoders.Pidx.FieldTicketv1_62.FieldTicket),
                             _ => throw new ArgumentOutOfRangeException(),
                         };

        // convert to the object
        var pidx = pidxAdapter.ConvertToPidx(context);

        // prep XML streams
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        using var xmlWriter = XmlWriter.Create(writer, new()
        {
            Indent = true,
            IndentChars = "    ",
        });

        // prep XML serializer
        var namespaces = pidxAdapter.GetXmlSerializerNamespaces(context.Request.GetMessageType() ?? default);
        var serializer = new XmlSerializer(targetType);

        // serialize
        serializer.Serialize(xmlWriter, pidx, namespaces);

        // reset the stream
        stream.Position = 0;

        // read all as string
        using var sr = new StreamReader(stream);
        var xml = sr.ReadToEnd();

        return xml;
    }

    private async Task<string> ConvertToCsv(InvoiceDeliveryContext context)
    {
        // create a basic encoder
        var storage = new Mock<IInvoiceAttachmentsBlobStorage>();
        var fileCompressorResolver = new Mock<IFileCompressorResolver>();
        var log = new Mock<ILog>();
        var appSettings = new Mock<IAppSettings>();
        var encoder = new CsvInvoiceDeliveryMessageEncoder(storage.Object, fileCompressorResolver.Object, log.Object, appSettings.Object);

        // encode into the CSV
        var encodedInvoice = await encoder.EncodeMessage(context);

        // find the invoice part
        var part = encodedInvoice.Parts.First(p => p.IsAttachment == false);

        // read as string (CSV)
        using var streamReader = new StreamReader(part.DataStream);
        var data = await streamReader.ReadToEndAsync();

        return data;
    }

    private class IntegrationScope : TestScope<InvoiceDeliveryMapper>
    {
        public IntegrationScope(string request, string ieg, string pidx)
        {
            InstanceUnderTest = new(new JsonFiddler(), new ExpressionManager(new DotNetCompiler()));

            RequestJson = JToken.Parse(typeof(InvoiceDeliveryMapperIntegrationTests).Assembly.GetResourceAsString(request, "TestData", "ieg_integration"));
            IegJson = JToken.Parse(typeof(InvoiceDeliveryMapperIntegrationTests).Assembly.GetResourceAsString(ieg, "TestData", "ieg_integration"));
            Output = typeof(InvoiceDeliveryMapperIntegrationTests).Assembly.GetResourceAsString(pidx, "TestData", "ieg_integration");
        }

        public JToken RequestJson { get; }

        public JToken IegJson { get; }

        public string Output { get; }

        public InvoiceDeliveryContext CreateInvoiceDeliveryContext(string configIdString)
        {
            // JSON fix
            foreach (var token in IegJson)
            {
                token["id"] = token["Id"];
            }

            // prepare the configuration
            var configId = Guid.Parse(configIdString);
            var allConfigs = IegJson.SelectTokens("$.[?(@.DocumentType=='InvoiceExchange')]")
                                    .Select(t => t.ToObject<InvoiceExchangeEntity>())
                                    .ToList();

            var appliedConfigs = FetchAllConfigTypes(allConfigs, configId);
            var finalConfig = InvoiceExchangeManager.MergeConfigs(appliedConfigs.ToArray());

            // new context
            return new()
            {
                Request = RequestJson.ToObject<DeliveryRequest>(),
                Lookups = new()
                {
                    SourceFields = IegJson.SelectTokens("$.[?(@.DocumentType=='SourceModelField')]")
                                          .Select(t => t.ToObject<SourceModelFieldEntity>())
                                          .ToDictionary(f => f!.Id),
                    DestinationFields = IegJson.SelectTokens("$.[?(@.DocumentType=='DestinationModelField')]")
                                               .Select(t => t.ToObject<DestinationModelFieldEntity>())
                                               .ToDictionary(f => f!.Id),
                    ValueFormats = IegJson.SelectTokens("$.[?(@.DocumentType=='ValueFormat')]")
                                          .Select(t => t.ToObject<ValueFormatEntity>())
                                          .ToDictionary(f => f!.Id),
                },
                Config = finalConfig,
            };

            List<InvoiceExchangeEntity> FetchAllConfigTypes(List<InvoiceExchangeEntity> configs, Guid targetConfigId)
            {
                // fetch the target / global config
                var targetConfig = configs.First(config => config.Id == targetConfigId);

                // selected config info
                var platformCode = targetConfig.PlatformCode;
                var businessStreamId = targetConfig.BusinessStreamId;
                var legalEntityId = targetConfig.LegalEntityId;
                var customerId = targetConfig.BillingAccountId;

                // fetch the global config for the given platform
                var globalConfig = configs.Where(e => e.IsDeleted == false &&
                                                      e.Type == InvoiceExchangeType.Global &&
                                                      e.PlatformCode == platformCode)
                                          .MinBy(c => c.CreatedAt);

                // fetch the business stream level for the defined config
                var businessStreamConfig = configs.Where(e => e.IsDeleted == false &&
                                                              e.Type == InvoiceExchangeType.BusinessStream &&
                                                              e.RootInvoiceExchangeId == globalConfig.Id &&
                                                              e.BusinessStreamId == businessStreamId)
                                                  .MinBy(c => c.CreatedAt);

                // fetch the legal entity level config
                var legalEntityConfig = configs.Where(e => e.IsDeleted == false &&
                                                           e.Type == InvoiceExchangeType.LegalEntity &&
                                                           e.RootInvoiceExchangeId == globalConfig.Id &&
                                                           e.BusinessStreamId == businessStreamId &&
                                                           e.LegalEntityId == legalEntityId)
                                               .MinBy(c => c.CreatedAt);

                // fetch the account config that matches the given platform/root
                var accountConfig = configs.Where(e => e.IsDeleted == false &&
                                                       e.Type == InvoiceExchangeType.Customer &&
                                                       e.RootInvoiceExchangeId == globalConfig.Id &&
                                                       e.BusinessStreamId == businessStreamId &&
                                                       e.LegalEntityId == legalEntityId &&
                                                       e.BillingAccountId == customerId)
                                           .MinBy(c => c.CreatedAt);

                return new()
                {
                    accountConfig,
                    legalEntityConfig,
                    businessStreamConfig,
                    globalConfig,
                };
            }
        }
    }
}
