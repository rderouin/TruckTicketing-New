using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class ServiceTypeFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_GetRoleById_OkResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceType
        {
            CountryCode = CountryCode.CA,
            ServiceTypeId = "Id",
            Class = Class.Class1,
            Name = "test1",
            ReportAsCutType = ReportAsCutTypes.Service,
            Stream = Stream.Pipeline,
        });

        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new ServiceTypeEntity { Id = id };

        //Setup
        scope.ServiceTypeManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeGetById(mockRequest, id);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<ServiceTypeEntity>();

        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeGetById_NotFoundResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var mockRequest = scope.CreateHttpRequest(null, null);
        var id = Guid.NewGuid();
        ServiceTypeEntity role = null;
        scope.ServiceTypeManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(role);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeGetById(mockRequest, id);
        var actual = response;

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, actual.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeCreate_OkResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceType
        {
            CountryCode = CountryCode.US,
            ServiceTypeId = "Id",
            Class = Class.Class1,
            Name = "test1",
            TotalItemName = "Product1",
            ReportAsCutType = ReportAsCutTypes.Water,
            Stream = Stream.Landfill,
        });

        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new ServiceTypeEntity { Id = id };

        //Setup
        scope.ServiceTypeManagerMock.Setup(x => x.Save(It.IsAny<ServiceTypeEntity>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeCreate(mockRequest);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<ServiceTypeEntity>();

        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeById_NotFoundResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceTypeEntity
        {
            CountryCode = CountryCode.US,
            ServiceTypeId = "Id",
            Class = Class.Class1,
            Name = "test1",
            TotalItemName = "Total",
            ReportAsCutType = ReportAsCutTypes.Oil,
            Stream = Stream.Landfill,
        });

        var mockRequest = scope.CreateHttpRequest(null, null);
        ServiceTypeEntity entity = null;

        //Setup
        scope.ServiceTypeManagerMock.Setup(x => x.GetById(It.IsAny<Guid>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeGetById(mockRequest, id);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<ServiceTypeEntity>();

        // assert
        Assert.AreEqual(HttpStatusCode.NotFound, actual.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeUpdate_BadRequestResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceType { Id = Guid.NewGuid() });
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeUpdate(mockRequest, id);
        var actual = response;

        // assert
        Assert.AreEqual(HttpStatusCode.BadRequest, actual.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeUpdate_OkResponse()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceType { Id = id });
        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new ServiceTypeEntity { Id = id };

        scope.ServiceTypeManagerMock.Setup(x => x.Save(It.IsAny<ServiceTypeEntity>(), false)).ReturnsAsync(entity);

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeUpdate(mockRequest, id);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<ServiceTypeEntity>();
        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task ServiceTypeFunctions_ServiceTypeSearch_FiltersByDefault()
    {
        // arrange
        var scope = new DefaultScope();
        var id = Guid.NewGuid();
        var serviceTypeId = Guid.NewGuid();
        var json = JsonConvert.SerializeObject(new ServiceTypeEntity
        {
            Id = id,
            CountryCode = CountryCode.US,
            ServiceTypeId = "Id",
            Class = Class.Class1,
            Name = "test1",
            TotalItemName = "Product1",
            ReportAsCutType = ReportAsCutTypes.AsPerCutsEntered,
            Stream = Stream.Landfill,
        });

        var mockRequest = scope.CreateHttpRequest(null, json);
        var entity = new ServiceTypeEntity { Id = id };

        //Setup
        scope.ServiceTypeManagerMock.Setup(x => x.Save(It.IsAny<ServiceTypeEntity>(), false)).ReturnsAsync(entity);

        scope.ServiceTypeManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria<ServiceTypeEntity>>(), false))
             .ReturnsAsync((SearchCriteria<ServiceTypeEntity> criteria, bool loadChildren) =>
                           {
                               return new();
                           });

        // act
        var response = await scope.InstanceUnderTest.ServiceTypeSearch(mockRequest);
        var actual = response;
        var actualContent = await response.ReadJsonToObject<ServiceTypeEntity>();

        // assert
        Assert.IsNotNull(actual);
        Assert.IsNotNull(actualContent);
    }

    private class DefaultScope : HttpTestScope<ServiceTypeFunctions>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(new Mock<ILog>().Object,
                                    new ServiceMapperRegistry(new[] { new ApiMapperProfile() }),
                                    ServiceTypeManagerMock.Object);
        }

        public Mock<IManager<Guid, ServiceTypeEntity>> ServiceTypeManagerMock { get; } = new();
    }
}
