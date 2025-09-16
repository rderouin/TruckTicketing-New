using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TridentContrib.Extensions.Azure.ServiceBus.ReEnqueue;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Contracts.Configuration;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class LowLatencyTruckTicketAttachmentQueueProcessorTests
{
    private static readonly JsonSerializerOptions EventSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private Mock<IAppSettings> _appSettingsMock;

    private Mock<BindingContext> _bindingContext = null!;

    private Dictionary<string, object> _bindingData = null!;

    private Mock<FunctionContext> _functionContext = null!;

    private Mock<IIntegrationsServiceBus> _integrationsServiceBusMock;

    private Mock<ILeaseObjectBlobStorage> _leaseObjectBlobStorage;

    private Mock<ILogger<LowLatencyTruckTicketAttachmentQueueProcessor>> _logger;

    private LowLatencyTruckTicketAttachmentQueueProcessor _processor;

    private IServiceBusMessageEnqueuer _serviceBusMessageReEnqueuer;

    private Mock<ITruckTicketManager> _truckTicketManager;

    [TestInitialize]
    public void Initialize()
    {
        _truckTicketManager = new();
        _logger = new();
        _leaseObjectBlobStorage = new();

        _integrationsServiceBusMock = new();
        _appSettingsMock = new();
        _serviceBusMessageReEnqueuer = new ServiceBusMessageEnqueuer(new ServiceBusReEnqueueStrategyFactory());

        _functionContext = new();
        _bindingContext = new();
        _functionContext.Setup(c => c.BindingContext)!.Returns(_bindingContext.Object);
        _bindingData = new();
        _bindingContext.Setup(c => c.BindingData)!.Returns(_bindingData);

        _processor = new(_truckTicketManager.Object, _logger.Object, _leaseObjectBlobStorage.Object, _integrationsServiceBusMock.Object, _appSettingsMock.Object, _serviceBusMessageReEnqueuer);
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_BlobCreatedEventCannotBeDeserialized_DeadLettersMessage()
    {
        // Act
        var process = async () => await _processor.ProcessTruckTicketScanUpload("Invalid JSON", _functionContext.Object);

        // Assert
        await process.Should().ThrowAsync<AggregateException>()
                     .WithMessage("Message Parsing Failure*");
    }

    [DataTestMethod]
    [DataRow("https://test.blob.core.windows.net/container1/invalid-ticket-number.pdf")]
    [DataRow("https://test.blob.core.windows.net/container1/KIFST132482-QT-INT.pdf")]
    public async Task ProcessTruckTicketScanUpload_TicketScanMetadataCannotBeParsed_DeadLettersMessage(string invalidUrl)
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = invalidUrl } }, EventSerializationOptions);

        // Act
        var process = async () => await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        await process.Should().ThrowAsync<AggregateException>()
                     .WithMessage("Ticket Metadata Parsing Failure*");
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_TruckTicketDoesNotExist_DeadLettersMessage_MissingApplicationProperties()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        SetupEntities(_truckTicketManager, Array.Empty<TruckTicketEntity>());

        // Act
        var process = async () => await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert

        await process.Should().ThrowAsync<AggregateException>()
                     .WithMessage("Missing Destination Truck Ticket*");
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_TruckTicketDoesNotExist_DeadLettersMessage_MessageNotReEnqueued_AfterMaxReEnqueueCount()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        SetupEntities(_truckTicketManager, Array.Empty<TruckTicketEntity>());

        var processTruckTicketScanUploadOptions = new ProcessTruckTicketScanUploadOptions
        {
            ReEnqueueOptions = new()
            {
                MaxReEnqueueCount = 3,
                BackoffType = BackoffTypeEnum.Exponential,
                Delay = TimeSpan.FromSeconds(30),
            },
        };

        SetupAppSettings(_appSettingsMock, processTruckTicketScanUploadOptions.ReEnqueueOptions);
        SetupFunctionContext_WithApplicationProperties_AfterMaxReEnqueueCount(_functionContext, _bindingData, processTruckTicketScanUploadOptions);

        SetupIntegrationServiceBus(_integrationsServiceBusMock);

        // Act
        var process = async () => await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert

        await process.Should().ThrowAsync<AggregateException>()
                     .WithMessage("Missing Destination Truck Ticket*");
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_TruckTicketDoesNotExist_DeadLettersMessage_MessageReEnqueued_AfterMaxDelay()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        SetupEntities(_truckTicketManager, Array.Empty<TruckTicketEntity>());

        var processTruckTicketScanUploadOptions = new ProcessTruckTicketScanUploadOptions
        {
            ReEnqueueOptions = new()
            {
                MaxReEnqueueCount = 10,
                BackoffType = BackoffTypeEnum.Exponential,
                Delay = TimeSpan.FromSeconds(30),
                MaxDelay = TimeSpan.FromSeconds(45),
            },
        };

        SetupAppSettings(_appSettingsMock, processTruckTicketScanUploadOptions.ReEnqueueOptions);
        SetupFunctionContext_WithApplicationProperties_AfterMaxDelay(_functionContext, _bindingData, processTruckTicketScanUploadOptions);
        SetupIntegrationServiceBus(_integrationsServiceBusMock);

        // Act
        await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        _leaseObjectBlobStorage.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_TruckTicketDoesNotExist_MessageReEnqueued_FirstTime()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        SetupEntities(_truckTicketManager, Array.Empty<TruckTicketEntity>());
        SetupFunctionContext_WithApplicationProperties_Empty(_functionContext, _bindingData);
        SetupIntegrationServiceBus(_integrationsServiceBusMock);

        // Act
        await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        _leaseObjectBlobStorage.VerifyNoOtherCalls();
    }

    public async Task ProcessTruckTicketScanUpload_TruckTicketDoesNotExist_MessageReEnqueued_SubsequentTime()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        SetupEntities(_truckTicketManager, Array.Empty<TruckTicketEntity>());

        var processTruckTicketScanUploadOptions = new ProcessTruckTicketScanUploadOptions
        {
            ReEnqueueOptions = new()
            {
                MaxReEnqueueCount = 3,
                BackoffType = BackoffTypeEnum.Exponential,
                Delay = TimeSpan.FromSeconds(30),
            },
        };

        SetupAppSettings(_appSettingsMock, processTruckTicketScanUploadOptions.ReEnqueueOptions);
        SetupFunctionContext_WithApplicationProperties_SuccessfulReEnqueue(_functionContext, _bindingData, processTruckTicketScanUploadOptions);
        SetupIntegrationServiceBus(_integrationsServiceBusMock);

        // Act
        await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        _leaseObjectBlobStorage.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_NewAttachmentAdded_SavesTruckTicket()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-INT.pdf" } },
                                                          EventSerializationOptions);

        var truckTicket = new TruckTicketEntity
        {
            TicketNumber = "KIFST123243-SP",
            CountryCode = CountryCode.CA,
        };

        SetupEntities(_truckTicketManager, new[] { truckTicket });

        _leaseObjectBlobStorage
           .Setup(p => p.AcquireLeaseAndExecute(It.IsAny<Func<Task<TruckTicketEntity>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Func<Task<TruckTicketEntity>> action, string _, string _, TimeSpan _, CancellationToken _) => action().Result);

        // Act
        await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        var existingAttachment = truckTicket.Attachments.Single(attachment => attachment.File == "KIFST123243-SP-INT.pdf");
        existingAttachment.File.Should().Be("KIFST123243-SP-INT.pdf");
        existingAttachment.Path.Should().Be("KIFST123243-SP-INT.pdf");
        existingAttachment.Container.Should().Be("container1");
        existingAttachment.AttachmentType.Should().Be(AttachmentType.Internal);
    }

    [TestMethod]
    public async Task ProcessTruckTicketScanUpload_ExistingAttachmentUpdated_SavesTruckTicket()
    {
        // Arrange
        var validBlobEventJson = JsonSerializer.Serialize(new BlobCreatedEvent { Data = new() { Url = "https://test.blob.core.windows.net/container1/KIFST123243-SP-EXT.pdf" } },
                                                          EventSerializationOptions);

        var truckTicket = new TruckTicketEntity
        {
            TicketNumber = "KIFST123243-SP",
            CountryCode = CountryCode.CA,
            Attachments = new()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    File = "KIFST123243-SP-EXT.pdf",
                    AttachmentType = AttachmentType.Internal,
                },
            },
        };

        SetupEntities(_truckTicketManager, new[] { truckTicket });

        _leaseObjectBlobStorage
           .Setup(p => p.AcquireLeaseAndExecute(It.IsAny<Func<Task<TruckTicketEntity>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Func<Task<TruckTicketEntity>> action, string _, string _, TimeSpan _, CancellationToken _) => action().Result);

        // Act
        await _processor.ProcessTruckTicketScanUpload(validBlobEventJson, _functionContext.Object);

        // Assert
        var existingAttachment = truckTicket.Attachments.Single(attachment => attachment.File == "KIFST123243-SP-EXT.pdf");
        existingAttachment.AttachmentType.Should().Be(AttachmentType.External);
    }

    private static void SetupEntities(Mock<ITruckTicketManager> mock, IEnumerable<TruckTicketEntity> entities)
    {
        mock.Setup(x => x.Get(It.IsAny<Expression<Func<TruckTicketEntity, bool>>>(),
                              It.IsAny<Func<IQueryable<TruckTicketEntity>,
                                  IOrderedQueryable<TruckTicketEntity>>>(),
                              It.IsAny<List<string>>(),
                              It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TruckTicketEntity, bool>> filter,
                           Func<IQueryable<TruckTicketEntity>, IOrderedQueryable<TruckTicketEntity>> _,
                           List<string> _,
                           bool _) => entities.Where(filter.Compile()));
    }

    private static void SetupFunctionContext_WithApplicationProperties_Empty(Mock<FunctionContext> mock, IDictionary<string, object> bindingData)
    {
        bindingData["ApplicationProperties"] = "{}";
    }

    private void SetupFunctionContext_WithApplicationProperties_AfterMaxReEnqueueCount(Mock<FunctionContext> mock, IDictionary<string, object> bindingData, ProcessTruckTicketScanUploadOptions processTruckTicketScanUploadOptions)
    {
        var reEnqueueOptions = processTruckTicketScanUploadOptions.ReEnqueueOptions;
        var reEnqueueCount = processTruckTicketScanUploadOptions.ReEnqueueOptions.MaxReEnqueueCount + 1;
        var reEnqueueState = new ReEnqueueState(reEnqueueOptions, reEnqueueCount);

        var applicationProperties = new Dictionary<string, object>
        {
            [ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonSerializer.Serialize(reEnqueueState)
        };

        bindingData["ApplicationProperties"] = JsonSerializer.Serialize(applicationProperties);

        _bindingContext.Setup(c => c.BindingData)!.Returns((IReadOnlyDictionary<string, object>) bindingData);
    }

    private void SetupFunctionContext_WithApplicationProperties_AfterMaxDelay(Mock<FunctionContext> mock, IDictionary<string, object> bindingData, ProcessTruckTicketScanUploadOptions processTruckTicketScanUploadOptions)
    {
        var reEnqueueOptions = processTruckTicketScanUploadOptions.ReEnqueueOptions;
        var reEnqueueCount = processTruckTicketScanUploadOptions.ReEnqueueOptions.MaxReEnqueueCount - 1;
        var reEnqueueState = new ReEnqueueState(reEnqueueOptions, reEnqueueCount);

        var applicationProperties = new Dictionary<string, object>
        {
            [ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonSerializer.Serialize(reEnqueueState)
        };

        bindingData["ApplicationProperties"] = JsonSerializer.Serialize(applicationProperties);

        _bindingContext.Setup(c => c.BindingData)!.Returns((IReadOnlyDictionary<string, object>)bindingData);
    }

    private void SetupFunctionContext_WithApplicationProperties_SuccessfulReEnqueue(Mock<FunctionContext> mock, IDictionary<string, object> bindingData, ProcessTruckTicketScanUploadOptions processTruckTicketScanUploadOptions)
    {
        var reEnqueueOptions = processTruckTicketScanUploadOptions.ReEnqueueOptions;
        var reEnqueueCount = processTruckTicketScanUploadOptions.ReEnqueueOptions.MaxReEnqueueCount - 1;
        var reEnqueueState = new ReEnqueueState(reEnqueueOptions, reEnqueueCount);

        var applicationProperties = new Dictionary<string, object>
        {
            [ReEnqueueConstants.Keys.ReEnqueueStrategy] = JsonSerializer.Serialize(reEnqueueState)
        };

        bindingData["ApplicationProperties"] = JsonSerializer.Serialize(applicationProperties);

        _bindingContext.Setup(c => c.BindingData)!.Returns((IReadOnlyDictionary<string, object>)bindingData);
    }

    private static void SetupIntegrationServiceBus(Mock<IIntegrationsServiceBus> mock)
    {
        mock.Setup(x => x.EnqueueDelayed(It.IsAny<string>(),
                                    It.IsAny<string>(),
                                    It.IsAny<Dictionary<string, string>>(),
                                    It.IsAny<DateTimeOffset>(),
                                    It.IsAny<string>(),
                                    It.IsAny<CancellationToken>()));
    }

    private static void SetupAppSettings(Mock<IAppSettings> mock, ReEnqueueOptions reEnqueueOptions)
    {
        var processTruckTicketScanUploadOptions = new ProcessTruckTicketScanUploadOptions
        {
            ReEnqueueOptions = reEnqueueOptions
        };

        mock.Setup(m => m.GetSection<ProcessTruckTicketScanUploadOptions>(It.IsAny<string>())).Returns(processTruckTicketScanUploadOptions);
    }
}
