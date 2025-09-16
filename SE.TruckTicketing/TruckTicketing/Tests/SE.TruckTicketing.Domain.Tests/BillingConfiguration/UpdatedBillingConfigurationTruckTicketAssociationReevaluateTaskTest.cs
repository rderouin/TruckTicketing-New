using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.BillingConfigurations.Tasks;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Business;
using Trident.Extensions;
using Trident.Search;
using Trident.Testing.TestScopes;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.BillingConfiguration;

[TestClass]
public class UpdatedBillingConfigurationTruckTicketAssociationReevaluateTaskTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_ShouldBeInstantiatedAsync()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationEntity.MatchCriteria.RemoveAt(0);
        var context = scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration);
        context.Operation = Operation.Update;
        var mustAlwaysRun = await scope.InstanceUnderTest.ShouldRun(context);
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        mustAlwaysRun.Should().BeFalse();
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_DefaultBillingConfiguration_ShouldNotBeInstantiatedAsync()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationEntity.IsDefaultConfiguration = true;
        var mustAlwaysRun = await scope.InstanceUnderTest.ShouldRun(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        mustAlwaysRun.Should().BeFalse();
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_NoMatchPredicates_ShouldNotBeInstantiatedAsync()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        billingConfigurationEntity.MatchCriteria = new();
        var mustAlwaysRun = await scope.InstanceUnderTest.ShouldRun(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        mustAlwaysRun.Should().BeFalse();
        operationStage.Should().Be(OperationStage.PostValidation);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_NoTruckTicketsAssociatedToUpdatedBillingConfiguration_NoTruckTicketUpdate()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Never);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_InvalidTruckTicketsAssociatedToUpdatedBillingConfiguration_NoNewMatchingBillingConfig_TruckTicketUpdated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);

        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_InvalidTruckTicketsAssociatedToUpdatedBillingConfiguration_NewMatchingBillingConfig_TruckTicketUpdated()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);

        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        var newMatchingBillingConfiguration = scope.DefaultBillingConfiguration.Clone();
        newMatchingBillingConfiguration.Id = Guid.NewGuid();
        newMatchingBillingConfiguration.CustomerGeneratorId = truckTicketWithDefaultBillingConfiguration.GeneratorId;
        newMatchingBillingConfiguration.Facilities = new() { List = new() { truckTicketWithDefaultBillingConfiguration.FacilityId } };
        scope.GetMatchingBillingConfigurationSetup(billingConfigurationEntity);

        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_ValidTruckTicketsAssociatedToUpdatedBillingConfiguration_NoTruckTicketUpdate()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);

        scope.MatchPredicateRankManagerMock.Setup(x => x.IsBillingConfigurationMatch(It.IsAny<TruckTicketEntity>(),
                                                                                     It.IsAny<MatchPredicateEntity>())).Returns(true);

        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            false), Times.Never);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_NewMatchingBillingConfiguration_DifferentCustomer_NoSalesLinePriceRefresh()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        truckTicketWithDefaultBillingConfiguration.BillingCustomerId = billingConfigurationEntity.BillingCustomerAccountId;
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);

        var salesLineEntity = scope.DefaultSalesLine.Clone();
        salesLineEntity.TruckTicketId = truckTicketWithDefaultBillingConfiguration.Id;
        var salesLineResults = new SearchResults<SalesLineEntity, SearchCriteria> { Results = new List<SalesLineEntity> { salesLineEntity } };

        scope.SalesLineManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(salesLineResults);

        scope.SetupSalesLinePriceRefresh(salesLineEntity);

        scope.GetMatchingBillingConfigurationSetup(billingConfigurationEntity);

        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));

        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.SalesLineManagerMock.Verify(tt => tt.Update(It.IsAny<SalesLineEntity>(),
                                                          true), Times.Never);

        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_NewMatchingBillingConfiguration_DifferentCustomer_SalesLinePriceRefreshAndUpdate()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        truckTicketWithDefaultBillingConfiguration.BillingCustomerId = Guid.NewGuid();
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);
        var salesLineEntity = scope.DefaultSalesLine.Clone();
        salesLineEntity.TruckTicketId = truckTicketWithDefaultBillingConfiguration.Id;
        var salesLineResults = new SearchResults<SalesLineEntity, SearchCriteria> { Results = new List<SalesLineEntity> { salesLineEntity } };

        scope.SalesLineManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(salesLineResults);

        scope.SetupSalesLinePriceRefresh(salesLineEntity);
        scope.GetMatchingBillingConfigurationSetup(billingConfigurationEntity);

        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));

        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.SalesLineManagerMock.Verify(tt => tt.Update(It.IsAny<SalesLineEntity>(),
                                                          true), Times.Once);

        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Once);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask_InvalidTruckTicketsAssociatedToUpdatedBillingConfiguration_NoNewMatchingBillingConfig_DeleteSalesLines()
    {
        // arrange
        var scope = new DefaultScope();

        // act
        var runOrder = scope.InstanceUnderTest.RunOrder;
        var truckTicketWithDefaultBillingConfiguration = scope.DefaultTruckTicket.Clone();
        scope.TruckTicketManagerSetup(truckTicketWithDefaultBillingConfiguration);

        var salesLineEntity = scope.DefaultSalesLine.Clone();
        var billingConfigurationEntity = scope.DefaultBillingConfiguration.Clone();
        salesLineEntity.TruckTicketId = truckTicketWithDefaultBillingConfiguration.Id;
        var salesLineResults = new SearchResults<SalesLineEntity, SearchCriteria> { Results = new List<SalesLineEntity> { salesLineEntity } };

        scope.SalesLineManagerMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(salesLineResults);

        scope.SetupSalesLinePriceRefresh(salesLineEntity);
        await scope.InstanceUnderTest.Run(scope.CreateContextWithValidBillingConfigurationEntity(billingConfigurationEntity, scope.DefaultBillingConfiguration));
        var operationStage = scope.InstanceUnderTest.Stage;
        // assert
        runOrder.Should().BePositive();
        operationStage.Should().Be(OperationStage.PostValidation);
        scope.TruckTicketManagerMock.Verify(tt => tt.Update(It.IsAny<TruckTicketEntity>(),
                                                            true), Times.Once);

        scope.SalesLineManagerMock.Verify(tt => tt.Delete(It.IsAny<SalesLineEntity>(),
                                                          true), Times.Once);
    }

    private class DefaultScope : TestScope<UpdatedBillingConfigurationTruckTicketAssociationReevaluateTask>
    {
        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                BillingConfigurationEnabled = true,
                BillingContactAddress = "599 Harry Square",
                BillingContactId = Guid.NewGuid(),
                BillingContactName = "Dr. Eduardo Lesch",
                BillingCustomerAccountId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty,
                CreatedById = Guid.NewGuid().ToString(),
                CustomerGeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
                CustomerGeneratorName = "Kemmer, Maggio and Reynolds",
                Description = null,
                EmailDeliveryEnabled = true,
                FieldTicketsUploadEnabled = false,
                StartDate = new DateTime(2022, 01, 01, 22, 02, 02, 0),
                EndDate = null,
                GeneratorRepresentativeId = Guid.NewGuid(),
                IncludeExternalAttachmentInLC = true,
                IncludeInternalAttachmentInLC = true,
                IsDefaultConfiguration = false,
                IncludeForAutomation = true,
                LastComment = "new comment added",
                LoadConfirmationsEnabled = true,
                LoadConfirmationFrequency = null,
                RigNumber = null,
                ThirdPartyBillingContactAddress = "07958 Althea Ford",
                ThirdPartyBillingContactId = Guid.NewGuid(),
                ThirdPartyBillingContactName = "Barbara McClure II",
                ThirdPartyCompanyId = Guid.NewGuid(),
                ThirdPartyCompanyName = null,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "Panth Shah",
                UpdatedById = Guid.NewGuid().ToString(),
                Facilities = new()
                {
                    Key = Guid.NewGuid(),
                    List = new() { Guid.NewGuid() },
                },
                EDIValueData = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Invoice Number",
                        EDIFieldValueContent = null,
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        EDIFieldDefinitionId = Guid.NewGuid(),
                        EDIFieldName = "Policy Name",
                        EDIFieldValueContent = null,
                    },
                },
                EmailDeliveryContacts = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        EmailAddress = "Noble60@gmail.com",
                        IsAuthorized = true,
                        SignatoryContact = "Jenna Schroeder",
                    },
                },
                Signatories = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AccountContactId = Guid.NewGuid(),
                        IsAuthorized = true,
                        Address = "6557 Cortez Field",
                        Email = "Janae_Corkery95@gmail.com",
                        FirstName = "Johnnie Kunde Sr.",
                        LastName = null,
                        PhoneNumber = "510-297-3998",
                    },
                },
                MatchCriteria = new()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Landfill,
                        StreamValueState = MatchPredicateValueState.Value,
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Drilling,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Stream = Stream.Pipeline,
                        StreamValueState = MatchPredicateValueState.Value,
                        IsEnabled = true,
                        ServiceType = null,
                        ServiceTypeId = null,
                        ServiceTypeValueState = MatchPredicateValueState.Any,
                        SourceIdentifier = null,
                        SourceLocationId = null,
                        SourceLocationValueState = MatchPredicateValueState.Any,
                        SubstanceId = null,
                        SubstanceName = null,
                        SubstanceValueState = MatchPredicateValueState.Any,
                        WellClassification = WellClassifications.Completions,
                        WellClassificationState = MatchPredicateValueState.Value,
                        StartDate = null,
                        EndDate = null,
                        Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
                    },
                },
            };

        public readonly SalesLineEntity DefaultSalesLine = new()
        {
            Id = Guid.NewGuid(),
            TruckTicketId = Guid.NewGuid(),
        };

        public readonly TruckTicketEntity DefaultTruckTicket = new()
        {
            Id = Guid.NewGuid(),
            Acknowledgement = "",
            AdditionalServicesEnabled = true,
            BillOfLading = "",
            BillingCustomerId = Guid.NewGuid(),
            BillingCustomerName = "",
            ClassNumber = "",
            CountryCode = CountryCode.CA,
            CustomerId = Guid.NewGuid(),
            CustomerName = "",
            Date = DateTimeOffset.UtcNow,
            Destination = "",
            FacilityId = Guid.NewGuid(),
            FacilityName = "",
            FacilityServiceSubstanceId = Guid.NewGuid(),
            ServiceTypeId = Guid.NewGuid(),
            GeneratorId = Guid.Parse("79e571ea-0d9b-41e4-8739-363f304ade33"),
            GeneratorName = "",
            GrossWeight = 1,
            IsDeleted = true,
            IsDow = true,
            Level = "",
            LoadDate = DateTimeOffset.UtcNow,
            LocationOperatingStatus = LocationOperatingStatus.Completions,
            ManifestNumber = "",
            MaterialApprovalId = Guid.NewGuid(),
            MaterialApprovalNumber = "",
            NetWeight = 1,
            OilVolume = 1,
            OilVolumePercent = 1,
            Quadrant = "",
            SaleOrderId = Guid.NewGuid(),
            SaleOrderNumber = "",
            Stream = Stream.Pipeline,
            ServiceType = "",
            SolidVolume = 1,
            SolidVolumePercent = 1,
            Source = TruckTicketSource.Manual,
            SourceLocationFormatted = "",
            SourceLocationId = Guid.NewGuid(),
            SourceLocationName = "",
            SpartanProductParameterDisplay = "",
            SpartanProductParameterId = Guid.NewGuid(),
            Status = TruckTicketStatus.Approved,
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "",
            TareWeight = 1,
            TicketNumber = "",
            TimeIn = DateTimeOffset.UtcNow,
            TimeOut = DateTimeOffset.UtcNow,
            Tnorms = "",
            TotalVolume = 1,
            TotalVolumePercent = 1,
            TrackingNumber = "",
            TrailerNumber = "",
            TruckNumber = "",
            TruckingCompanyId = Guid.NewGuid(),
            TruckingCompanyName = "",
            UnNumber = "",
            UnloadOilDensity = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            UploadFieldTicket = true,
            ValidationStatus = TruckTicketValidationStatus.Valid,
            WaterVolume = 1,
            WaterVolumePercent = 1,
            WellClassification = WellClassifications.Completions,
            AdditionalServices = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AdditionalServiceName = "Service 1",
                    AdditionalServiceNumber = "123",
                    AdditionalServiceQuantity = 1,
                    IsPrimarySalesLine = true,
                    ProductId = Guid.NewGuid(),
                },
            },
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Container = "",
                    File = "Sample-Work-Ticket-US-Stub.pdf",
                    Path = "66e6ba1e-afc2-4a87-923e-41335a92b98f/Sample-Work-Ticket-US-Stub.pdf",
                },
            },
            BillingContact = new()
            {
                AccountContactId = Guid.NewGuid(),
                Address = "",
                Email = "",
                Name = "",
                PhoneNumber = "",
            },
            EdiFieldValues = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    EDIFieldDefinitionId = Guid.NewGuid(),
                    EDIFieldName = "Invoice Number",
                    EDIFieldValueContent = "123",
                },
            },
            Signatories = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountContactId = Guid.NewGuid(),
                    ContactAddress = "512 Jamarcus Islands",
                    ContactEmail = "Waldo_Harvey32@yahoo.com",
                    ContactName = "Kent Purdy",
                    ContactPhoneNumber = "742-548-1249",
                    IsAuthorized = true,
                },
            },
        };

        public DefaultScope()
        {
            InstanceUnderTest = new(TruckTicketManagerMock.Object,
                                    SalesLineManagerMock.Object,
                                    MatchPredicateRankManagerMock.Object);
        }

        public Mock<ITruckTicketManager> TruckTicketManagerMock { get; } = new();

        public Mock<ISalesLineManager> SalesLineManagerMock { get; } = new();

        public Mock<IMatchPredicateRankManager> MatchPredicateRankManagerMock { get; } = new();

        public BusinessContext<BillingConfigurationEntity> CreateContextWithValidBillingConfigurationEntity(BillingConfigurationEntity target, BillingConfigurationEntity original = null)
        {
            return new(target, original);
        }

        public void SetupSalesLinePriceRefresh(params SalesLineEntity[] salesLineEntities)
        {
            SalesLineManagerMock.Setup(x => x.PriceRefresh(It.IsAny<List<SalesLineEntity>>()))
                                .ReturnsAsync(salesLineEntities.ToList());
        }

        public void TruckTicketManagerSetup(params TruckTicketEntity[] truckTicketEntities)
        {
            TruckTicketManagerMock.Setup(x => x.Get(It.IsAny<Expression<Func<TruckTicketEntity, bool>>>(),
                                                    null,
                                                    It.IsAny<List<string>>(),
                                                    false))
                                  .ReturnsAsync(truckTicketEntities.ToList());
        }

        public void GetMatchingBillingConfigurationSetup(params BillingConfigurationEntity[] billingConfigurationEntities)
        {
            TruckTicketManagerMock.Setup(x => x.GetMatchingBillingConfigurations(It.IsAny<TruckTicketEntity>()))
                                  .ReturnsAsync(billingConfigurationEntities.ToList());
        }
    }
}
