using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Data.Contracts;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceRepositoryTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityServiceRepository_CanBeInstantiated()
    {
        var scope = new DefaultScope();
        scope.InstanceUnderTest.Should().NotBeNull();
    }

    private class DefaultScope : TestScope<FacilityServiceRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ResultsBuilderMock.Object,
                                    QueryBuilderMock.Object,
                                    AbstractContextFactoryMock.Object,
                                    QueryableHelperMock.Object);
        }

        public Mock<ISearchResultsBuilder> ResultsBuilderMock { get; } = new();

        public Mock<ISearchQueryBuilder> QueryBuilderMock { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactoryMock { get; } = new();

        public Mock<IQueryableHelper> QueryableHelperMock { get; } = new();
    }
}
