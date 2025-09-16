using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;

using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Encoders;

[TestClass]
public class InvoiceDeliveryMessageEncoderSelectorTests
{
    [TestMethod]
    public void InvoiceDeliveryMessageEncoderSelector_Select_Supported()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var encoder = scope.InstanceUnderTest.Select(MessageAdapterType.Pidx);

        // assert
        encoder.SupportedMessageAdapterType.Should().Be(MessageAdapterType.Pidx);
    }

    [TestMethod]
    public void InvoiceDeliveryMessageEncoderSelector_Select_Unsupported()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        try
        {
            scope.InstanceUnderTest.Select(MessageAdapterType.Undefined);
        }
        catch (NotSupportedException e) when (e.Message.StartsWith("Encoder is not supported"))
        {
            // expected outcome
            return;
        }

        // assert
        Assert.Fail("Exception should have been thrown");
    }

    private class DefaultScope : TestScope<InvoiceDeliveryMessageEncoderSelector>
    {
        public DefaultScope()
        {
            // random encoder
            SupportedEncoder = new();
            SupportedEncoder.Setup(e => e.SupportedMessageAdapterType).Returns(MessageAdapterType.Pidx);

            // collection of encoders
            Encoders.Add(SupportedEncoder.Object);

            // main instance
            InstanceUnderTest = new(Encoders);
        }

        public Mock<IInvoiceDeliveryMessageEncoder> SupportedEncoder { get; }

        public List<IInvoiceDeliveryMessageEncoder> Encoders { get; } = new();
    }
}
