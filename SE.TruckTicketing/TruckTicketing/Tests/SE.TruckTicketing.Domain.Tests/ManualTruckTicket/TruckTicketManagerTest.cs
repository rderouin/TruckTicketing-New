using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Tests.ManualTruckTicket;

[TestClass]
public class TruckTicketManagerTest
{
    private const string Landfill_FacilityId = "ae62f28e-fd0b-4594-b235-0e254bc4771a";

    private const string Pipeline_FacilityId = "42b89075-213e-48ab-9a21-b33e9bfc741d";

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_CreateManualTruckTicket_LandfillFacilityType_SequenceTypeShouldBeScaleTicket()
    {
        // arrange
        var scope = new DefaultScope();
        // act
        await scope.InstanceUnderTest.CreatePrePrintedTruckTicketStubs(Guid.Parse(Landfill_FacilityId), 5);

        //// assert
        scope.SequenceNumberGeneratorMock.Verify(num => num.GenerateSequenceNumbers(It.Is<string>(sequenceType => sequenceType == SequenceTypes.ScaleTicket), It.IsAny<string>(),
                                                                                    It.IsAny<int>(),
                                                                                    It.IsAny<string>(),
                                                                                    It.IsAny<string>()));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_CreateManualTruckTicket_PipelineFacilityType_SequenceTypeShouldBeWorkTicket()
    {
        // arrange
        var scope = new DefaultScope();
        // act
        await scope.InstanceUnderTest.CreatePrePrintedTruckTicketStubs(Guid.Parse(Pipeline_FacilityId), 5);

        //// assert
        scope.SequenceNumberGeneratorMock.Verify(num => num.GenerateSequenceNumbers(It.Is<string>(sequenceType => sequenceType == SequenceTypes.WorkTicket), It.IsAny<string>(),
                                                                                    It.IsAny<int>(),
                                                                                    It.IsAny<string>(),
                                                                                    It.IsAny<string>()));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_CreateManualTruckTicket_GetByFacilityId_ValidPrefixForSequenceGeneration()
    {
        // arrange
        var scope = new DefaultScope();
        // act
        await scope.InstanceUnderTest.CreatePrePrintedTruckTicketStubs(Guid.Parse(Landfill_FacilityId), 5);

        //// assert
        scope.SequenceNumberGeneratorMock.Verify(num => num.GenerateSequenceNumbers(It.IsAny<string>(), It.Is<string>(prefix => prefix == "TAFA"),
                                                                                    It.IsAny<int>(),
                                                                                    It.IsAny<string>(),
                                                                                    It.IsAny<string>()));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_GetMatchingBillingConfigurations_GetBillingConfigurationsIncludedInAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        // act
        await scope.InstanceUnderTest.GetMatchingBillingConfigurations(scope.DefaultTruckTicket);

        //// assert
        scope.MatchPredicateManagerMock.Verify(num => num.GetBillingConfigurations(It.IsAny<TruckTicketEntity>()));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_GetMatchingBillingConfigurations_GetBillingConfigurationsNotIncludedInAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        // act
        await scope.InstanceUnderTest.GetMatchingBillingConfigurations(scope.DefaultTruckTicket);

        //// assert
        scope.MatchPredicateManagerMock.Verify(num => num.GetBillingConfigurations(It.IsAny<TruckTicketEntity>()));
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_GetMatchingBillingConfigurations_GetRankAndMatchFromMatchingAlgorithm_NoBillingConfigurationForAutomation()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfiguration = scope.DefaultBillingConfiguration;
        billingConfiguration.IncludeForAutomation = false;
        scope.ConfigureMatchPredicateManagerNoIncludeForAutomationMock(billingConfiguration);
        // act
        var results = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(scope.DefaultTruckTicket);

        //// assert
        Assert.IsTrue(results.Count(x => x.IncludeForAutomation) == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_GetMatchingBillingConfigurations_GetRankAndMatchFromMatchingAlgorithm_NoMatchingBillingConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfiguration = scope.DefaultBillingConfiguration;
        billingConfiguration.IncludeForAutomation = false;
        // act
        var results = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(scope.DefaultTruckTicket);

        //// assert
        Assert.IsTrue(results.Count(x => x.IncludeForAutomation) == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task TruckTicketManager_GetMatchingBillingConfigurations_GetRankAndMatchFromMatchingAlgorithm_BillingConfigurationForAutomationExist()
    {
        // arrange
        var scope = new DefaultScope();
        var billingConfiguration = scope.DefaultBillingConfiguration;
        billingConfiguration.IncludeForAutomation = true;
        scope.ConfigureMatchPredicateManagerMock(billingConfiguration);
        // act
        var results = await scope.InstanceUnderTest.GetMatchingBillingConfigurations(scope.DefaultTruckTicket);

        //// assert
        Assert.IsTrue(results.Count(x => x.IncludeForAutomation) > 0);
    }

    private class DefaultScope : TestScope<TruckTicketManager>
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
            Status = TruckTicketStatus.Hold,
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
            FacilityProviderMock.Setup(x => x.GetById(It.IsAny<Guid>(),
                                                      It.IsAny<bool>(),
                                                      It.IsAny<bool>(),
                                                      It.IsAny<bool>())).ReturnsAsync((Guid id, bool _, bool __, bool ___) => GenerateFacilityEntity().Where(entity => entity.Id == id)
                                                                                         .FirstOrDefault(new FacilityEntity
                                                                                          {
                                                                                              SiteId = null,
                                                                                              Name = null,
                                                                                              LegalEntity = null,
                                                                                          }));

            InstanceUnderTest = new(LoggerMock.Object,
                                    TruckTicketProviderMock.Object,
                                    SequenceNumberGeneratorMock.Object,
                                    FacilityProviderMock.Object,
                                    TruckTicketUploadBlobStorageMock.Object,
                                    MatchPredicateManagerMock.Object,
                                    MatchPredicateRankManagerMock.Object,
                                    NotesProviderMock.Object,
                                    NoteManagerMock.Object,
                                    SalesLinesManagerMock.Object,
                                    TruckTicketClassificationUsageEntityMock.Object,
                                    SourceLocationEntityProviderMock.Object,
                                    TruckTicketValidationManagerMock.Object,
                                    TruckTicketWorkflowManagerMock.Object);

            SetSequenceGeneratorManager();
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IProvider<Guid, TruckTicketEntity>> TruckTicketProviderMock { get; } = new();

        public Mock<ITruckTicketUploadBlobStorage> TruckTicketUploadBlobStorageMock { get; } = new();

        public Mock<IValidationManager<TruckTicketEntity>> TruckTicketValidationManagerMock { get; } = new();

        public Mock<IWorkflowManager<TruckTicketEntity>> TruckTicketWorkflowManagerMock { get; } = new();

        public Mock<IMatchPredicateManager> MatchPredicateManagerMock { get; } = new();

        public Mock<IMatchPredicateRankManager> MatchPredicateRankManagerMock { get; } = new();

        public Mock<ISequenceNumberGenerator> SequenceNumberGeneratorMock { get; } = new();

        public Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock { get; } = new();

        public Mock<IProvider<Guid, NoteEntity>> NotesProviderMock { get; } = new();

        public Mock<IManager<Guid, NoteEntity>> NoteManagerMock { get; } = new();

        public Mock<ISalesLineManager> SalesLinesManagerMock { get; } = new();

        public Mock<IManager<Guid, TruckTicketWellClassificationUsageEntity>> TruckTicketClassificationUsageEntityMock { get; } = new();

        public Mock<IProvider<Guid, SourceLocationEntity>> SourceLocationEntityProviderMock { get; } = new();

        public List<FacilityEntity> GenerateFacilityEntity()
        {
            return new()
            {
                new()
                {
                    Id = Guid.Parse("ae62f28e-fd0b-4594-b235-0e254bc4771a"),
                    SiteId = "TAFA",
                    Name = "Facility1",
                    Type = FacilityType.Lf,
                    LegalEntity = "Canada",
                },
                new()
                {
                    Id = Guid.Parse("42b89075-213e-48ab-9a21-b33e9bfc741d"),
                    SiteId = "DVFST",
                    Name = "Facility1",
                    Type = FacilityType.Fst,
                    LegalEntity = "Canada",
                },
            };
        }

        private IAsyncEnumerable<string> GenerateSequenceNumber()
        {
            return new List<string>
            {
                "TAFA-10001-LF",
                "TAFA-10002-LF",
                "TAFA-10003-LF",
                "TAFA-10004-LF",
            }.ToAsyncEnumerable();
        }

        public void SetSequenceGeneratorManager()
        {
            ConfigureSequenceGeneratorManagerMock(SequenceNumberGeneratorMock);
        }

        private void ConfigureSequenceGeneratorManagerMock(Mock<ISequenceNumberGenerator> mock)
        {
            mock.Setup(sequence => sequence.GenerateSequenceNumbers(It.IsAny<string>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<int>(),
                                                                    It.IsAny<string>(),
                                                                    It.IsAny<string>())).Returns(GenerateSequenceNumber());
        }

        public void ConfigureMatchPredicateManagerMock(params BillingConfigurationEntity[] billingConfigurationEntities)
        {
            MatchPredicateManagerMock.Setup(sequence => sequence.GetBillingConfigurations(It.IsAny<TruckTicketEntity>()))
                                     .ReturnsAsync(billingConfigurationEntities.ToList());

            MatchPredicateManagerMock.Setup(sequence => sequence.GetMatchingBillingConfiguration(It.IsAny<List<BillingConfigurationEntity>>(), It.IsAny<TruckTicketEntity>()))
                                     .ReturnsAsync(billingConfigurationEntities.First);

            MatchPredicateManagerMock.Setup(sequence => sequence.SelectAutomatedBillingConfiguration(It.IsAny<List<BillingConfigurationEntity>>(), It.IsAny<TruckTicketEntity>()))
                                     .Returns(billingConfigurationEntities.First);
        }

        public void ConfigureMatchPredicateManagerNoIncludeForAutomationMock(params BillingConfigurationEntity[] billingConfigurationEntities)
        {
            MatchPredicateManagerMock.Setup(sequence => sequence.GetBillingConfigurations(It.IsAny<TruckTicketEntity>()))
                                     .ReturnsAsync(billingConfigurationEntities.ToList());

            MatchPredicateManagerMock.Setup(sequence => sequence.GetMatchingBillingConfiguration(It.IsAny<List<BillingConfigurationEntity>>(), It.IsAny<TruckTicketEntity>()))
                                     .ReturnsAsync(billingConfigurationEntities.First);

            MatchPredicateManagerMock.Setup(sequence => sequence.SelectAutomatedBillingConfiguration(It.IsAny<List<BillingConfigurationEntity>>(), It.IsAny<TruckTicketEntity>()))
                                     .Returns(billingConfigurationEntities.First);
        }

        public void ConfigureMatchPredicateRankManagerMock(int predicateMatch = 0, int predicateWeight = 0)

        {
            MatchPredicateRankManagerMock.Setup(sequence => sequence.Evaluate(It.IsAny<TruckTicketEntity>(), It.IsAny<MatchPredicateEntity>()))
                                         .Returns((matches: predicateMatch, weight: predicateWeight));
        }
    }
}
