using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Domain.Entities.AdditionalServicesConfiguration;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class AdditionalServicesConfigurationFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationFunctions_AdditionalServicesConfigurationSearch_ShouldReturnSuccessResponse_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.AdditionalServicesConfigurationSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<AdditionalServicesConfiguration, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationFunctions_AdditionalServicesConfigurationSearch_ShouldReturnAllFacilities_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.AdditionalServicesConfigurationSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<AdditionalServicesConfiguration, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(scope.TestAdditionalServicesConfigurations.Count(), actualContent.Results.Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationFunctions_AdditionalServicesConfigurationSearch_ShouldReturnNoData_FilterNotMatchingData()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("FacilityName", "NoMatch");
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.AdditionalServicesConfigurationSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<AdditionalServicesConfiguration, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(!actualContent.Results.Any());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task AdditionalServicesConfigurationFunctions_AdditionalServicesConfigurationSearch_ShouldReturnValidData_MultipleFiltersApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("CustomerName", "TestCustomer");

        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.AdditionalServicesConfigurationSearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<AdditionalServicesConfiguration, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(actualContent.Results.Any());
    }

    private class DefaultScope : HttpTestScope<AdditionalServicesConfigurationFunctions>
    {
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new[] { new ApiMapperProfile() });
            TestAdditionalServicesConfigurations = GetTestAdditionalServicesConfigurations();
            ConfigureAdditionalServicesConfigurationMockSearch();
            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    AdditionalServicesConfigurationManagerMock.Object);
        }

        public Mock<ILog> LogMock { get; } = new();

        public IMapperRegistry MapperRegistry { get; }

        public Mock<IManager<Guid, AdditionalServicesConfigurationEntity>> AdditionalServicesConfigurationManagerMock { get; } = new();

        public List<AdditionalServicesConfigurationEntity> TestAdditionalServicesConfigurations { get; }

        public void ConfigureAdditionalServicesConfigurationMockSearch()
        {
            var queryResult = GetTestAdditionalServicesConfigurations();
            AdditionalServicesConfigurationManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria<AdditionalServicesConfigurationEntity>>(), false))
                                                      .ReturnsAsync((SearchCriteria<AdditionalServicesConfigurationEntity> criteria, bool loadChildren) =>
                                                                    {
                                                                        var filterBy = criteria.Filters;
                                                                        foreach (var key in filterBy.Keys)
                                                                        {
                                                                            var parameterExpression = Expression.Parameter(typeof(AdditionalServicesConfigurationEntity));
                                                                            var property = Expression.Property(parameterExpression, key);
                                                                            var constant = Expression.Constant(criteria.Filters[key]);
                                                                            var expression = Expression.Equal(property, constant);
                                                                            var lambda = Expression.Lambda<Func<AdditionalServicesConfigurationEntity, bool>>(expression, parameterExpression);
                                                                            if (queryResult.Count > 0)
                                                                            {
                                                                                queryResult = queryResult.Where(lambda.Compile()).ToList();
                                                                            }
                                                                        }

                                                                        var result = new SearchResults<AdditionalServicesConfigurationEntity, SearchCriteria>
                                                                        {
                                                                            Results = queryResult.ToList(),
                                                                        };

                                                                        return result;
                                                                    });
        }

        private List<AdditionalServicesConfigurationEntity> GetTestAdditionalServicesConfigurations()
        {
            return new()
            {
                new()
                {
                    FacilityName = "TestFacility",
                    CustomerName = "TestCustomer",
                },
            };
        }
    }
}
