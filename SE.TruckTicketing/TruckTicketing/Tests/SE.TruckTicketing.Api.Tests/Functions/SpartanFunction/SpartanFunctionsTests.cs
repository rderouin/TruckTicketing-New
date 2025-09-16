using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Processors;
using SE.TruckTicketing.Api.Functions.SpartanData;
using SE.TruckTicketing.Contracts.Api.Models.SpartanData;

using Trident.IoC;
using Trident.Logging;

namespace SE.TruckTicketing.Api.Tests.Functions.SpartanFunction;

[TestClass]
public class SpartanFunctionsTests
{
    private Mock<BindingContext> _bindingContext = null!;

    private Dictionary<string, object> _bindingData = null!;

    private Mock<IEntityProcessor<SpartanSummaryModel>> _entityProcessor = null!;

    private Mock<FunctionContext> _functionContext = null!;

    private Mock<ILog> _log = null!;

    private Mock<IIoCServiceLocator> _serviceLocator = null!;

    private SpartanIntegrationFunctions _spartanIntegrationFunctions = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _entityProcessor = new();
        _log = new();
        _serviceLocator = new();
        _spartanIntegrationFunctions = new(_log.Object!, _serviceLocator.Object!);
        _functionContext = new();
        _bindingContext = new();
        _functionContext.Setup(c => c.BindingContext)!.Returns(_bindingContext.Object);
        _bindingData = new();
        _bindingContext.Setup(c => c.BindingData)!.Returns(_bindingData);
    }

    [TestMethod("ProcessEntityUpdate should be able to process a unsupported message.")]
    public async Task EntityFunctions_ProcessEntityUpdate_BadMessage()
    {
        // Arrange
        var message = typeof(SpartanFunctionsTests).Assembly.GetResourceAsString("SpartanFunctions-Sample.json", "Resources")!;
        _serviceLocator.Setup(l => l.GetOptionalNamed<IEntityProcessor>(It.Is<string>(s => s == "SpartanOffLoadSummary")!))!.Returns(_entityProcessor.Object);
        _entityProcessor.Setup(p => p.Process(It.IsAny<string>()!))!.Throws<InvalidOperationException>();
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        Exception exception = null!;
        try
        {
            await _spartanIntegrationFunctions.ProcessSpartanTicket(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        _entityProcessor.Verify(p => p.Process(It.Is<string>(m => m == message)!), Times.Once);
        _log.Verify(l => l.Error(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Unable to process a message"))!, It.IsAny<object[]>()!));
        exception.Should()!.BeOfType<InvalidOperationException>();
    }

    [TestMethod("ProcessEntityUpdate should be able to process a normal message.")]
    public async Task EntityFunctions_ProcessEntityUpdate_NormalMessage()
    {
        // Arrange
        var message = typeof(SpartanFunctionsTests).Assembly.GetResourceAsString("SpartanFunctions-Sample.json", "Resources")!;
        _serviceLocator.Setup(l => l.GetOptionalNamed<IEntityProcessor>(It.Is<string>(s => s == "SpartanOffLoadSummary")!))!.Returns(_entityProcessor.Object);
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        await _spartanIntegrationFunctions.ProcessSpartanTicket(message, _functionContext.Object!);

        // Assert
        _entityProcessor.Verify(p => p.Process(It.Is<string>(m => m == message)!), Times.Once);
    }

    private static void UpdateMetadataBasedOnMessage(Dictionary<string, object> metadata, string message)
    {
        var model = JsonConvert.DeserializeObject<EntityEnvelopeModel<CustomerModel>>(message)!;
        metadata[MessageConstants.EntityUpdate.MessageId] = $"{Guid.NewGuid():N}";
        metadata[MessageConstants.EntityUpdate.MessageType] = model.MessageType;
        metadata[MessageConstants.CorrelationId] = model.CorrelationId;
    }
}
