using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Sequences;

using Trident.Data.Contracts;
using Trident.EFCore;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Sequences;

[TestClass]
public class SequenceNumberGeneratorRepositoryTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void SequenceNumberGeneratorRepository_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(CosmosEFCoreSearchRepositoryBase<SequenceEntity>));
    }

    private class DefaultScope : TestScope<SequenceRepository>
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
