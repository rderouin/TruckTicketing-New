using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceProviderTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityServiceProvider_CanBeInstantiated()
    {
        var scope = new DefaultScope();
        scope.InstanceUnderTest.Should().NotBeNull();
    }

    private class DefaultScope : TestScope<FacilityServiceProvider>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(RepositoryMock.Object);
        }

        public Mock<ISearchRepository<FacilityServiceEntity>> RepositoryMock { get; } = new();
    }
}
