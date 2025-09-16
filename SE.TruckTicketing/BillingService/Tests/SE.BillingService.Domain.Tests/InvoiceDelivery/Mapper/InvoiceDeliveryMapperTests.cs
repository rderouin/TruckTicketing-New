using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.Entities.InvoiceExchange;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_62;
using SE.BillingService.Domain.InvoiceDelivery.Mapper;

using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Mapper;

[TestClass]
public class InvoiceDeliveryMapperTests
{
    [TestMethod]
    public async Task InvoiceDeliveryMapper_Map_GeneralIntegration()
    {
        // arrange
        var scope = new IntegrationScope();
        var sourceFields = scope.GetSourceFields().ToList();
        var destFields = scope.GetDestinationFields().ToList();
        var formats = scope.GetFormats().ToList();
        var expectedObject = scope.GetExpectedInvoice();
        var context = new InvoiceDeliveryContext
        {
            Request = new()
            {
                Blobs = new()
                {
                    new() { Filename = "invoice.pdf" },
                    new() { Filename = "image.png" },
                },
                Payload = scope.GetSimpleRequest(),
            },
            Lookups = new()
            {
                SourceFields = sourceFields.ToDictionary(f => f.Id),
                DestinationFields = destFields.ToDictionary(f => f.Id),
                ValueFormats = formats.ToDictionary(f => f.Id),
            },
            Config = new()
            {
                Id = Guid.NewGuid(),
                InvoiceDeliveryConfiguration = new()
                {
                    MessageAdapterType = MessageAdapterType.Pidx,
                    Mappings =
                    {
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[0].Id,
                            DestinationModelFieldId = destFields[0].Id,
                            DestinationFormatId = formats[0].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[1].Id,
                            DestinationModelFieldId = destFields[1].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[2].Id,
                            DestinationModelFieldId = destFields[2].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[3].Id,
                            DestinationModelFieldId = destFields[3].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[4].Id,
                            DestinationModelFieldId = destFields[4].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[5].Id,
                            DestinationModelFieldId = destFields[5].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[6].Id,
                            DestinationModelFieldId = destFields[6].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[7].Id,
                            DestinationModelFieldId = destFields[7].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[0].Id,
                            DestinationModelFieldId = destFields[8].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[3].Id,
                            DestinationModelFieldId = destFields[9].Id,
                            DestinationUsesValueExpression = true,
                            DestinationValueExpression = @"(refs,cache) =>
{
    // inputs
    var request = (Newtonsoft.Json.Linq.JObject) refs[""request""];
    var item = (Newtonsoft.Json.Linq.JValue) refs[""item""];
    var value = (string) refs[""value""];

    // logic
    var j = (Newtonsoft.Json.Linq.JValue) request[""Payload""][""INVOICEID""];
    var invoiceId = (long)j.Value;
    var output = $""INV-{invoiceId:D6}-{value}"";

    // result
    return output;
}",
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            DestinationConstantValue = "PartnerIdentifier",
                            DestinationModelFieldId = destFields[10].Id,
                            DestinationFormatId = formats[1].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[0].Id,
                            DestinationModelFieldId = destFields[11].Id,
                            DestinationFormatId = formats[2].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[1].Id,
                            DestinationModelFieldId = destFields[12].Id,
                            DestinationFormatId = formats[3].Id,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[8].Id,
                            DestinationModelFieldId = destFields[13].Id,
                            DestinationPlacementHint = "a1=*",
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[9].Id,
                            DestinationModelFieldId = destFields[14].Id,
                            IsDisabled = true,
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[3].Id,
                            DestinationModelFieldId = destFields[15].Id,
                            DestinationUsesValueExpression = true,
                            DestinationValueExpression = @"(refs,cache) =>
{
    // inputs
    var request = (Newtonsoft.Json.Linq.JObject) refs[""request""];
    var item = (Newtonsoft.Json.Linq.JValue) refs[""item""];
    var value = (string) refs[""value""];

    // logic
    var itemDescription = (string) item.Parent.Parent[""LineItemDescription""];
    var output = $""{value} - {itemDescription}"";

    // result
    return output;
}",
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[3].Id,
                            DestinationModelFieldId = destFields[16].Id,
                            DestinationUsesValueExpression = true,
                            DestinationValueExpression = @"(refs,cache) =>
{
    // inputs
    var request = (Newtonsoft.Json.Linq.JObject) refs[""request""];
    var item = (Newtonsoft.Json.Linq.JValue) refs[""item""];
    var value = (string) refs[""value""];

    // fetch the value from the cache
    int inc = 0;
    if (cache.TryGetValue(""increment"", out var incObj))
    {
        inc = (int)incObj;
    }

    // keep latest value
    inc++;
    cache[""increment""] = inc;
    cache[$""increment-{inc}""] = inc;

    // global increment
    var output = $""CACHED-{inc}"";

    // result
    return output;
}",
                        },
                        new InvoiceExchangeMessageFieldMappingEntity
                        {
                            Id = Guid.NewGuid(),
                            SourceModelFieldId = sourceFields[10].Id,
                            DestinationModelFieldId = destFields[17].Id,
                            DestinationPlacementHint = "a1=*,a2=**",
                        },
                    },
                },
            },
        };

