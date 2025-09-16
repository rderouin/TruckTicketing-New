using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Facility;

[TestClass]
public class TruckTicketRepositoryTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityRepository_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(CosmosEFCoreSearchRepositoryBase<FacilityEntity>));
    }

    private class DefaultScope : TestScope<FacilitiesRepository>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(SearchResultBuilderMock.Object,
                                    SearchQueryBuilderMock.Object,
                                    AbstractContextFactoryMock.Object,
                                    QueryableHelper.Object);
        }

        public Mock<ISearchResultsBuilder> SearchResultBuilderMock { get; } = new();

        public Mock<ISearchQueryBuilder> SearchQueryBuilderMock { get; } = new();

        public Mock<IAbstractContextFactory> AbstractContextFactoryMock { get; } = new();

        public Mock<IQueryableHelper> QueryableHelper { get; } = new();
    }
}
