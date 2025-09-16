using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TridentContrib.Extensions.Azure.Blobs;
using SE.TruckTicketing.Api.Configuration;
using SE.TruckTicketing.Contracts.Api.Models.SpartanData;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.FacilityService;
using SE.TruckTicketing.Domain.Entities.SpartanProductParameters;
using SE.TruckTicketing.Domain.Entities.SpartanSummary;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Mapper;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.SpartanSummary;

[TestClass]
public class SpartanSummaryMessageProcessorTests
{
    [TestMethod]
    public async Task Processor_BuildBaseTruckTicket_WithCorrectStatus()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var summary = new SpartanSummaryModel();

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.Source.Should().Be(TruckTicketSource.Spartan);
        actualTruckTicket.ValidationStatus.Should().Be(TruckTicketValidationStatus.Valid);
        actualTruckTicket.Status.Should().Be(TruckTicketStatus.Open);
    }

    [TestMethod]
    public async Task Processor_BuildBaseTruckTicket_WithCorrectTruckTicketData()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var summary = new SpartanSummaryModel { TransferFinishTime = DateTimeOffset.Now };

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.LoadDate.Should().Be(summary.TransferFinishTime);
    }

    [TestMethod]
    [DataRow(15.2546)]
    [DataRow(15.2936)]
    [DataRow(15.66324936)]
    public async Task Processor_BuildBaseTruckTicket_WithCorrectUnloadDensityRoundOfData(double unloadDensityValue)
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var roundOffValue = Math.Round(unloadDensityValue, 1);
        var summary = new SpartanSummaryModel { CorrectedOilDensity = unloadDensityValue };

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.UnloadOilDensity.Should().Be(roundOffValue);
    }

    [TestMethod]
    public async Task Processor_EnrichFacilityData_ExistingFacility()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var facility = new FacilityEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Facility",
            SiteId = "TSFST",
        };

        scope.FacilityProviderMock.SetupEntities(new[] { facility });
        var summary = new SpartanSummaryModel { PlantIdentifier = facility.SiteId };

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.FacilityId.Should().Be(facility.Id);
        actualTruckTicket.FacilityName.Should().Be(facility.Name);
    }

    [TestMethod]
    public async Task Processor_EnrichFacilityData_NonExistingFacility()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var summary = new SpartanSummaryModel();

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.FacilityId.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Processor_EnrichSourceLocationData_ExistingSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var sourceLocation = new SourceLocationEntity
        {
            Id = Guid.NewGuid(),
            FormattedIdentifier = "Test Identifier",
            SourceLocationName = "TestName",
        };

        scope.SourceLocationProviderMock.SetupEntities(new[] { sourceLocation });
        var summary = new SpartanSummaryModel { LocationUwi = sourceLocation.FormattedIdentifier };

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.SourceLocationId.Should().Be(sourceLocation.Id);
        actualTruckTicket.SourceLocationName.Should().Be(sourceLocation.SourceLocationName);
        actualTruckTicket.SourceLocationFormatted.Should().Be(sourceLocation.FormattedIdentifier);
    }

    [TestMethod]
    public async Task Processor_EnrichSourceLocationData_NonExistingSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var summary = new SpartanSummaryModel();

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.SourceLocationId.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Processor_EnrichTruckingCompanyData_ExistingtruckingCompany()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var truckingAccount = new AccountEntity
        {
            Id = Guid.NewGuid(),
            Name = "Truck1",
            AccountTypes = new()
            {
                Key = Guid.NewGuid(),
                List = new() { nameof(AccountTypes.TruckingCompany) },
            },
        };

        scope.AccountProviderMock.SetupEntities(new[] { truckingAccount });
        var summary = new SpartanSummaryModel { TransportCompanyName = truckingAccount.Name };

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.TruckingCompanyId.Should().Be(truckingAccount.Id);
        actualTruckTicket.TruckingCompanyName.Should().Be(truckingAccount.Name);
    }

    [TestMethod]
    public async Task Processor_EnrichSpartanProductData_NonExistingSourceLocation()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var summary = new SpartanSummaryModel();

        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.SpartanProductParameterId.Should().BeEmpty();
    }

    [TestMethod]
    public void Processor_SpartanMapperProfile_IsValid()
    {
        // arrange
        var profile = new SpartanSummaryMapperProfile();
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile(profile));

        // assert
        configuration.AssertConfigurationIsValid();
    }

    [TestMethod]
    public async Task Processor_AssignFacilityServiceAsync_NoSpartanProductInFacilityService()
    {
        // arrange
        var scope = new DefaultScope();
        TruckTicketEntity actualTruckTicket = null;
        scope.TruckTicketManagerMock.Setup(manager => manager.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
             .Callback((TruckTicketEntity entity, bool _) => actualTruckTicket = entity);

        var facility = new FacilityEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Facility",
            SiteId = "TAFA",
        };

        var spartanProductParameter = new SpartanProductParameterEntity
        {
            Id = Guid.NewGuid(),
            ProductName = "FST",
            LocationOperatingStatus = LocationOperatingStatus.Drilling,
            MinWaterPercentage = 10,
            MaxWaterPercentage = 50,
            MinFluidDensity = 5,
            MaxFluidDensity = 50,
        };

        List<FacilityServiceSpartanProductParameterEntity> facilityServiceSpartanProductParameter = new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                SpartanProductParameterId = spartanProductParameter.Id,
                SpartanProductParameterName = spartanProductParameter.ProductName,
            },
            new()
            {
                Id = Guid.NewGuid(),
                SpartanProductParameterId = Guid.NewGuid(),
                SpartanProductParameterName = "Test",
            },
        };

        var facilityServiceEntity = new FacilityServiceEntity
        {
            Id = Guid.NewGuid(),
            SpartanProductParameters = facilityServiceSpartanProductParameter,
            FacilityId = facility.Id,
        };

        scope.FacilityProviderMock.SetupEntities(new[] { facility });

        var facilityServiceSubstanceEntity = new FacilityServiceSubstanceIndexEntity
        {
            Id = Guid.NewGuid(),
            FacilityServiceId = facilityServiceEntity.Id,
            FacilityId = facility.Id,
        };

        scope.FacilityServiceManagerMock.SetupEntities(new[] { facilityServiceSubstanceEntity });

        var summary = new SpartanSummaryModel
        {
            PlantIdentifier = facility.SiteId,
            ProductName = spartanProductParameter.ProductName,
            RoundedCorrectedEmulsionWaterCut = 12,
            CorrectedOilDensity = 15,
            LocationOperatingStatus = LocationOperatingStatus.Drilling,
        };

        scope.SpartanProductParameterProviderMock.SetupEntities(new[] { spartanProductParameter });
        scope.FacilityServiceProviderMock.SetupEntities(new[] { facilityServiceEntity });
        // act
        await scope.InstanceUnderTest.Process(new EntityEnvelopeModel<SpartanSummaryModel> { Payload = summary });

        // assert
        actualTruckTicket.LockedSpartanFacilityServiceId.Should().NotBeEmpty();
    }

    private class DefaultScope : TestScope<SpartanSummaryMessageProcessor>
    {
        public DefaultScope()
        {
            var mapperRegistry = new ServiceMapperRegistry(new Profile[] { new SpartanSummaryMapperProfile() });

            InstanceUnderTest = new(TruckTicketManagerMock.Object,
                                    FacilityProviderMock.Object,
                                    SourceLocationProviderMock.Object,
                                    SpartanProductParameterProviderMock.Object,
                                    FacilityServiceProviderMock.Object,
                                    AccountProviderMock.Object,
                                    FacilityServiceManagerMock.Object,
                                    NoteManagerMock.Object,
                                    mapperRegistry,
                                    new LeaseManager("", ""));
        }

        public Mock<IManager<Guid, TruckTicketEntity>> TruckTicketManagerMock { get; } = new();

        public Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock { get; } = new();

        public Mock<IProvider<Guid, SourceLocationEntity>> SourceLocationProviderMock { get; } = new();

        public Mock<ILeaseObjectBlobStorage> LeaseManagerMock { get; } = new();

        public Mock<IProvider<Guid, SpartanProductParameterEntity>> SpartanProductParameterProviderMock { get; } = new();

        public Mock<IProvider<Guid, AccountEntity>> AccountProviderMock { get; } = new();

        public Mock<IProvider<Guid, FacilityServiceEntity>> FacilityServiceProviderMock { get; } = new();

        public Mock<IManager<Guid, FacilityServiceSubstanceIndexEntity>> FacilityServiceManagerMock { get; } = new();

        private Mock<IManager<Guid, NoteEntity>> NoteManagerMock { get; } = new();

        public void InitializeEntities()
        {
            var scope = new DefaultScope();

            var facility = new FacilityEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Facility",
                SiteId = "TSFST",
            };

            scope.FacilityProviderMock.SetupEntities(new[] { facility });
        }

        public class LeaseManager : BlobStorage, ILeaseObjectBlobStorage
        {
            public LeaseManager(string connectionString, string defaultContainerName) : base(connectionString, defaultContainerName)
            {
            }

            protected override string Prefix { get; }

            public new async Task<T> AcquireLeaseAndExecute<T>(Func<Task<T>> task,
                                                               string leaseBlobName,
                                                               string containerName = "lease-objects",
                                                               TimeSpan? timeout = null,
                                                               CancellationToken cancellationToken = default)
            {
                return await task();
            }
        }
    }
}
