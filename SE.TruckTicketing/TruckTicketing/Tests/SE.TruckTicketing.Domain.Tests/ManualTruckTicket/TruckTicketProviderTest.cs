using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket;

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
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ProviderBase<Guid, TruckTicketEntity>));
    }

    private class DefaultScope : TestScope<TruckTicketProvider>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(TruckTicketSearchRepositoryMock.Object);
        }

        public Mock<ISearchRepository<TruckTicketEntity>> TruckTicketSearchRepositoryMock { get; } = new();
    }
}
