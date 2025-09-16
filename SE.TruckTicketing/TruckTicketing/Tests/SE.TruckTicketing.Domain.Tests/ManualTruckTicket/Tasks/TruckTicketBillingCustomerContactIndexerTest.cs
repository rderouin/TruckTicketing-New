using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket.Tasks;

[TestClass]
public class TruckTicketBillingCustomerContactIndexerTest
{
    [TestMethod]
    public async Task Task_ShouldNotRun_WhenTargetIsNull()
    {
        // arrange
        var scope = new DefaultScope();
        var context = new BusinessContext<TruckTicketEntity>(null);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenInsert()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<TruckTicketEntity>(targetEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenBillingCustomerContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        var targetEntity = originalEntity.Clone();
        targetEntity.BillingCustomerId = Guid.NewGuid();
        targetEntity.BillingContact = GenFu.GenFu.New<BillingContactEntity>();
        targetEntity.BillingContact.AccountContactId = Guid.NewGuid();
        targetEntity.BillingContact.Id = Guid.Empty;
        var context = new BusinessContext<TruckTicketEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldRun_WhenBillingCustomerContactSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        originalEntity.BillingCustomerId = Guid.Empty;
        originalEntity.BillingContact = null;
        var targetEntity = originalEntity.Clone();
        targetEntity.BillingCustomerId = Guid.NewGuid();
        targetEntity.BillingContact = GenFu.GenFu.New<BillingContactEntity>();
        targetEntity.BillingContact.AccountContactId = Guid.NewGuid();
        targetEntity.BillingContact.Id = Guid.Empty;
        var context = new BusinessContext<TruckTicketEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoContactUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<TruckTicketEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldNotRun_WhenNoCustomerSelected()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        originalEntity.BillingCustomerId = Guid.Empty;
        originalEntity.BillingContact = new();
        var targetEntity = originalEntity.Clone();
        var context = new BusinessContext<TruckTicketEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.ShouldRun(context);

        // assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task Task_ShouldCreateNewIndex_WhenBillingCustomerAccountContactIsSet()
    {
        // arrange
        var scope = new DefaultScope();
        var entity = scope.ValidTruckTicketEntity.Clone();
        entity.BillingCustomerId = Guid.NewGuid();
        entity.BillingContact.AccountContactId = Guid.NewGuid();
        var context = new BusinessContext<TruckTicketEntity>(entity, scope.ValidTruckTicketEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Insert(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerAccountContactMatch(entity, index, false)), It.IsAny<bool>()));
    }

    [TestMethod]
    public async Task Task_ShouldEnableExistingIndex_WhenBillingCustomerAccountContactIsUpdated()
    {
        // arrange
        var scope = new DefaultScope();
        var originalEntity = scope.ValidTruckTicketEntity;
        var targetEntity = originalEntity.Clone();

        targetEntity.BillingCustomerId = Guid.NewGuid();
        targetEntity.BillingContact = GenFu.GenFu.New<BillingContactEntity>();
        targetEntity.BillingContact.AccountContactId = Guid.NewGuid();

        var existingIndices = GenFu.GenFu.ListOf<AccountContactReferenceIndexEntity>(2);
        existingIndices.ForEach(x => x.ReferenceEntityId = targetEntity.Id);
        existingIndices[0].AccountContactId = originalEntity.BillingContact.AccountContactId;
        existingIndices[0].AccountId = originalEntity.BillingCustomerId;
        existingIndices[0].IsDisabled = false;

        existingIndices[1].AccountContactId = targetEntity.BillingContact.AccountContactId;
        existingIndices[1].AccountId = targetEntity.BillingCustomerId;
        existingIndices[1].IsDisabled = true;

        scope.IndexProviderMock.SetupEntities(existingIndices);
        var context = new BusinessContext<TruckTicketEntity>(targetEntity, originalEntity);

        // act
        var result = await scope.InstanceUnderTest.Run(context);

        // assert
        result.Should().BeTrue();
        scope.IndexProviderMock.Verify(p => p.Update(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerAccountContactMatch(originalEntity, index, true)), It.IsAny<bool>()));
        scope.IndexProviderMock.Verify(p => p.Update(It.Is<AccountContactReferenceIndexEntity>(index => IsBillingCustomerAccountContactMatch(targetEntity, index, false)), It.IsAny<bool>()));
        scope.IndexProviderMock.Verify(s => s.Insert(It.IsAny<AccountContactReferenceIndexEntity>(), It.IsAny<bool>()), Times.Never);
    }

