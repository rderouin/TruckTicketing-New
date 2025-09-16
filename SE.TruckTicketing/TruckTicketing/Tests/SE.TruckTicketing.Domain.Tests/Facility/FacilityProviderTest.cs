using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;

using Trident.Business;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.Facility;

[TestClass]
public class TruckTicketProviderTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityProvider_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ProviderBase<Guid, FacilityEntity>));
    }

    private class DefaultScope : TestScope<FacilityProvider>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(FacilitySearchRepositoryMock.Object);
        }

        //ISearchRepository<FacilityEntity> repository
        public Mock<ISearchRepository<FacilityEntity>> FacilitySearchRepositoryMock { get; } = new();
    }
}
