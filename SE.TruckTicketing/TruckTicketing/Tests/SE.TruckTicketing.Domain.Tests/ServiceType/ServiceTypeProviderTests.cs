using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.ServiceType;
using SE.TruckTicketing.Domain.Entities.ServiceType;

using Trident.Search;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ServiceType;

[TestClass]
public class ServiceTypeProviderTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void ServiceTypeProvider_CanBeInstantiated()
    {
        var scope = new DefaultScope();
        scope.InstanceUnderTest.Should().NotBeNull();
    }

    private class DefaultScope : TestScope<ServiceTypeProvider>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(RepositoryMock.Object);
        }

        public Mock<ISearchRepository<ServiceTypeEntity>> RepositoryMock { get; } = new();
    }
}