    private bool IsBillingCustomerAccountContactMatch(TruckTicketEntity entity, AccountContactReferenceIndexEntity index, bool isDisabled = false)
    {
        return entity.Id == index.ReferenceEntityId &&
               entity.BillingCustomerId == index.AccountId &&
               entity.BillingContact.AccountContactId == index.AccountContactId &&
               isDisabled == index.IsDisabled;
    }

    public class DefaultScope : TestScope<TruckTicketBillingCustomerContactIndexer>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(IndexProviderMock.Object);
        }

        public Mock<IProvider<Guid, AccountContactReferenceIndexEntity>> IndexProviderMock { get; } = new();

        public TruckTicketEntity ValidTruckTicketEntity =>
            new()
            {
                Id = Guid.NewGuid(),
                AdditionalServicesEnabled = true,
                BillOfLading = "",
                BillingCustomerId = Guid.NewGuid(),
                BillingCustomerName = null,
                ClassNumber = "",
                CountryCode = CountryCode.CA,
                CustomerId = Guid.NewGuid(),
                CustomerName = "Hayes Customer",
                Date = DateTime.UtcNow,
                Destination = "",
                FacilityId = Guid.NewGuid(),
                FacilityName = "Fox Creek Landfill",
                FacilityServiceSubstanceId = Guid.NewGuid(),
                GeneratorId = Guid.NewGuid(),
                GeneratorName = "Test Generator Company",
                GrossWeight = 10,
                IsDeleted = false,
                IsDow = false,
                Level = "5",
                LoadDate = DateTimeOffset.Now,
                LocationOperatingStatus = LocationOperatingStatus.Drilling,
                ManifestNumber = "",
                MaterialApprovalId = Guid.NewGuid(),
                MaterialApprovalNumber = "SESC213063",
                NetWeight = 5,
                OilVolume = 0.39999999999999997,
                OilVolumePercent = 12,
                Quadrant = "8-quad",
                SaleOrderId = Guid.NewGuid(),
                SaleOrderNumber = null,
                ServiceType = null,
                SolidVolume = 2,
                SolidVolumePercent = 40,
                Source = TruckTicketSource.Manual,
                SourceLocationFormatted = "123/A-323-B/323-K-23/90",
                SourceLocationId = Guid.NewGuid(),
                SourceLocationName = null,
                SpartanProductParameterDisplay = null,
                SpartanProductParameterId = Guid.NewGuid(),
                Status = TruckTicketStatus.Stub,
                SubstanceId = Guid.NewGuid(),
                SubstanceName = "Sludge - Emulsion",
                TareWeight = 5,
                TicketNumber = "FOXLF10000001-LF",
                TimeIn = DateTimeOffset.UtcNow,
                TimeOut = DateTimeOffset.Now,
                Tnorms = "",
                TotalVolume = 4.8,
                TotalVolumePercent = 100,
                TrackingNumber = "",
                TrailerNumber = "",
                TruckNumber = "",
                TruckingCompanyId = Guid.NewGuid(),
                TruckingCompanyName = "Hayes",
                UnNumber = "",
                UnloadOilDensity = 80,
                UploadFieldTicket = true,
                ValidationStatus = TruckTicketValidationStatus.Unverified,
                WaterVolume = 2.4,
                WaterVolumePercent = 48,
                WellClassification = WellClassifications.Completions,
                AdditionalServices = new(),
                BillingContact = new()
                {
                    AccountContactId = Guid.NewGuid(),
                    Address = "512 Jamarcus Islands",
                    Email = "Waldo_Harvey32@yahoo.com",
                    Name = "Kent",
                    PhoneNumber = "742-548-1249",
                },
                EdiFieldValues = new(),
                Signatories = new()
                {
                    new()
                    {
                        AccountContactId = Guid.NewGuid(),
                        AccountId = Guid.NewGuid(),
                        ContactAddress = "512 Jamarcus Islands",
                        ContactEmail = "Waldo_Harvey32@yahoo.com",
                        ContactName = "Kent Purdy",
                        ContactPhoneNumber = "742-548-1249",
                        IsAuthorized = true,
                    },
                },
            };
    }
}
