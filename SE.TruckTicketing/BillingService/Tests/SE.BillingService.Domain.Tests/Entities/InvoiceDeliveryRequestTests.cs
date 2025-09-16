using System;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Models.InvoiceDelivery;
using SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;

namespace SE.BillingService.Domain.Tests.Entities;

[TestClass]
public class InvoiceDeliveryRequestTests
{
    [TestMethod]
    public void InvoiceDeliveryRequest_DataContract()
    {
        // arrange
        var json = typeof(InvoiceDeliveryRequestTests).Assembly.GetResourceAsString("invoice-delivery-request.json", "TestData");

        // act
        var request = JsonConvert.DeserializeObject<DeliveryRequest>(json)!;
        var newJson = JsonConvert.SerializeObject(request, Formatting.Indented);

        // assert
        newJson.Trim().Should().Be(json.Trim());
        request.Source.Should().Be("D365FO");
        request.EnterpriseId.Should().Be(new Guid("e01346e5-369a-43cd-a408-318379c69b62"));
        request.Blobs.Should().Contain(b => b.ContainerName == "invoice-delivery");
        request.Blobs.Should().Contain(b => b.BlobPath == "attachments/test-with-images.pdf");
        request.Blobs.Should().Contain(b => b.ContentType == "application/pdf");
        request.Blobs.Should().Contain(b => b.Filename == "test-with-images.pdf");
        request.InvoiceId.Should().Be("BCFST10000042-IP");
        request.CustomerId.Should().Be(new Guid("f37b7c90-c9a9-425c-88c3-06378f16562a"));
        request.Platform.Should().Be("OpenInvoice");
        var invoiceModel = request.Payload.ToObject<InvoiceModel>()!;
        invoiceModel.CurrencyCode.Should().Be("CAD");
        invoiceModel.InvoiceTotal.Should().Be(182.87);
        invoiceModel.TotalLineItems.Should().Be(1);
        invoiceModel.BillToDuns.Should().Be("243826232");
        invoiceModel.RemitToDuns.Should().Be("243483406");
        var lineItem = invoiceModel.LineItems.First();
        lineItem.ItemNumber.Should().Be("701001");
        lineItem.Quantity.Should().Be(15.5);
        lineItem.UnitsOfMeasure.Should().Be("M3");
        lineItem.Rate.Should().Be(10.99);
        lineItem.Tax[0].Province.Should().Be("SK");
        lineItem.Tax[0].Country.Should().Be("CA");
        lineItem.Tax[0].Rate.Should().Be(5.0);
        lineItem.Tax[0].Amount.Should().Be(22.33);
        lineItem.Tax[0].Description.Should().Be("Good and Services Tax");
        lineItem.Tax[0].TaxType.Should().Be("GoodAndServices");
        lineItem.Tax[0].TypeCode.Should().Be("GST");
        lineItem.TaxAmount.Should().Be(22.14);
        lineItem.DiscountAmount.Should().Be(9.62);
        lineItem.DiscountPercent.Should().Be(0.05);
        lineItem.TotalAmount.Should().Be(182.87);
        lineItem.Edi.WellFacility.Should().Be("100-02-14-026-19W3-00");
    }

    [TestMethod]
    public void InvoiceDeliveryRequest_SourceId()
    {
        // arrange
        var eid = Guid.NewGuid();
        var request = new DeliveryRequest
        {
            Source = "D365FO",
            SourceId = eid.ToString(),
            MessageType = MessageType.InvoiceRequest.ToString(),
            Operation = "SendInvoice",
            CorrelationId = Guid.NewGuid().ToString(),
            MessageDate = DateTime.UtcNow,
            Payload = new(),
        };

        // act
        var response = request.CreateResponse();

        // assert
        response.Source.Should().Be("BS");
        response.SourceId.Should().Be($"{eid}");
        response.MessageType.Should().Be(MessageType.InvoiceResponse.ToString());
        response.CorrelationId.Should().Be(request.CorrelationId);
    }
}
