using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Api.Functions.Facilities;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class FacilityFunctionsTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityFunctions_FacilitySearch_ShouldReturnSuccessResponse_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.FacilitySearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<Facility, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(HttpStatusCode.OK, httpResponseData.StatusCode);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityFunctions_FacilitySearch_ShouldReturnAllFacilities_NoFilterApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);
        // act
        var httpResponseData = await scope.InstanceUnderTest.FacilitySearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<Facility, SearchCriteriaModel>>();
        // assert
        Assert.AreEqual(scope.TestFacilities.Count(), actualContent.Results.Count());
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityFunctions_FacilitySearch_ShouldReturnNoData_FilterNotMatchingData()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("LegalEntity", "USA");
        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.FacilitySearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<Facility, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(actualContent.Results.Count() == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task FacilityFunctions_FacilitySearch_ShouldReturnValidData_MultipleFiltersApplied()
    {
        // arrange
        var scope = new DefaultScope();
        var searchCriteria = new SearchCriteriaModel();
        searchCriteria.AddFilter("LegalEntity", "Canada");
        searchCriteria.AddFilter("SiteId", "TAFA");

        var json = JsonConvert.SerializeObject(searchCriteria);
        var mockRequest = scope.CreateHttpRequest(null, json);

        // act
        var httpResponseData = await scope.InstanceUnderTest.FacilitySearch(mockRequest);
        var actualContent = await httpResponseData.ReadJsonToObject<SearchResultsModel<Facility, SearchCriteriaModel>>();
        // assert
        Assert.IsTrue(actualContent.Results.Count() > 0);
    }

    private class DefaultScope : HttpTestScope<FacilityFunctions>
    {
        //public IComplexFilter<FacilityEntity> facilityFilter;
        public DefaultScope()
        {
            MapperRegistry = new ServiceMapperRegistry(new[] { new ApiMapperProfile() });
            TestFacilities = GetTestFacilities();
            ConfigureFacilityMockSearch();
            InstanceUnderTest = new(LogMock.Object,
                                    MapperRegistry,
                                    FacilityManagerMock.Object);
        }

        public Mock<ILog> LogMock { get; } = new();

        public IMapperRegistry MapperRegistry { get; }

        public Mock<IManager<Guid, FacilityEntity>> FacilityManagerMock { get; } = new();

        public List<FacilityEntity> TestFacilities { get; }

        public Mock<IComplexFilter<FacilityEntity>> facilityFilter { get; } = new();

        public void ConfigureFacilityMockSearch()
        {
            var queryResult = GetTestFacilities();
            FacilityManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria<FacilityEntity>>(), false))
                               .ReturnsAsync((SearchCriteria<FacilityEntity> criteria, bool loadChildren) =>
                                             {
                                                 var filterBy = criteria.Filters;
                                                 foreach (var key in filterBy.Keys)
                                                 {
                                                     var parameterExpression = Expression.Parameter(typeof(FacilityEntity));
                                                     var property = Expression.Property(parameterExpression, key);
                                                     var constant = Expression.Constant(criteria.Filters[key]);
                                                     var expression = Expression.Equal(property, constant);
                                                     var lambda = Expression.Lambda<Func<FacilityEntity, bool>>(expression, parameterExpression);
                                                     if (queryResult.Count > 0)
                                                     {
                                                         queryResult = queryResult.Where(lambda.Compile()).ToList();
                                                     }
                                                 }

                                                 var result = new SearchResults<FacilityEntity, SearchCriteria>
                                                 {
                                                     Results = queryResult.ToList(),
                                                 };

                                                 return result;
                                             });
        }

        private List<FacilityEntity> GetTestFacilities()
        {
            return new()
            {
                new()
                {
                    SiteId = "TAFA",
                    Name = "Facility1",
                    Type = FacilityType.Lf,
                    LegalEntity = "Canada",
                },
            };
        }
    }
}
