using System;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Tests.Entities.PricingRules;

[TestClass]
public class PricingManagerTests
{
    [TestMethod]
    public void RealWorldActiveFromTest()
    {
        //setup
        //2023-08-01T00:00:00Z
        const string realWorldActiveFromDate = "2023-08-01T23:59:59Z";
        var activeFromDate  = DateTime.Parse(realWorldActiveFromDate);

        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 08, 1, 1, 2, 3, offset: TimeSpan.Zero) };
        PricingRuleEntity rule = new PricingRuleEntity { ActiveFrom = activeFromDate };

        //execute
        var result = rule.ActiveFrom.Date <= request.Date.Date;
        
        //assert
        Assert.IsTrue(result, "Should return true");
    }

    [TestMethod]
    public void IsActiveToGreaterThanOrEqualTo_SameDateDifferentTimes_ReturnTrue()
    {
        //setup
        var sepSixOneSecondAfterMidnight = new DateTimeOffset(2023, 9, 6, 0, 0, 1, offset: TimeSpan.Zero);
        var sepSixOneSecondToMidnight = new DateTimeOffset(2023, 9, 6, 23, 59, 59, TimeSpan.Zero);

        ComputePricingRequest request = new ComputePricingRequest { Date = sepSixOneSecondAfterMidnight };
        PricingRuleEntity isActiveToDate = new PricingRuleEntity { ActiveTo = sepSixOneSecondToMidnight };

        //execute
        var result = isActiveToDate.ActiveTo == null || isActiveToDate.ActiveTo.Value.Date >= request.Date.Date;

        //assert
        Assert.IsTrue(result, "Should return true");
    }

    [TestMethod]
    public void IsActiveToGreaterThanOrEqualTo_LessThan_ReturnFalse()
    {
        //setup
        TimeSpan theTimeSpan = new TimeSpan(0);
        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 1, 2, 0, 0, 0, offset: theTimeSpan) };
        PricingRuleEntity isActiveToDate = new PricingRuleEntity { ActiveTo = new(2023, 1, 1, 23, 59, 59, theTimeSpan) };

        //execute
        var result = isActiveToDate.ActiveTo == null || isActiveToDate.ActiveTo.Value.Date >= request.Date.Date;

        //assert
        Assert.IsFalse(result, "Should return false");
    }

    [TestMethod]
    public void IsActiveToGreaterThanOrEqualTo_EqualTo_ReturnTrue()
    {
        //setup
        TimeSpan theTimeSpan = new TimeSpan(0);
        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 1, 1, 1, 1, 1, offset: theTimeSpan) };
        PricingRuleEntity isActiveToDate = new PricingRuleEntity { ActiveTo = new(2023, 1, 1, 1, 2, 3, theTimeSpan) };

        //execute
        var result = isActiveToDate.ActiveTo == null || isActiveToDate.ActiveTo.Value.Date >= request.Date.Date;

        //assert
        Assert.IsTrue(result, "Should return true");
    }

    [TestMethod]
    public void IsActiveFromLessThanOrEqualTo_LessThan_ReturnTrue()
    {
        //setup
        TimeSpan theTimeSpan = new TimeSpan(0);
        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 1, 2, 0, 0, 0, offset: theTimeSpan) };
        PricingRuleEntity isActiveFromDate = new PricingRuleEntity { ActiveFrom = new(2023, 1, 1, 23, 59, 59, theTimeSpan) };

        //execute
        var result = isActiveFromDate.ActiveFrom.Date <= request.Date.Date;

        //assert
        Assert.IsTrue(result, "Should return true");
    }

    [TestMethod]
    public void IsActiveFromLessThanOrEqualTo_EqualTo_ReturnTrue()
    {
        //setup
        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 1, 1, 1, 2, 3, offset: TimeSpan.Zero) };
        PricingRuleEntity rule = new PricingRuleEntity { ActiveFrom = new(2023, 1, 1, 23, 59, 59, TimeSpan.Zero) };

        //execute
        var result = rule.ActiveFrom.Date <= request.Date.Date;

        //assert
        Assert.IsTrue(result, "Should return true");
    }

    [TestMethod]
    public void IsActiveFromLessThanOrEqualTo_EqualTo_ReturnFalse()
    {
        //setup
        TimeSpan theTimeSpan = new TimeSpan(0);
        ComputePricingRequest request = new ComputePricingRequest { Date = new(2023, 1, 1, 23, 59, 59, offset: theTimeSpan) };
        PricingRuleEntity rule = new PricingRuleEntity { ActiveFrom = new(2023, 1, 2, 0, 0, 0, theTimeSpan) };

        //execute
        var result = rule.ActiveFrom.Date <= request.Date.Date;

        //assert
        Assert.IsFalse(result, "Should return false");
    }
    
    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_FacilityBaseRate()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.SalesQuoteType = SalesQuoteType.FacilityBaseRate;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = "C0412",
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_HistoricalRate()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = new(new(2023, 1, 1));
        pricingRule.ProductNumber = "706060";
        pricingRule.SalesQuoteType = SalesQuoteType.FacilityBaseRate;
        pricingRules.Add(pricingRule);

        var historicalRule = pricingRule.Clone();
        historicalRule.Price = 3.50;
        historicalRule.Id = Guid.NewGuid();
        historicalRule.ActiveFrom = DateTimeOffset.MinValue;
        historicalRule.ActiveTo = pricingRule.ActiveFrom.Subtract(TimeSpan.FromDays(1));
        pricingRules.Add(historicalRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = "C0412",
            Date = new(new(2022, 12, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == historicalRule.Id &&
                                                   Math.Abs(pricing.Price - historicalRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_JobQuote()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.SourceLocation = "SourceLocation";
        pricingRule.CustomerNumber = "C4103";
        pricingRule.SalesQuoteType = SalesQuoteType.JobQuote;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = pricingRule.CustomerNumber,
            SourceLocation = pricingRule.SourceLocation,
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_BdAgreement()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.SourceLocation = "SourceLocation";
        pricingRule.CustomerNumber = "C4103";
        pricingRule.SalesQuoteType = SalesQuoteType.BDAgreement;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = pricingRule.CustomerNumber,
            SourceLocation = pricingRule.SourceLocation,
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_CustomerQuote()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.CustomerNumber = "C4103";
        pricingRule.SalesQuoteType = SalesQuoteType.CustomerQuote;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = pricingRule.CustomerNumber,
            SourceLocation = pricingRule.SourceLocation,
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_CommercialTerms()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.CustomerNumber = "C4104";
        pricingRule.SalesQuoteType = SalesQuoteType.CommercialTerms;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = "C4104",
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectCorrectPrice_TieredPricing()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var pricingRule = GenFu.GenFu.New<PricingRuleEntity>();
        pricingRule.SiteId = "DCFST";
        pricingRule.Price = 2.50;
        pricingRule.Id = Guid.NewGuid();
        pricingRule.ActiveFrom = DateTimeOffset.MinValue;
        pricingRule.ProductNumber = "706060";
        pricingRule.PriceGroup = "Gold";
        pricingRule.SalesQuoteType = SalesQuoteType.TieredPricing;
        pricingRules.Add(pricingRule);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = pricingRule.SiteId,
            ProductNumber = new() { pricingRule.ProductNumber },
            CustomerNumber = "C4104",
            TieredPriceGroup = "Gold",
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == pricingRule.Id &&
                                                   pricing.ProductNumber == pricingRule.ProductNumber &&
                                                   Math.Abs(pricing.Price - pricingRule.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectPriceLowerInHierarchy_MultipleMatchingRules()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var baseRate = GenFu.GenFu.New<PricingRuleEntity>();
        baseRate.SiteId = "DCFST";
        baseRate.Price = 2.50;
        baseRate.Id = Guid.NewGuid();
        baseRate.ActiveFrom = DateTimeOffset.MinValue;
        baseRate.ProductNumber = "706060";
        baseRate.SalesQuoteType = SalesQuoteType.FacilityBaseRate;
        pricingRules.Add(baseRate);

        var customerQuote = baseRate.Clone();
        customerQuote.Id = Guid.NewGuid();
        customerQuote.SalesQuoteType = SalesQuoteType.CustomerQuote;
        customerQuote.Price = 3.50;
        customerQuote.CustomerNumber = "C4104";
        pricingRules.Add(customerQuote);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = baseRate.SiteId,
            ProductNumber = new() { baseRate.ProductNumber },
            CustomerNumber = "C4104",
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == customerQuote.Id &&
                                                   pricing.ProductNumber == customerQuote.ProductNumber &&
                                                   Math.Abs(pricing.Price - customerQuote.Price) < 0.01);
    }

    [TestMethod]
    public async Task Manager_ShouldSelectLatestRule_MultipleExactMatchingRules()
    {
        // arrange
        var pricingRules = GenFu.GenFu.ListOf<PricingRuleEntity>();
        pricingRules.ForEach(rule => rule.ActiveFrom = DateTimeOffset.MinValue);

        var baseRate = GenFu.GenFu.New<PricingRuleEntity>();
        baseRate.SiteId = "DCFST";
        baseRate.Price = 2.50;
        baseRate.Id = Guid.NewGuid();
        baseRate.ActiveFrom = DateTimeOffset.MinValue;
        baseRate.ProductNumber = "706060";
        baseRate.SalesQuoteType = SalesQuoteType.FacilityBaseRate;
        baseRate.UpdatedAt = DateTimeOffset.UtcNow;
        pricingRules.Add(baseRate);

        var latestRate = baseRate.Clone();
        latestRate.Id = Guid.NewGuid();
        latestRate.Price = 3.50;
        latestRate.UpdatedAt = latestRate.UpdatedAt.AddDays(1);
        pricingRules.Add(latestRate);

        var scope = new DefaultScope();
        scope.PricingProviderMock.SetupEntities(pricingRules);

        var request = new ComputePricingRequest
        {
            SiteId = baseRate.SiteId,
            ProductNumber = new() { baseRate.ProductNumber },
            CustomerNumber = "C4104",
            Date = new(new(2023, 1, 1)),
        };

        // act
        var response = await scope.InstanceUnderTest.ComputePrice(request);
        pricingRules.Reverse();
        var reversedResponse = await scope.InstanceUnderTest.ComputePrice(request);

        // assert
        response.Should().NotBeNull();
        response.Should().ContainSingle(pricing => pricing.PricingRuleId == latestRate.Id &&
                                                   pricing.ProductNumber == latestRate.ProductNumber &&
                                                   Math.Abs(pricing.Price - latestRate.Price) < 0.01);

        response.Should().BeEquivalentTo(reversedResponse);
    }

    private class DefaultScope : TestScope<PricingRuleManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object,
                                    PricingProviderMock.Object,
                                    ValidationManagerMock.Object,
                                    WorkFlowManagerMock.Object);
        }

        private Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, PricingRuleEntity>> PricingProviderMock { get; } = new();

        private Mock<IValidationManager<PricingRuleEntity>> ValidationManagerMock { get; } = new();

        private Mock<IWorkflowManager<PricingRuleEntity>> WorkFlowManagerMock { get; } = new();
    }
}
