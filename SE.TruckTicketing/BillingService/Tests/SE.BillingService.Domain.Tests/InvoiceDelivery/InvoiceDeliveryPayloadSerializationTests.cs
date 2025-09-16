using System;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Models;
using SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery;

[TestClass]
public class InvoiceDeliveryPayloadSerializationTests
{
    [DataTestMethod]
    [DataRow(typeof(EntityEnvelopeModel<InvoiceModel>))]
    [DataRow(typeof(EntityEnvelopeModel<FieldTicketModel>))]
    public void InvoiceDelivery_Serialize(Type targetType)
    {
        // arrange
        var json = typeof(InvoiceDeliveryPayloadSerializationTests).Assembly.GetResourceAsString("invoice-delivery-payload.json", "TestData").Trim();

        // act
        var target = JsonConvert.DeserializeObject(json, targetType);
        var output = JsonConvert.SerializeObject(target, Formatting.Indented).Trim();

        // assert
        output.Should().Be(json);
    }
}
