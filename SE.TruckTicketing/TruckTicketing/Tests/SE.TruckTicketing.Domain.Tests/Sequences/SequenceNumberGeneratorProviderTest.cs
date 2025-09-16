using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Sequences;

using Trident.Business;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Sequences;

[TestClass]
public class SequenceNumberGeneratorProviderTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityProvider_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ProviderBase<Guid, SequenceEntity>));
    }

    private class DefaultScope : TestScope<SequenceProvider>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(SequenceSearchRepositoryMock.Object);
        }

        public Mock<ISearchRepository<SequenceEntity>> SequenceSearchRepositoryMock { get; } = new();
    }
}
