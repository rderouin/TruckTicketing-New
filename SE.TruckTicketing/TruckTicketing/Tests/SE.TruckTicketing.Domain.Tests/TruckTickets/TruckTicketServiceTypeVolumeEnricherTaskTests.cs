using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketServiceTypeVolumeEnricherTaskTests
{
    [DataTestMethod]
    [DataRow(TruckTicketType.WT)]
    [DataRow(TruckTicketType.SP)]
    public async Task Task_ShouldRun_ManualWorkTickets(TruckTicketType ticketType)
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = ticketType;
        ticket.Source = TruckTicketSource.Manual;
        ticket.TotalVolume = 10;
        ticket.ServiceTypeId = serviceType.Id;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_ChangedVolume()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.TotalVolume = 10;
        ticket.ServiceTypeId = serviceType.Id;

        var originalTicket = ticket.Clone();
        originalTicket.TotalVolume += 1;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_ChangedSerivceType()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        var otherServiceType = serviceType.Clone();
        otherServiceType.Id = Guid.NewGuid();
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType, otherServiceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.TotalVolume = 10;
        ticket.ServiceTypeId = serviceType.Id;

        var originalTicket = ticket.Clone();
        originalTicket.ServiceTypeId = otherServiceType.Id;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_LandfillTicket()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.LF;
        ticket.Source = TruckTicketSource.Scaled;
        ticket.ServiceTypeId = serviceType.Id;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_AutoSpartanTicket()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.SP;
        ticket.Source = TruckTicketSource.Spartan;
        ticket.ServiceTypeId = serviceType.Id;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_ZeroTotalVolume()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.ServiceTypeId = serviceType.Id;
        ticket.TotalVolume = 0;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_BlankServiceType()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.ServiceTypeId = default;
        ticket.TotalVolume = 10;
        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_NoVolumeChangeOrServiceTypeChange()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.ServiceTypeId = serviceType.Id;
        ticket.TotalVolume = 10;

        var originalTicket = ticket.Clone();
        var context = new BusinessContext<TruckTicketEntity>(ticket, originalTicket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRunNotRun_NonExistentServiceType()
    {
        // Arrange
        var scope = new DefaultScope();

        var serviceType = GenFu.GenFu.New<ServiceTypeEntity>();
        serviceType.Id = Guid.NewGuid();
        serviceType.ReportAsCutType = ReportAsCutTypes.Water;
        scope.ServiceTypeProviderMock.SetupEntities(new[] { serviceType });

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.TruckTicketType = TruckTicketType.WT;
        ticket.Source = TruckTicketSource.Manual;
        ticket.ServiceTypeId = Guid.NewGuid();
        ticket.TotalVolume = 10;

        var context = new BusinessContext<TruckTicketEntity>(ticket);

        // Act
        var shouldRun = await scope.InstanceUnderTest.ShouldRun(context);

        // Assert
        shouldRun.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldSetOilCutTypeParameters()
    {
        // Arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.ReportAsCutType = ReportAsCutTypes.Oil;
        ticket.TotalVolume = 10;

        var newTicket = ticket.Clone();
        newTicket.TotalVolume = 20;
        var context = new BusinessContext<TruckTicketEntity>(newTicket, ticket);

        // Act
        await scope.InstanceUnderTest.Run(context);

        // Assert
        newTicket.OilVolume.Should().Be(newTicket.TotalVolume);
        newTicket.OilVolumePercent.Should().Be(100);
        newTicket.WaterVolume.Should().Be(0);
        newTicket.WaterVolumePercent.Should().Be(0);
        newTicket.SolidVolume.Should().Be(0);
        newTicket.SolidVolumePercent.Should().Be(0);
    }

    [TestMethod]
    public async Task Task_ShouldSetWaterCutTypeParameters()
    {
        // Arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.ReportAsCutType = ReportAsCutTypes.Water;
        ticket.TotalVolume = 10;

        var newTicket = ticket.Clone();
        newTicket.TotalVolume = 20;
        var context = new BusinessContext<TruckTicketEntity>(newTicket, ticket);

        // Act
        await scope.InstanceUnderTest.Run(context);

        // Assert
        newTicket.OilVolume.Should().Be(0);
        newTicket.OilVolumePercent.Should().Be(0);
        newTicket.WaterVolume.Should().Be(newTicket.TotalVolume);
        newTicket.WaterVolumePercent.Should().Be(100);
        newTicket.SolidVolume.Should().Be(0);
        newTicket.SolidVolumePercent.Should().Be(0);
    }

    [TestMethod]
    public async Task Task_ShouldSetSolidCutTypeParameters()
    {
        // Arrange
        var scope = new DefaultScope();

        var ticket = GenFu.GenFu.New<TruckTicketEntity>();
        ticket.ReportAsCutType = ReportAsCutTypes.Solids;
        ticket.TotalVolume = 10;

        var newTicket = ticket.Clone();
        newTicket.TotalVolume = 20;
        var context = new BusinessContext<TruckTicketEntity>(newTicket, ticket);

        // Act
        await scope.InstanceUnderTest.Run(context);

        // Assert
        newTicket.OilVolume.Should().Be(0);
        newTicket.OilVolumePercent.Should().Be(0);
        newTicket.WaterVolume.Should().Be(0);
        newTicket.WaterVolumePercent.Should().Be(0);
        newTicket.SolidVolume.Should().Be(newTicket.TotalVolume);
        newTicket.SolidVolumePercent.Should().Be(100);
    }

    public class DefaultScope : TestScope<TruckTicketServiceTypeVolumeEnricherTask>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(ServiceTypeProviderMock.Object);
        }

        public Mock<IProvider<Guid, ServiceTypeEntity>> ServiceTypeProviderMock { get; } = new();
    }
}
