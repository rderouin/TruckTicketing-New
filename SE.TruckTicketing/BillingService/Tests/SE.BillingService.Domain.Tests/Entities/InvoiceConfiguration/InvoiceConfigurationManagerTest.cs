using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;
using Trident.Testing.TestScopes;
using Trident.Validation;
using Trident.Workflow;

namespace SE.BillingService.Domain.Tests.Entities.InvoiceConfiguration;

[TestClass]
public class InvoiceConfigurationManagerTest
{
    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_DefaultBillingConfiguration_NoMatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        billingConfiguration.MatchCriteria = new();
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_NoInvalidBillingConfiguration()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.WellClassifications.List.AddRange(new List<string>
        {
            WellClassifications.Drilling.ToString(),
            WellClassifications.Completions.ToString(),
        });

        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count == 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnSourceLocation_InvalidMatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitBySourceLocation = true;
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { scope.DefaultBillingConfiguration.Clone() } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnServiceType_InvalidMatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitByServiceType = true;
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { scope.DefaultBillingConfiguration.Clone() } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnWellClassification_InvalidMatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitByWellClassification = true;
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { scope.DefaultBillingConfiguration.Clone() } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnSubstance_InvalidMatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitBySubstance = true;
        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { scope.DefaultBillingConfiguration.Clone() } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnSourceLocation_InvalidValueSelectedForSourceLocation_MatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitBySourceLocation = true;
        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        billingConfiguration.MatchCriteria = new()
        {
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
                SourceLocationId = Guid.NewGuid(),
                SourceLocationValueState = MatchPredicateValueState.Value,
                SubstanceId = null,
                SubstanceName = null,
                SubstanceValueState = MatchPredicateValueState.Any,
                WellClassification = WellClassifications.Completions,
                WellClassificationState = MatchPredicateValueState.Value,
                StartDate = null,
                EndDate = null,
                Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
            },
        };

        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnSourceLocation_InvalidValueSelectedForSubstance_MatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitBySubstance = true;
        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        billingConfiguration.MatchCriteria = new()
        {
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
                SubstanceId = Guid.NewGuid(),
                SubstanceName = null,
                SubstanceValueState = MatchPredicateValueState.Value,
                WellClassification = WellClassifications.Completions,
                WellClassificationState = MatchPredicateValueState.Value,
                StartDate = null,
                EndDate = null,
                Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
            },
        };

        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnSourceLocation_InvalidValueSelectedForWellClassification_MatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitByWellClassification = true;
        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        billingConfiguration.MatchCriteria = new()
        {
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
        };

        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    [TestMethod]
    [TestCategory("Unit")]
    public async Task InvoiceConfigurationManager_InvoiceConfigurationSplitOnServiceType_InvalidValueSelectedForServiceType_MatchPredicate()
    {
        // arrange
        var scope = new DefaultScope();
        var invoiceConfiguration = scope.GenerateInvoiceConfiguration().First().Clone();
        invoiceConfiguration.IsSplitByServiceType = true;
        var billingConfiguration = scope.DefaultBillingConfiguration.Clone();
        billingConfiguration.MatchCriteria = new()
        {
            new()
            {
                Id = Guid.NewGuid(),
                Stream = Stream.Pipeline,
                StreamValueState = MatchPredicateValueState.Value,
                IsEnabled = true,
                ServiceType = null,
                ServiceTypeId = Guid.NewGuid(),
                ServiceTypeValueState = MatchPredicateValueState.Value,
                SourceIdentifier = null,
                SourceLocationId = null,
                SourceLocationValueState = MatchPredicateValueState.Any,
                SubstanceId = null,
                SubstanceName = null,
                SubstanceValueState = MatchPredicateValueState.Any,
                WellClassification = WellClassifications.Undefined,
                WellClassificationState = MatchPredicateValueState.Any,
                StartDate = null,
                EndDate = null,
                Hash = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
            },
        };

        var results = new SearchResults<BillingConfigurationEntity, SearchCriteria> { Results = new List<BillingConfigurationEntity> { billingConfiguration } };

        scope.BillingConfigurationManagerMockMock.Setup(x => x.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
             .ReturnsAsync(results);

        // act
        var invalidBillingConfigurations = await scope.InstanceUnderTest.ValidateBillingConfiguration(invoiceConfiguration);

        //// assert
        Assert.IsTrue(invalidBillingConfigurations.Count > 0);
    }

    private class DefaultScope : TestScope<InvoiceConfigurationManager>
    {
        public readonly BillingConfigurationEntity DefaultBillingConfiguration =
            new()
            {
                Id = Guid.NewGuid(),
                InvoiceConfigurationId = Guid.Parse("6e09ae37-bff6-4c99-9c7e-f9491e811b40"),
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

        public DefaultScope()
        {
            InstanceUnderTest = new(LoggerMock.Object,
                                    InvoiceConfigurationProviderMock.Object,
                                    BillingConfigurationManagerMockMock.Object,
                                    MapperMock.Object,
                                    InvoiceConfigurationValidationManagerMock.Object,
                                    InvoiceConfigurationWorkflowManagerMock.Object);
        }

        public Mock<ILog> LoggerMock { get; } = new();

        public Mock<IMapperRegistry> MapperMock { get; } = new();

        public Mock<IProvider<Guid, InvoiceConfigurationEntity>> InvoiceConfigurationProviderMock { get; } = new();

        public Mock<IManager<Guid, BillingConfigurationEntity>> BillingConfigurationManagerMockMock { get; } = new();

        public Mock<IValidationManager<InvoiceConfigurationEntity>> InvoiceConfigurationValidationManagerMock { get; } = new();

        public Mock<IWorkflowManager<InvoiceConfigurationEntity>> InvoiceConfigurationWorkflowManagerMock { get; } = new();

        public List<InvoiceConfigurationEntity> GenerateInvoiceConfiguration()
        {
            return new()
            {
                new()
                {
                    Id = Guid.Parse("6e09ae37-bff6-4c99-9c7e-f9491e811b40"),
                    BusinessUnitId = "AA-100050",
                    AllFacilities = false,
                    AllServiceTypes = false,
                    AllSourceLocations = false,
                    AllSubstances = false,
                    AllWellClassifications = false,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Generator/Customer 01",
                    Description = "This is test invoice configuration",
                    IncludeInternalDocumentAttachment = true,
                    IncludeExternalDocumentAttachment = true,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Petro Canada",
                    Facilities = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    ServiceTypes = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    SourceLocationIdentifier = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Id-001",
                            "Id-002",
                        },
                    },
                    SourceLocations = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { "Drilling" },
                    },
                    PermutationsHash = "5537093D2EA52B432228E072824E0CE0DE63986C44638429A85B29411AF43810",
                    Permutations = new()
                    {
                        new()
                        {
                            Name = "TT Petro Canada",
                            SourceLocation = "Test SourceLocation A",
                            ServiceType = "Test ServiceType A",
                            WellClassification = "All",
                            Substance = "All",
                            Facility = "Facility A",
                        },
                    },
                    SubstancesName = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Substance01",
                            "Substance02",
                        },
                    },
                    ServiceTypesName = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            "Service01",
                            "Service02",
                        },
                    },
                    Substances = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new()
                        {
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                        },
                    },
                    CreatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                    UpdatedAt = new DateTime(2025, 1, 17, 18, 11, 30),
                    CreatedBy = "Panth Shah",
                    UpdatedBy = "Panth Shah",
                    CreatedById = Guid.NewGuid().ToString(),
                    UpdatedById = Guid.NewGuid().ToString(),
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AllFacilities = true,
                    AllServiceTypes = true,
                    AllSourceLocations = true,
                    AllSubstances = true,
                    AllWellClassifications = false,
                    CatchAll = false,
                    CustomerId = Guid.NewGuid(),
                    CustomerName = "QQ Customer 555",
                    Description = "New Invoice Configuration added with new customer",
                    IncludeInternalDocumentAttachment = false,
                    IncludeExternalDocumentAttachment = false,
                    IsSplitByFacility = false,
                    IsSplitByServiceType = false,
                    IsSplitBySourceLocation = false,
                    IsSplitBySubstance = false,
                    IsSplitByWellClassification = false,
                    Name = "TT Shell Corp",
                    Facilities = null,
                    ServiceTypes = null,
                    SourceLocationIdentifier = null,
                    SourceLocations = null,
                    SplitEdiFieldDefinitions = null,
                    SplittingCategories = null,
                    WellClassifications = new()
                    {
                        Key = Guid.NewGuid(),
                        List = new() { "Drilling" },
                    },
                    SubstancesName = null,
                    ServiceTypesName = null,
                    Substances = null,
                },
            };
        }
    }
}
