using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Domain;
using SE.TruckTicketing.Api.Functions.SourceLocations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Domain.Entities.SourceLocation;

using Trident.Logging;
using Trident.Mapper;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class SourceLocationFunctionsTests
{
    [TestMethod]
    public async Task SourceLocationFunctions_SourceLocation_MarkDelete_ShouldRun()
    {
        //arrange
        var scope = new DefaultScope();
        var sourceLocation = GenFu.GenFu.New<SourceLocation>();
        var json = JsonConvert.SerializeObject(sourceLocation);
        var mockRequest = scope.CreateHttpRequest(null, json);
        scope.SourceLocationManagerMock.Setup(x => x.MarkSourceLocationDelete(It.IsAny<Guid>())).ReturnsAsync(true);
        //act
        var httpResponseData = await scope.InstanceUnderTest.MarkSourceLocationDeleted(mockRequest, sourceLocation.Id);

        //assert
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    public async Task SourceLocationFunctions_SourceLocation_MarkDeleteSuccessful_BooleanResult_ShouldRun()
    {
        //arrange
        var scope = new DefaultScope();
        var sourceLocation = GenFu.GenFu.New<SourceLocation>();
        var json = JsonConvert.SerializeObject(sourceLocation);
        var mockRequest = scope.CreateHttpRequest(null, json);
        scope.SourceLocationManagerMock.Setup(x => x.MarkSourceLocationDelete(It.IsAny<Guid>())).ReturnsAsync(true);
        //act
        var httpResponseData = await scope.InstanceUnderTest.MarkSourceLocationDeleted(mockRequest, sourceLocation.Id);

        //assert
        var isDelete = await httpResponseData.ReadJsonToObject<bool>();
        Assert.IsTrue(isDelete);
    }

    [TestMethod]
    public async Task SourceLocationFunctions_SourceLocation_MarkDeleteUnSuccessful_BooleanResult_ShouldRun()
    {
        //arrange
        var scope = new DefaultScope();
        var sourceLocation = GenFu.GenFu.New<SourceLocation>();
        var json = JsonConvert.SerializeObject(sourceLocation);
        var mockRequest = scope.CreateHttpRequest(null, json);
        scope.SourceLocationManagerMock.Setup(x => x.MarkSourceLocationDelete(It.IsAny<Guid>())).ReturnsAsync(false);
        //act
        var httpResponseData = await scope.InstanceUnderTest.MarkSourceLocationDeleted(mockRequest, sourceLocation.Id);

        //assert
        var isDelete = await httpResponseData.ReadJsonToObject<bool>();
        Assert.IsFalse(isDelete);
    }

    private class DefaultScope : HttpTestScope<SourceLocationFunctions>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new[] { new ApiMapperProfile() });
            InstanceUnderTest = new(LogMock.Object, MapperRegistry, SourceLocationManagerMock.Object);
        }

        public IMapperRegistry MapperRegistry { get; }

        public Mock<ILog> LogMock { get; } = new();

        public Mock<ISourceLocationManager> SourceLocationManagerMock { get; } = new();
    }
}