        Console.WriteLine(new string('=', 40) + " Request " + new string('=', 40) + Environment.NewLine + context.Request.Payload);

        // act
        await scope.InstanceUnderTest.Map(context);

        // assert
        Console.WriteLine(Environment.NewLine + new string('=', 40) + " Resulting JObject " + new string('=', 40) + Environment.NewLine + context.Medium + Environment.NewLine);
        var pidx = context.Medium.ToObject<Invoice>();
        Console.WriteLine(Environment.NewLine + new string('=', 40) + " Resulting PIDX POCO " + new string('=', 40) + Environment.NewLine + JObject.FromObject(pidx));
        pidx.Should().BeEquivalentTo(expectedObject);
    }

    private class DefaultScope : TestScope<InvoiceDeliveryMapper>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(JsonFiddler.Object, ExpressionManager.Object);
        }

        public Mock<IJsonFiddler> JsonFiddler { get; } = new();

        public Mock<IExpressionManager> ExpressionManager { get; } = new();
    }

    private class IntegrationScope : TestScope<InvoiceDeliveryMapper>
    {
        public IntegrationScope()
        {
            InstanceUnderTest = new(new JsonFiddler(), new ExpressionManager(new DotNetCompiler()));
        }

        public JObject GetSimpleRequest()
        {
            var json = @"
{
    ""INVOICEID"" : 123,
    ""INVOICEACCOUNT"" : ""000002"",
    ""INVOICEDATE"" : ""2022-06-10T13:30:30Z"",
	""SONumber"" : ""000001"",
	""Comment"" : ""my comment"",
	""Lines"": [
		{
			""ItemNumber"" : ""M3006"",
			""LineItemDescription"" : ""Paint Base 1"",
			""Quantity"" : 1,
			""UnitPrice"" : 10.00,
			""Tax"" : [
		        {
                    ""Amount"" : 11.1,
                },
			],
        },
		{
			""ItemNumber"" : ""M3007"",
			""LineItemDescription"" : ""Paint Base 2"",
			""Quantity"" : 2,
			""UnitPrice"" : 20.00,
			""Tax"" : [
		        {
                    ""Amount"" : 11.1,
                },
		        {
                    ""Amount"" : 22.2,
                },
			],
        },
		{
			""ItemNumber"" : ""M3008"",
			""LineItemDescription"" : ""Paint Base 3"",
			""Quantity"" : 3,
			""UnitPrice"" : 30.00,
			""Tax"" : [
		        {
                    ""Amount"" : 11.1,
                },
		        {
                    ""Amount"" : 22.2,
                },
		        {
                    ""Amount"" : 33.3,
                },
			],
        }
    ]
}
";

            return JObject.Parse(json);
        }

        public Invoice GetExpectedInvoice()
        {
            return new()
            {
                InvoiceProperties = new()
                {
                    InvoiceNumber = "INV000123",
                    InvoiceDate = new(2022, 06, 10, 13, 30, 30),
                    Intrastat = new()
                    {
                        SupplementaryUnits = 1,
                        SupplementaryUnitsSpecified = true,
                        CommodityDescription = "123.00",
                        TransactionNature = "06/10/2022",
                    },
                    RevisionNumber = "123",
                    PartnerInformation = new[]
                    {
                        new PartnerInformation
                        {
                            PartnerIdentifier = new PartnerIdentifier[]
                            {
                                new()
                                {
                                    Value = "000002",
                                    definitionOfOther = "PartnerIdentifier",
                                },
                            },
                        },
                    },
                    Attachment = new[]
                    {
                        new Attachment
                        {
                            FileName = "invoice.pdf",
                        },
                        new Attachment
                        {
                            FileName = "image.png",
                        },
                    },
                },
                InvoiceDetails = new InvoiceLineItem[]
                {
                    new()
                    {
                        LineItemNumber = "M3006",
                        LineItemInformation = new()
                        {
                            LineItemDescription = "Paint Base 1",
                        },
                        InvoiceQuantity = new()
                        {
                            Quantity = 1,
                        },
                        Pricing = new()
                        {
                            UnitPrice = new()
                            {
                                MonetaryAmount = 10.00m,
                            },
                        },
                        Comment = "INV-000123-M3006",
                        PurchaseOrderLineItemNumber = "M3006 - Paint Base 1",
                        FieldTicketLineItemNumber = "CACHED-1",
                        Tax = new[]
                        {
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 11.1m,
                                },
                            },
                        },
                    },
                    new()
                    {
                        LineItemNumber = "M3007",
                        LineItemInformation = new()
                        {
                            LineItemDescription = "Paint Base 2",
                        },
                        InvoiceQuantity = new()
                        {
                            Quantity = 2,
                        },
                        Pricing = new()
                        {
                            UnitPrice = new()
                            {
                                MonetaryAmount = 20.00m,
                            },
                        },
                        Comment = "INV-000123-M3007",
                        PurchaseOrderLineItemNumber = "M3007 - Paint Base 2",
                        FieldTicketLineItemNumber = "CACHED-2",
                        Tax = new[]
                        {
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 11.1m,
                                },
                            },
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 22.2m,
                                },
                            },
                        },
                    },
                    new()
                    {
                        LineItemNumber = "M3008",
                        LineItemInformation = new()
                        {
                            LineItemDescription = "Paint Base 3",
                        },
                        InvoiceQuantity = new()
                        {
                            Quantity = 3,
                        },
                        Pricing = new()
                        {
                            UnitPrice = new()
                            {
                                MonetaryAmount = 30.00m,
                            },
                        },
                        Comment = "INV-000123-M3008",
                        PurchaseOrderLineItemNumber = "M3008 - Paint Base 3",
                        FieldTicketLineItemNumber = "CACHED-3",
                        Tax = new[]
                        {
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 11.1m,
                                },
                            },
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 22.2m,
                                },
                            },
                            new TaxType
                            {
                                TaxAmount = new()
                                {
                                    MonetaryAmount = 33.3m,
                                },
                            },
                        },
                    },
                },
            };
        }

        public IEnumerable<SourceModelFieldEntity> GetSourceFields()
        {
            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.INVOICEID",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.INVOICEDATE",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.INVOICEACCOUNT",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Lines[*].ItemNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Lines[*].LineItemDescription",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Lines[*].Quantity",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Lines[*].UnitPrice",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.SONumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Blobs[*].Filename",
                IsGlobal = true,
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Comment",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.Lines[*].Tax[**].Amount",
            };
        }

        public IEnumerable<DestinationModelFieldEntity> GetDestinationFields()
        {
            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.InvoiceNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.InvoiceDate",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.PartnerInformation[0].PartnerIdentifier[0].Value",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].LineItemNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].LineItemInformation.LineItemDescription",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].InvoiceQuantity.Quantity",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].Pricing.UnitPrice.MonetaryAmount",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.Intrastat.SupplementaryUnits",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.RevisionNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].Comment",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.PartnerInformation[0].PartnerIdentifier[0].definitionOfOther",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.Intrastat.CommodityDescription",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.Intrastat.TransactionNature",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.Attachment[a1].FileName",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceProperties.Comment",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].PurchaseOrderLineItemNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[*].FieldTicketLineItemNumber",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                JsonPath = "$.InvoiceDetails[a1].Tax[a2].TaxAmount.MonetaryAmount",
            };
        }

        public IEnumerable<ValueFormatEntity> GetFormats()
        {
            yield return new()
            {
                Id = Guid.NewGuid(),
                ValueExpression = "INV{0:D6}",
                SourceType = "System.Int32",
            };

            yield return new()
            {
                Id = Guid.Empty,
                SourceType = null,
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                ValueExpression = "{0:N2}",
                SourceType = "System.Double",
            };

            yield return new()
            {
                Id = Guid.NewGuid(),
                ValueExpression = "{0:MM/dd/yyyy}",
                SourceType = "System.DateTime",
            };
        }
    }
}
