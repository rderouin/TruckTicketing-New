using AutoMapper;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SE.TokenService.Api.Configuration;

namespace SE.TokenService.Api.Tests.Configuration;

[TestClass]
public class ApiMapperProfileTests
{
    [TestMethod]
    public void ApiMapperProfileConfiguration_ShouldBeValid()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<ApiMapperProfile>());
        configuration.AssertConfigurationIsValid();
    }
}
