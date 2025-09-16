using System;

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.TruckTicketing.Client.Configuration;

using Trident.Contracts.Configuration;

//using SE.TruckTicketing.Api.Configuration;

namespace SE.TruckTicketing.Client.Tests;

[TestClass]
public class ConfigurationSettingsTest
{
    private IAppSettings _appSettings;

    [TestInitialize]
    public void TestInitialize()
    {
        var config = new ConfigurationBuilder();
        config.SetBasePath(Environment.CurrentDirectory);
        config.AddJsonFile("appsettings.json", false, true);
        _appSettings = new AppSettings(config);
        config.Build();
    }

    [TestMethod]
    public void AppSettings()
    {
        var key = "TruckTicketing:LocalizationContainer";

        var appSettingsValue = _appSettings.GetKeyOrDefault(key);
        var appSettingsIndexerValue = _appSettings[key];

        Assert.AreEqual(appSettingsIndexerValue, "localization");
    }
}
