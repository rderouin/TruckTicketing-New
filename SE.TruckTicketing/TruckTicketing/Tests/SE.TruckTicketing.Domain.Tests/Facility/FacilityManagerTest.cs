using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Facilities;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.Facility;

[TestClass]
public class FacilityManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityManager_ScopeInvoke_ValidInstanceType()
    {
        // arrange
        var scope = new DefaultScope();
        // act / assert
        Assert.IsInstanceOfType(scope.InstanceUnderTest, typeof(ManagerBase<Guid, FacilityEntity>));
    }

    private class DefaultScope : TestScope<FacilityManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object,
                                    TruckTicketProviderMock.Object,
                                    TruckTicketValidationManagerMock.Object,
                                    TruckTicketWorkflowManagerMock.Object);
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, FacilityEntity>> TruckTicketProviderMock { get; } = new();

        public Mock<IValidationManager<FacilityEntity>> TruckTicketValidationManagerMock { get; } = new();

        public Mock<IWorkflowManager<FacilityEntity>> TruckTicketWorkflowManagerMock { get; } = new();
    }
}
