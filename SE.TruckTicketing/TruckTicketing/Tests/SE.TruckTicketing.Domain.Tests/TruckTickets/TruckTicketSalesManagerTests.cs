using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.LoadConfirmation;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Tests.TestUtilities;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.SalesLine;
using SE.TruckTicketing.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Contracts;
using Trident.Data.Contracts;

using Xunit;

namespace SE.TruckTicketing.Domain.Tests.TruckTickets;

public class TruckTicketSalesManagerTests
{
    private readonly Mock<IProvider<Guid, AccountEntity>> _accountProviderMock;

    private readonly Mock<IProvider<Guid, BillingConfigurationEntity>> _billingConfigurationProviderMock;

    private readonly Mock<ITruckTicketInvoiceService> _invoiceServiceMock;

    private readonly Mock<ITruckTicketLoadConfirmationService> _loadConfirmationServiceMock;

    private readonly Mock<ILogger<TruckTicketSalesManager>> _loggerMock;

    private readonly Mock<IManager<Guid, SalesLineEntity>> _salesLineManagerMock;

    private readonly Mock<ISalesLinesPublisher> _salesLinesPublisherMock;

    private readonly Mock<IManager<Guid, TruckTicketEntity>> _truckTicketManagerMock;

    private readonly TruckTicketSalesManager _truckTicketSalesManager;

    private readonly Mock<IProvider<Guid, MaterialApprovalEntity>> _materialApprovalManagerMock;

    public TruckTicketSalesManagerTests()
    {
        _truckTicketManagerMock = new();
        _salesLineManagerMock = new();
        _billingConfigurationProviderMock = new();
        _accountProviderMock = new();
        _salesLinesPublisherMock = new();
        _invoiceServiceMock = new();
        _loadConfirmationServiceMock = new();
        _loggerMock = new();
        _materialApprovalManagerMock = new();

        _truckTicketSalesManager = new(_truckTicketManagerMock.Object,
                                       _salesLineManagerMock.Object,
                                       _billingConfigurationProviderMock.Object,
                                       _accountProviderMock.Object,
                                       _salesLinesPublisherMock.Object,
                                       _invoiceServiceMock.Object,
                                       _loadConfirmationServiceMock.Object,
                                       _loggerMock.Object,
                                       _materialApprovalManagerMock.Object);
    }

    
    [Fact]
    public async Task PersistTruckTicketAndSalesLines_UpdatesLastTransactionDate_WhenTicketIsValid()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity
        {
            BillingCustomerId = Guid.NewGuid(),
            TicketNumber = "LGFST1",
        };

        var account = new AccountEntity { Id = truckTicket.BillingCustomerId };
        var salesLines = new List<SalesLineEntity> { new() };

