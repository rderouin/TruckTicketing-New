using System;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.FacilityService;

[TestClass]
public class FacilityServiceManagerTests
{
    [TestMethod]
    [TestCategory("Unit")]
    public void FacilityServiceManager_CanBeInstantiated()
    {
        var scope = new DefaultScope();
        scope.InstanceUnderTest.Should().NotBeNull();
    }

    private class DefaultScope : TestScope<FacilityServiceManager>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object,
                                    FacilitySerivceProviderMock.Object,
                                    ValidationManagerMock.Object,
                                    WorkFlowMnagerMock.Object);
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, FacilityServiceEntity>> FacilitySerivceProviderMock { get; } = new();

        public Mock<IValidationManager<FacilityServiceEntity>> ValidationManagerMock { get; } = new();

        public Mock<IWorkflowManager<FacilityServiceEntity>> WorkFlowMnagerMock { get; } = new();
    }
}
