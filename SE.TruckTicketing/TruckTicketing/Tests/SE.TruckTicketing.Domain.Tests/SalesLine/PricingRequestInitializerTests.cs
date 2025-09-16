using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.PricingRules;
using SE.TruckTicketing.Domain.Entities.SalesLine;

namespace SE.TruckTicketing.Domain.Tests.SalesLine;

[TestClass]
public class PricingRequestInitializerTests
{
    [TestMethod]
    public void ShouldInitializeWithAccountCustomerNumber()
    {
        //setup
        AccountEntity account = new AccountEntity(){CustomerNumber = "Cust #44", PriceGroup = string.Empty, TmaGroup = "SomeTMA"};
        DateTimeOffset? loadDate = DateTimeOffset.UtcNow;//not prepared to assert this until we use a Clock class to wrap times and dates.
        string productNumber = "ProdNum01";
        string siteId = "Site61";
        string sourceLocation = "SourceLocation54";

        //execute
        ComputePricingRequest result = PricingRequestInitializer.Instance.Initialize(account, loadDate, productNumber, siteId, sourceLocation);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Cust #44", result.CustomerGroup, "Customer Groups should be equal");
        Assert.AreEqual("Cust #44", result.CustomerNumber, "Customer Numbers should be equal");
        Assert.IsTrue(result.ProductNumber.Count == 1);
        Assert.AreEqual("ProdNum01", result.ProductNumber[0], "Product Number should be equal");
        Assert.AreEqual("Site61", result.SiteId, "Site Ids should be equal");
        Assert.AreEqual("SourceLocation54", result.SourceLocation, "Source Locations should be equal");
        Assert.AreEqual("SomeTMA", result.TieredPriceGroup, "TieredPriceGroups should be equal");
    }
    
    [TestMethod]
    public void ShouldInitializeWithAccountPriceGroup()
    {
        //setup
        AccountEntity account = new AccountEntity(){CustomerNumber = "Cust #44", PriceGroup = "My Price Group", TmaGroup = "SomeTMA"};
        DateTimeOffset? loadDate = DateTimeOffset.UtcNow;//not prepared to assert this until we use a Clock class to wrap times and dates.
        string productNumber = "ProdNum01";
        string siteId = "Site61";
        string sourceLocation = "SourceLocation54";

        //execute
        ComputePricingRequest result = PricingRequestInitializer.Instance.Initialize(account, loadDate, productNumber, siteId, sourceLocation);

        //assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Cust #44", result.CustomerGroup, "Customer Groups should be equal");
        Assert.AreEqual("My Price Group", result.CustomerNumber, "Customer Numbers should be equal");
        Assert.IsTrue(result.ProductNumber.Count == 1);
        Assert.AreEqual("ProdNum01", result.ProductNumber[0], "Product Number should be equal");
        Assert.AreEqual("Site61", result.SiteId, "Site Ids should be equal");
        Assert.AreEqual("SourceLocation54", result.SourceLocation, "Source Locations should be equal");
        Assert.AreEqual("SomeTMA", result.TieredPriceGroup, "TieredPriceGroups should be equal");
    }
}
