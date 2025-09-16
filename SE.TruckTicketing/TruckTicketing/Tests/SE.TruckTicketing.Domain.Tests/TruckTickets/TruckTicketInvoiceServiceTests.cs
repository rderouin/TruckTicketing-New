using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.EDIFieldDefinition;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.LegalEntity;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Testing.TestScopes;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

[TestClass]
public class TruckTicketInvoiceServiceTests
{
    [TestMethod]
    public async Task Service_CreatesInvoice_InUnPostedStatus()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.Status.Should().Be(InvoiceStatus.UnPosted);
    }

    [TestMethod]
    public async Task Service_CreatesInvoice_WithNormalDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.InvoiceStartDate.Day.Should().Be(1);
        invoice.InvoiceStartDate.Month.Should().Be(date.Month);
        invoice.InvoiceStartDate.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_CreatesInvoice_WithCurrentMonthStartDate_WhenTicketLoadDateIsAfter7Am()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 1, 7, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.InvoiceStartDate.Day.Should().Be(1);
        invoice.InvoiceStartDate.Month.Should().Be(date.Month);
        invoice.InvoiceStartDate.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_CreatesInvoice_WithNoEndDate_WhenEndOfJobInvoicingIsEnabled()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var materialApproval = GenFu.GenFu.New<MaterialApprovalEntity>();
        materialApproval.EnableEndOfJobInvoicing = true;
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
            MaterialApprovalId = materialApproval.Id,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });
        scope.MaterialApprovalProviderMock.SetupEntities(new[] { materialApproval });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.InvoiceStartDate.Day.Should().Be(1);
        invoice.InvoiceStartDate.Month.Should().Be(date.Month);
        invoice.InvoiceStartDate.Year.Should().Be(date.Year);
        invoice.InvoiceEndDate.Should().BeNull();
    }

    [TestMethod]
    public async Task Service_ReturnsExistingInvoice_IfOneIsFound_ThatEndsAfterTicketLoadDate()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });
        var existingInvoice = GenFu.GenFu.New<InvoiceEntity>();
        existingInvoice.Status = InvoiceStatus.UnPosted;
        existingInvoice.InvoicePermutationId = await scope.InstanceUnderTest.ComputeInvoicePermutationId(truckTicket, invoiceConfig);
        existingInvoice.FacilityId = truckTicket.FacilityId;
        existingInvoice.InvoiceConfigurationId = billingConfig.InvoiceConfigurationId;
        existingInvoice.IsReversed = false;
        existingInvoice.IsReversal = false;
        existingInvoice.InvoiceStartDate = new(2022, 4, 1, 0, 0, 0, default);
        existingInvoice.InvoiceEndDate = date.AddMonths(1).AddDays(-1);
        scope.InvoiceManagerMock.SetupEntities(new[] { existingInvoice });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.Should().BeEquivalentTo(existingInvoice);
    }

    [TestMethod]
    public async Task Service_CreatesInvoices_IfOneIsFound_ThatEndsBeforeTicketLoadDate()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });
        var existingInvoice = GenFu.GenFu.New<InvoiceEntity>();
        existingInvoice.InvoiceEndDate = date.AddDays(-1);
        scope.InvoiceManagerMock.SetupEntities(new[] { existingInvoice });

        // act
        var invoice = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        invoice.Should().NotBeEquivalentTo(existingInvoice);
    }

    [TestMethod]
    public async Task Service_CreateInvoice_Uses_CurrencyCode_When_It_Exists()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var invoiceConfig = GenFu.GenFu.New<InvoiceConfigurationEntity>();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.InvoiceConfigurationId = invoiceConfig.Id;
        scope.InvoiceConfigProviderMock.SetupEntities(new[] { invoiceConfig });
        var existingInvoice = GenFu.GenFu.New<InvoiceEntity>();
        existingInvoice.InvoiceEndDate = date.AddDays(-1);
        scope.InvoiceManagerMock.SetupEntities(new[] { existingInvoice });

        var accountEntity = GenFu.GenFu.New<AccountEntity>();
        accountEntity.CurrencyCode = CurrencyCode.CAD;
        accountEntity.Contacts = new();

        var legalEntity = GenFu.GenFu.New<LegalEntityEntity>();
        legalEntity.CountryCode = CountryCode.US;

        scope.AccountEntityProviderMock.Setup(s => s.GetById(It.IsAny<Guid>(),
                                                             It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
             .ReturnsAsync(accountEntity);

        scope.LegalEntityProviderMock.Setup(s => s.Get(It.IsAny<Expression<Func<LegalEntityEntity, bool>>>(), It.IsAny<Func<IQueryable<LegalEntityEntity>, IOrderedQueryable<LegalEntityEntity>>>(),
                                                       It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
             .ReturnsAsync(new[] { legalEntity });

        // act
        var result = await scope.InstanceUnderTest.GetTruckTicketInvoice(truckTicket, billingConfig);

        // assert
        result.Currency.Should().BeEquivalentTo("CAD");
    }

    private class DefaultScope : TestScope<TruckTicketInvoiceService>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(InvoiceManagerMock.Object,
                                    InvoiceConfigProviderMock.Object,
                                    MaterialApprovalProviderMock.Object,
                                    AccountEntityProviderMock.Object,
                                    EdiFieldDefintionProviderMock.Object,
                                    FacilityProviderMock.Object,
                                    LegalEntityProviderMock.Object);

            AccountEntityProviderMock.SetupEntities(GenFu.GenFu.ListOf<AccountEntity>());
            FacilityProviderMock.SetupEntities(GenFu.GenFu.ListOf<FacilityEntity>());
        }

        public Mock<IManager<Guid, InvoiceEntity>> InvoiceManagerMock { get; } = new();

        public Mock<IProvider<Guid, InvoiceConfigurationEntity>> InvoiceConfigProviderMock { get; } = new();

        public Mock<IProvider<Guid, MaterialApprovalEntity>> MaterialApprovalProviderMock { get; } = new();

        public Mock<IProvider<Guid, AccountEntity>> AccountEntityProviderMock { get; } = new();

        public Mock<IProvider<Guid, EDIFieldDefinitionEntity>> EdiFieldDefintionProviderMock { get; } = new();

        public Mock<IProvider<Guid, FacilityEntity>> FacilityProviderMock { get; } = new();

        public Mock<IProvider<Guid, LegalEntityEntity>> LegalEntityProviderMock { get; } = new();

        public void SetupInvoiceSearchResult(InvoiceEntity invoiceEntity)
        {
            FacilityProviderMock.Setup(x => x.GetById(It.IsAny<Guid>(), false, false, false))
                                .ReturnsAsync(GenFu.GenFu.New<FacilityEntity>());

            LegalEntityProviderMock.Setup(s => s.Get(It.IsAny<Expression<Func<LegalEntityEntity, bool>>>(), It.IsAny<Func<IQueryable<LegalEntityEntity>, IOrderedQueryable<LegalEntityEntity>>>(),
                                                     It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                                   .ReturnsAsync(new[] { GenFu.GenFu.New<LegalEntityEntity>() });
        }
    }
}

[TestClass]
public class TruckTicketLoadConfirmationServiceTests
{
    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalDailyDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 4, 2, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Daily;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(date.Day);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(date.Day);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);

        lc.EndDate.Value.Subtract(lc.StartDate).TotalHours.Should().BeGreaterThan(23);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalWeeklyDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 11, 24, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Weekly;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(20);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(26);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);

        lc.EndDate.Value.Subtract(lc.StartDate).TotalDays.Should().BeGreaterThan(6);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalWeeklyDateRange_WithFirstDayOfWeekAligningWithTicketDate()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 24, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Weekly;
        billingConfig.FirstDayOfTheWeek = DayOfWeek.Friday;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(24);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(30);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalMonthlyDateRange_WithTicketDateInFirstHalfOfTheMonth()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 5, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FirstDayOfTheMonth = 16;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(1);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(15);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalMonthlyDateRange_WithTicketDateInSecondHalfOfTheMonth()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 18, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FirstDayOfTheMonth = 16;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(16);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(31);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalMonthlyDateRange()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2022, 11, 24, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(1);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(30);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);

        lc.EndDate.Value.Subtract(lc.StartDate).TotalDays.Should().BeGreaterThan(29);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalWeeklyDateRange_With_EndDateClipped_ByInvoice()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 28, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Weekly;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(26);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(31);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_WithNormalWeeklyDateRange_With_StartDateClipped_ByInvoice()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Weekly;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.StartDate.Day.Should().Be(1);
        lc.StartDate.Month.Should().Be(date.Month);
        lc.StartDate.Year.Should().Be(date.Year);

        lc.EndDate.Value.Day.Should().Be(4);
        lc.EndDate.Value.Month.Should().Be(date.Month);
        lc.EndDate.Value.Year.Should().Be(date.Year);
    }

    [TestMethod]
    public async Task Service_Should_Select_ExistingLC_That_Is_Open()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = truckTicket.FacilityId;
        existingLc.IsReversed = false;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;
        existingLc.Generators = new();

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().Be(existingLc.Id);
    }

    [DataTestMethod]
    [DataRow(LoadConfirmationStatus.Posted)]
    [DataRow(LoadConfirmationStatus.Rejected)]
    [DataRow(LoadConfirmationStatus.Unknown)]
    [DataRow(LoadConfirmationStatus.Void)]
    [DataRow(LoadConfirmationStatus.PendingSignature)]
    [DataRow(LoadConfirmationStatus.SignatureVerified)]
    [DataRow(LoadConfirmationStatus.SubmittedToGateway)]
    [DataRow(LoadConfirmationStatus.WaitingForInvoice)]
    [DataRow(LoadConfirmationStatus.WaitingSignatureValidation)]
    public async Task Service_Should_Not_Select_ExistingLC_That_Is_Not_Open(LoadConfirmationStatus lcStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = lcStatus;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = truckTicket.FacilityId;
        existingLc.IsReversed = false;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().NotBe(existingLc.Id);
    }

    [DataTestMethod]
    [DataRow(InvoiceStatus.Posted)]
    [DataRow(InvoiceStatus.Unknown)]
    [DataRow(InvoiceStatus.Void)]
    [DataRow(InvoiceStatus.PaidPartial)]
    [DataRow(InvoiceStatus.PaidSettled)]
    [DataRow(InvoiceStatus.PostedRejected)]
    [DataRow(InvoiceStatus.AgingUnSent)]
    [DataRow(InvoiceStatus.PaidUnSettled)]
    public async Task Service_Should_Not_Select_ExistingLC_That_Is_Not_UnPosted(InvoiceStatus invoiceStatus)
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = invoiceStatus;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = truckTicket.FacilityId;
        existingLc.IsReversed = false;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().NotBe(existingLc.Id);
    }

    [TestMethod]
    public async Task Service_Should_Not_Select_ExistingLC_That_Is_Reversed()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = truckTicket.FacilityId;
        existingLc.IsReversed = true;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;
        existingLc.Generators = new();

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().Be(Guid.Empty);
    }

    [TestMethod]
    public async Task Service_Should_Not_Select_ExistingLC_That_Is_Reversal()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = truckTicket.FacilityId;
        existingLc.IsReversed = false;
        existingLc.IsReversal = true;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;
        existingLc.Generators = new();

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().Be(Guid.Empty);
    }

    [TestMethod]
    public async Task Service_Should_Not_Select_ExistingLC_That_Has_Different_FacilityId()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = Guid.NewGuid();
        existingLc.IsReversed = false;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;
        existingLc.Generators = new();

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().Be(Guid.Empty);
    }

    [TestMethod]
    public async Task Service_Should_Not_Select_ExistingLC_That_Is_Outside_Date_Range()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            EffectiveDate = date.Date,
        };

        truckTicket.FacilityId = Guid.NewGuid();
        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Monthly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.LoadConfirmationBatch;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.Status = InvoiceStatus.UnPosted;
        invoice.InvoiceStartDate = new(2023, 1, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 1, 31, 0, 0, 0, default);

        var existingLc = GenFu.GenFu.New<LoadConfirmationEntity>();
        existingLc.BillingConfigurationId = billingConfig.Id;
        existingLc.Status = LoadConfirmationStatus.Open;
        existingLc.InvoiceStatus = invoice.Status;
        existingLc.InvoicePermutationId = invoice.InvoicePermutationId;
        existingLc.FacilityId = Guid.NewGuid();
        existingLc.IsReversed = false;
        existingLc.IsReversal = false;
        existingLc.Frequency = billingConfig.LoadConfirmationFrequency.ToString();
        existingLc.StartDate = invoice.InvoiceStartDate;
        existingLc.EndDate = invoice.InvoiceEndDate;
        existingLc.Generators = new();

        scope.LoadConfirmationManagerMock.SetupEntities(new[] { existingLc }.Concat(GenFu.GenFu.ListOf<LoadConfirmationEntity>()));

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Id.Should().Be(Guid.Empty);
    }

    [TestMethod]
    public async Task Service_Creates_NewLC_With_TicketNumber_As_LcNumber_For_TicketByTicket_Delivery()
    {
        // arrange
        var scope = new DefaultScope();
        var date = new DateTimeOffset(2023, 3, 3, 13, 0, 0, default);
        var truckTicket = new TruckTicketEntity
        {
            LoadDate = date,
            TicketNumber = "BCFST293038-SP",
        };

        var billingConfig = GenFu.GenFu.New<BillingConfigurationEntity>();
        billingConfig.LoadConfirmationsEnabled = true;
        billingConfig.LoadConfirmationFrequency = LoadConfirmationFrequency.Weekly;
        billingConfig.FieldTicketDeliveryMethod = FieldTicketDeliveryMethod.TicketByTicket;
        var invoice = GenFu.GenFu.New<InvoiceEntity>();
        invoice.InvoiceStartDate = new(2023, 3, 1, 0, 0, 0, default);
        invoice.InvoiceEndDate = new DateTimeOffset(2023, 3, 31, 0, 0, 0, default);

        // act
        var lc = await scope.InstanceUnderTest.GetTruckTicketLoadConfirmation(truckTicket, billingConfig, invoice);

        // assert
        lc.Number.Should().Be(truckTicket.TicketNumber);
        lc.Number.Should().NotBe(default);
    }

    private class DefaultScope : TestScope<TruckTicketLoadConfirmationService>
    {
        public DefaultScope()
        {
            InstanceUnderTest = new(LoadConfirmationManagerMock.Object,
                                    AccountProviderMock.Object,
                                    MaterialApprovalProviderMock.Object,
                                    LegalEntityProviderMock.Object);

            SetupSequenceNumberGenerator();

            AccountProviderMock.Setup(s => s.GetById(It.IsAny<Guid>(),
                                                     It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                               .ReturnsAsync(GenFu.GenFu.New<AccountEntity>());
        }

        public Mock<IManager<Guid, LoadConfirmationEntity>> LoadConfirmationManagerMock { get; } = new();

        public Mock<IProvider<Guid, AccountEntity>> AccountProviderMock { get; } = new();

        public Mock<IProvider<Guid, MaterialApprovalEntity>> MaterialApprovalProviderMock { get; } = new();

        public Mock<IProvider<Guid, LegalEntityEntity>> LegalEntityProviderMock { get; } = new();

        public Mock<ISequenceNumberGenerator> SequenceNumberGeneratorMock { get; } = new();

        public void SetupSequenceNumberGenerator()
        {
            SequenceNumberGeneratorMock.Setup(generator => generator.GenerateSequenceNumbers(It.IsAny<string>(),
                                                                                             It.IsAny<string>(),
                                                                                             It.IsAny<int>(),
                                                                                             It.IsAny<string>(),
                                                                                             It.IsAny<string>()))
                                       .Returns(new[] { "ABCDE10001001-SO" }.ToAsyncEnumerable());
        }
    }
}
