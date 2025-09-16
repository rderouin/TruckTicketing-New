using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;

using Moq;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Functions;
using SE.Integrations.Api.Functions.TruckTicketAttachments;
using SE.Shared.Domain.Infrastructure;

using Trident.Contracts.Configuration;
using Trident.IoC;
using Trident.Logging;

namespace SE.Integrations.Api.Tests.Functions;

[TestClass]
public class TruckTicketAttachmentBlobFunctionsTests
{
    private Mock<BindingContext> _bindingContext = null!;

    private Dictionary<string, object> _bindingData = null!;

    private TruckTicketAttachmentBlobFunctions _truckTicketAttachmentBlobFunctions = null!;

    private Mock<FunctionContext> _functionContext = null!;

    private Mock<ILog> _log = null!;

    public Mock<IAppSettings> AppSettingsMock { get; } = new();

    public Mock<IIntegrationsServiceBus> IntegrationsServiceBusMock { get; } = new();

    private Mock<ITtScannedAttachmentBlobStorage> _ttScannedAttachmentBlobStorage = null;

    [TestInitialize]
    public void TestInitialize()
    {
        _log = new Mock<ILog>();
        _ttScannedAttachmentBlobStorage = new Mock<ITtScannedAttachmentBlobStorage>();
        _truckTicketAttachmentBlobFunctions = new TruckTicketAttachmentBlobFunctions(_log.Object!, AppSettingsMock.Object, IntegrationsServiceBusMock.Object);
        _functionContext = new Mock<FunctionContext>();
        _bindingContext = new Mock<BindingContext>();
        _functionContext.Setup(c => c.BindingContext)!.Returns(_bindingContext.Object);
        _bindingData = new Dictionary<string, object>();
        _bindingContext.Setup(c => c.BindingData)!.Returns(_bindingData);
    }

    private static void UpdateMetadataBasedOnMessage(Dictionary<string, object> metadata, string message)
    {
        var model = JsonConvert.DeserializeObject<EntityEnvelopeModel<CustomerModel>>(message)!;
        metadata[MessageConstants.EntityUpdate.MessageId] = $"{Guid.NewGuid():N}";
        metadata[MessageConstants.EntityUpdate.MessageType] = model.MessageType;
        metadata[MessageConstants.CorrelationId] = model.CorrelationId;
    }

    [TestMethod("TruckTicketAttachmentBlobFunctions should not be process a blank file name.")]
    public void TruckTicketAttachmentBlobFunctions_ProcessTruckTicketAttachmentBlobUpdate_BlankFileNameInfo()
    {
        // Arrange

        // Act
        Exception exception = null!;
        try
        {
            _truckTicketAttachmentBlobFunctions.Run("", "", null, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        _log.Verify(l => l.Error(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("File name is blank"))!, It.IsAny<object[]>()!));
        _log.Verify(l => l.Error(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("File name is blank"))!, It.IsAny<object[]>()!));
        exception.Should()!.BeOfType<Exception>();
    }

    [TestMethod("TruckTicketAttachmentBlobFunctions process a invalid uri.")]
    public void TruckTicketAttachmentBlobFunctions_ProcessTruckTicketAttachmentBlobUpdate_ValidInfo()
    {
        // Arrange

        // Act
        Exception exception = null!;
        try
        {
            _truckTicketAttachmentBlobFunctions.Run("", "TAFAT12345-LF", null, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        exception.Should()!.NotBeNull();
    }
}