        _accountProviderMock.SetupEntities(new[] { account });

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        _accountProviderMock.Verify(x => x.Update(It.Is<AccountEntity>(acc => account.Id == acc.Id), true), Times.Once);
        account.LastTransactionDate.Should().BeWithin(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PersistTruckTicketAndSalesLines_TransitionsTicketToOpenStatus_WhenStatusIsNew()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.New };
        var salesLines = new List<SalesLineEntity>();

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.Equal(TruckTicketStatus.Open, truckTicket.Status);
    }

    [Fact]
    public async Task PersistTruckTicketAndSalesLines_TransitionsTicketToOpenStatus_WhenStatusIsStub()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.Stub };
        var salesLines = new List<SalesLineEntity>();

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.Equal(TruckTicketStatus.Open, truckTicket.Status);
    }

    [Fact]
    public async Task PersistTruckTicketAndSalesLines_DoesNotTransitionTicketToOpenStatus_WhenStatusIsNotNewOrStub()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity { Status = TruckTicketStatus.Approved };
        var salesLines = new List<SalesLineEntity>();

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.NotEqual(TruckTicketStatus.Open, truckTicket.Status);
    }

    [Fact]
    public async Task PersistTruckTicketAndSalesLines_SanitizesSalesLineValues()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity { TicketNumber = "LGFST1" };
        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                Id = Guid.Empty,
                Rate = 1,
                Quantity = 1.1111,
                TotalValue = 1.1111,
            },
        };

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.NotEqual(Guid.Empty, salesLines[0].Id);
        Assert.Equal(1.11, salesLines[0].Quantity);
        Assert.Equal(1.11, salesLines[0].TotalValue);
    }

    [Fact]
    public async Task PersistTruckTicketAndSalesLines_SanitizesSalesLineValues_Floating_Point_Quirks()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity { TicketNumber = "LGFST1" };
        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                Id = Guid.Empty,
                Rate = 1,
                Quantity = 8.245,
                TotalValue = 1.25,
            },
        };

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.NotEqual(Guid.Empty, salesLines[0].Id);
        Assert.Equal(8.25, salesLines[0].TotalValue);
    }

    [Fact]
    public async Task TryApproveSalesLines_ApprovesSalesLines_WhenTicketIsJustApproved()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Status = TruckTicketStatus.Approved,
            TicketNumber = "LGFST1",
        };

        var existingTruckTicket = new TruckTicketEntity
        {
            Id = truckTicket.Id,
            Status = TruckTicketStatus.Open,
        };

        var salesLines = new List<SalesLineEntity> { new() };

        _truckTicketManagerMock.SetupEntities(new[] { existingTruckTicket });

        _invoiceServiceMock
           .Setup(x => x.GetTruckTicketInvoice(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new InvoiceEntity());

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.Equal(SalesLineStatus.Approved, salesLines[0].Status);
    }

    [Fact]
    public async Task TryApproveSalesLines_SyncsEffectiveDatesForTicketAndSalesLine_WhenTicketIsJustApproved()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Status = TruckTicketStatus.Approved,
            TicketNumber = "LGFST1",
            EffectiveDate = DateTime.Today.Subtract(TimeSpan.FromDays(1)),
        };

        var existingTruckTicket = new TruckTicketEntity
        {
            Id = truckTicket.Id,
            Status = TruckTicketStatus.Open,
        };

        var salesLines = new List<SalesLineEntity>
        {
            new()
            {
                TruckTicketEffectiveDate = DateTime.Today,
            },
        };

        _truckTicketManagerMock.SetupEntities(new[] { existingTruckTicket });

        _invoiceServiceMock
           .Setup(x => x.GetTruckTicketInvoice(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new InvoiceEntity());

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.Equal(truckTicket.EffectiveDate, salesLines[0].TruckTicketEffectiveDate);
    }

    [Fact]
    public async Task TryApproveSalesLines_DoesNotApproveSalesLines_WhenTicketIsNotJustApproved()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Status = TruckTicketStatus.Open,
            TicketNumber = "LGFST1",
        };

        var existingTruckTicket = new TruckTicketEntity
        {
            Id = truckTicket.Id,
            Status = TruckTicketStatus.Open,
        };

        var salesLines = new List<SalesLineEntity> { new() };

        _truckTicketManagerMock.SetupEntities(new[] { existingTruckTicket });

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.NotEqual(SalesLineStatus.Approved, salesLines[0].Status);
    }

    [Fact]
    public async Task AssignLoadConfirmation_AssignsLoadConfirmationToSalesLines_WhenLoadConfirmationIsNotNull()
    {
        // Arrange
        var newApprovedTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Status = TruckTicketStatus.Approved,
            TicketNumber = "LGFST1",
        };

        var existingOpenTicket = new TruckTicketEntity
        {
            Id = newApprovedTicket.Id,
            Status = TruckTicketStatus.Open,
        };

        var salesLines = new List<SalesLineEntity> { new() };

        _truckTicketManagerMock.SetupEntities(new[] { existingOpenTicket });

        _loadConfirmationServiceMock
           .Setup(x => x.GetTruckTicketLoadConfirmation(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<InvoiceEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new LoadConfirmationEntity
            {
                Id = Guid.NewGuid(),
                Number = "LC123",
            });

        _invoiceServiceMock
           .Setup(x => x.GetTruckTicketInvoice(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new InvoiceEntity());

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(newApprovedTicket, salesLines);

        // Assert
        Assert.NotEqual(Guid.Empty, salesLines[0].LoadConfirmationId);
        Assert.Equal("LC123", salesLines[0].LoadConfirmationNumber);
    }

    [Fact]
    public async Task AssignInvoice_AssignsInvoiceToSalesLines()
    {
        // Arrange
        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            Status = TruckTicketStatus.Approved,
            TicketNumber = "LGFST1",
        };

        var existingTruckTicket = new TruckTicketEntity
        {
            Id = truckTicket.Id,
            Status = TruckTicketStatus.Open,
        };

        var salesLines = new List<SalesLineEntity> { new() };

        _truckTicketManagerMock.SetupEntities(new[] { existingTruckTicket });

        _loadConfirmationServiceMock
           .Setup(x => x.GetTruckTicketLoadConfirmation(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<InvoiceEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new LoadConfirmationEntity
            {
                Id = Guid.NewGuid(),
                Number = "LC123",
            });

        _invoiceServiceMock
           .Setup(x => x.GetTruckTicketInvoice(It.IsAny<TruckTicketEntity>(), It.IsAny<BillingConfigurationEntity>(), It.IsAny<int>(), It.IsAny<double>()))
           .ReturnsAsync(new InvoiceEntity
            {
                ProformaInvoiceNumber = "INV123",
            });

        // Act
        await _truckTicketSalesManager.PersistTruckTicketAndSalesLines(truckTicket, salesLines);

        // Assert
        Assert.Equal("INV123", salesLines[0].ProformaInvoiceNumber);
    }

    [Fact]
    public void LoadSummaryReportConditions_MeetsConditions()
    {
        //setup
        var requestedStartDate = new DateTime(2022, 12, 31);
        var requestedEndDate = new DateTime(2023, 01, 02);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = new DateTimeOffset(2023, 01, 01, 1, 1, 1, TimeSpan.FromHours(7)),
            NetWeight = 0.1,
            Status = TruckTicketStatus.Approved
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.True(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_DoesMeetConditionsRequestStartDate()
    {
        //setup
        var requestedStartDate = new DateTime(2023, 01, 02);
        var requestedEndDate = new DateTime(2023, 01, 02);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = new DateTimeOffset(2023, 01, 01, 1, 1, 1, TimeSpan.FromHours(7)),
            NetWeight = 0.1,
            Status = TruckTicketStatus.Approved
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.False(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_DoesMeetConditionsRequestEndDate()
    {
        //setup
        var requestedStartDate = new DateTime(2022, 12, 31);
        var requestedEndDate = new DateTime(2022, 12, 31);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = new DateTimeOffset(2023, 01, 01, 1, 1, 1, TimeSpan.FromHours(7)),
            NetWeight = 0.1,
            Status = TruckTicketStatus.Approved
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.False(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_DoesMeetConditionsStatusIsVoid()
    {
        var requestedStartDate = new DateTime(2022, 12, 31);
        var requestedEndDate = new DateTime(2023, 01, 01);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = new DateTimeOffset(2023, 01, 01, 1, 1, 1, TimeSpan.FromHours(7)),
            NetWeight = 0.1,
            Status = TruckTicketStatus.Void
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.False(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_DoesMeetConditionsStatusHasNoNetWeight()
    {
        var requestedStartDate = new DateTime(2022, 12, 31);
        var requestedEndDate = new DateTime(2023, 01, 01);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = new DateTimeOffset(2023, 01, 01, 1, 1, 1, TimeSpan.FromHours(7)),
            NetWeight = 0,
            Status = TruckTicketStatus.Invoiced
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.False(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_DoesMeetConditionsStatusHasNoNotTimeOut()
    {
        var requestedStartDate = new DateTime(2022, 12, 31);
        var requestedEndDate = new DateTime(2023, 01, 01);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01),
            TimeOut = null,
            NetWeight = 0.01,
            Status = TruckTicketStatus.Invoiced
        };

        //execute
        var result = truckTicket.MeetsReportConditions(requestedStartDate, requestedEndDate);

        //assert
        Assert.False(result, "All conditions should Meet report requirements");
    }

    [Fact]
    public void LoadSummaryReportConditions_StartDateIsEqualToEffectiveDate()
    {
        //setup
        DateTime loadSummaryRequestStartDate = new DateTime(2023, 01, 01);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2023, 01, 01)
        };

        //execute

        var actualResult = truckTicket.EffectiveDateIsGreaterThanOrEqualTo(loadSummaryRequestStartDate);

        //assert
        Assert.True(actualResult, "Start Date should be less than or equal to Effective Date");
    }

    [Fact]
    public void LoadSummaryReportConditions_StartDateIsGreaterThanEffectiveDate()
    {
        //setup
        DateTime loadSummaryRequestStartDate = new DateTime(2023, 01, 01);

        var truckTicket = new TruckTicketEntity
        {
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2022, 12, 31)
        };

        //execute

        var actualResult = truckTicket.EffectiveDateIsGreaterThanOrEqualTo(loadSummaryRequestStartDate);

        //assert
        Assert.False(actualResult, "Start Date should be less than or equal to Effective Date");
    }
}
