using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json.Linq;

using SE.BillingService.Domain.InvoiceDelivery.Validation;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

using Trident.Testing.TestScopes;

namespace SE.BillingService.Domain.Tests.InvoiceDelivery.Validation;

[TestClass]
public class InvoiceDeliveryRequestValidatorTests
{
    [DataTestMethod]
    [DataRow(nameof(DeliveryRequest.Payload), "Payload for the invoice delivery must be provided.")]
    public async Task InvoiceDeliveryRequestValidator_Validate_AllErrors(string prop, string message)
    {
        // arrange
        var scope = new DefaultScope();
        var request = new DeliveryRequest
        {
            EnterpriseId = Guid.NewGuid(),
            Payload = new(),
        };

        request = SetBlank(request, prop);

        // act
        var errors = await scope.InstanceUnderTest.Validate(request);

        // assert
        errors.Should().Contain(message);

        static DeliveryRequest SetBlank(DeliveryRequest request, string property)
        {
            var j = JObject.FromObject(request);
            j[property] = null;
            return j.ToObject<DeliveryRequest>();
        }
    }

    private class DefaultScope : TestScope<InvoiceDeliveryRequestValidator>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new();
        }
    }
}
