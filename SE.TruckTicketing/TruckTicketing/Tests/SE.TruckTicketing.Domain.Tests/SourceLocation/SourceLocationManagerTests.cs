using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.SourceLocation;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.SourceLocation;

[TestClass]
public class SourceLocationManagerTests
{
    [TestMethod]
    public void SourceLocationManager_ScopeInvoke_ValidInstanceType()
    {
        //arrange
        var scope = new DefaultScope();

        //act //assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ManagerBase<Guid, SourceLocationEntity>));
    }

    private class DefaultScope : TestScope<SourceLocationManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object, SourceLocationProviderMock.Object, TruckTicketingProviderMock.Object);
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, SourceLocationEntity>> SourceLocationProviderMock { get; } = new();

        public Mock<IProvider<Guid, TruckTicketEntity>> TruckTicketingProviderMock { get; } = new();

        public Mock<IManager<Guid, SourceLocationEntity>> ManagerBaseMock { get; } = new();
    }
}
