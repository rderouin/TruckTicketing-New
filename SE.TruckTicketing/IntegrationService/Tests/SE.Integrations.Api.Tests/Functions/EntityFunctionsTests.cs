using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;

using Moq;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Functions;
using SE.Integrations.Domain.Processors;

using Trident.IoC;
using Trident.Logging;

namespace SE.Integrations.Api.Tests.Functions;

[TestClass]
public class EntityFunctionsTests
{
    private Mock<BindingContext> _bindingContext = null!;

    private Dictionary<string, object> _bindingData = null!;

    private EntityFunctions _entityFunctions = null!;

    private Mock<IEntityProcessor<CustomerModel>> _entityProcessor = null!;

    private Mock<FunctionContext> _functionContext = null!;

    private Mock<ILog> _log = null!;

    private Mock<IIoCServiceLocator> _serviceLocator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _entityProcessor = new();
        _log = new();
        _serviceLocator = new();
        _entityFunctions = new(_log.Object!, _serviceLocator.Object!);
        _functionContext = new();
        _bindingContext = new();
        _functionContext.Setup(c => c.BindingContext)!.Returns(_bindingContext.Object);
        _bindingData = new();
        _bindingContext.Setup(c => c.BindingData)!.Returns(_bindingData);
    }

    [TestMethod("ProcessEntityUpdate should be able to process a blank message.")]
    [ExpectedException(typeof(ArgumentException),
                       "Message is blank")]
    public async Task EntityFunctions_ProcessEntityUpdate_BlankMessage()
    {
        // Arrange

        // Act
        await _entityFunctions.ProcessEntityUpdate("", _functionContext.Object!);

        // Assert
        _serviceLocator.VerifyNoOtherCalls();
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message is blank"))!, It.IsAny<object[]>()!));
    }

    [TestMethod("ProcessEntityUpdate should be able to process a blank JSON message.")]
    [ExpectedException(typeof(ArgumentException),
                       "Message is blank")]
    public async Task EntityFunctions_ProcessEntityUpdate_BlankJsonMessage()
    {
        // Arrange

        // Act
        await _entityFunctions.ProcessEntityUpdate("{}", _functionContext.Object!);

        // Assert
        _serviceLocator.VerifyNoOtherCalls();
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message Type is blank"))!, It.IsAny<object[]>()!));
    }

    [TestMethod("ProcessEntityUpdate should be able to process a unsupported message.")]
    [ExpectedException(typeof(ArgumentException),
                       "Entity Processor is not defined for the message type")]
    public async Task EntityFunctions_ProcessEntityUpdate_UnsupportedMessage()
    {
        // Arrange
        var message = @"{MessageType:""Unsupported""}";
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        await _entityFunctions.ProcessEntityUpdate(message, _functionContext.Object!);

        // Assert
        _log.Verify(l => l.Warning(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Entity Processor is not defined for the message type"))!, It.IsAny<object[]>()!));
    }

    [TestMethod("ProcessEntityUpdate should be able to process a unsupported message.")]
    public async Task EntityFunctions_ProcessEntityUpdate_BadMessage()
    {
        // Arrange
        var message = typeof(EntityFunctionsTests).Assembly.GetResourceAsString("EntityFunctions-Sample.json", "Resources")!;
        _serviceLocator.Setup(l => l.GetOptionalNamed<IEntityProcessor>(It.Is<string>(s => s == "Customer")!))!.Returns(_entityProcessor.Object);
        _entityProcessor.Setup(p => p.Process(It.IsAny<string>()!))!.Throws<InvalidOperationException>();
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.ProcessEntityUpdate(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        // Assert
        _entityProcessor.Verify(p => p.Process(It.Is<string>(m => m == message)!), Times.Once);
        //_log.Verify(l => l.Error(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Unable to process a message"))!, It.IsAny<object[]>()!));
        exception.Should()!.BeOfType<InvalidOperationException>();
    }

    [TestMethod("ProcessEntityUpdate should be able to process a normal message.")]
    public async Task EntityFunctions_ProcessEntityUpdate_NormalMessage()
    {
        // Arrange
        var message = typeof(EntityFunctionsTests).Assembly.GetResourceAsString("EntityFunctions-Sample.json", "Resources")!;
        _serviceLocator.Setup(l => l.GetOptionalNamed<IEntityProcessor>(It.Is<string>(s => s == "Customer")!))!.Returns(_entityProcessor.Object);
        UpdateMetadataBasedOnMessage(_bindingData, message);

        // Act
        await _entityFunctions.ProcessEntityUpdate(message, _functionContext.Object!);

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
