using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Integrations.Api.Functions;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Infrastructure;

using Trident.Contracts.Configuration;
using Trident.Data.Contracts;
using Trident.IoC;
using Trident.Logging;
using Trident.Mapper;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SalesOrderReceiverTest
{
    private readonly Mock<IProvider<Guid, InvoiceEntity>> _invoiceProvider = new();

    private Mock<IAppSettings> _appSettingMock = null!;

    private Mock<BindingContext> _bindingContext = null!;

    private Dictionary<string, object> _bindingData = null!;

    private Mock<IHttpClientFactory> _clientFactory;

    private SalesOrderEntityReceiverFunctions _entityFunctions = null!;

    private Mock<FunctionContext> _functionContext = null!;

    private Mock<HttpClient> _httpClientMock = null!;

    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;

    private IntegrationsServiceBus _integrationsServiceBus;

    private Mock<ILog> _log = null!;

    private Mock<ILogger<SalesOrderAckProcessor>> _logger = null!;

    private Mock<IMapperRegistry> _mapperMock;

    private Mock<IIoCServiceLocator> _serviceLocator = null!;

    public Mock<IAppSettings> AppSettingsMock { get; } = new();

    [TestInitialize]
    public void TestInitialize()
    {
        _httpMessageHandlerMock = new(MockBehavior.Strict);
        _log = new();
        _logger = new();

        _integrationsServiceBus =
            new("Endpoint=sb://zcac-sb-devint-private-s3skko4ttcwak.servicebus.windows.net/;SharedAccessKeyName=SharedAppKey;SharedAccessKey=xEqohRmcjZCbpYulwiyQAd1sMkr7nVvzCmeUBWZHUqE=");

        _serviceLocator = new();
        _appSettingMock = new();
        _clientFactory = new();
        _mapperMock = new();
        _entityFunctions = new(_log.Object!, _mapperMock.Object!, _appSettingMock.Object!, _clientFactory.Object!);
        _functionContext = new();
        _bindingContext = new();
        _functionContext.Setup(c => c.BindingContext)!.Returns(_bindingContext.Object);
        _bindingData = new()
        {
            { MessageConstants.EntityUpdate.MessageId, Guid.NewGuid().ToString() },
            { MessageConstants.EntityUpdate.MessageType, "SalesOrder" },
            { MessageConstants.CorrelationId, Guid.NewGuid().ToString() },
            { MessageConstants.SessionId, Guid.NewGuid().ToString() },
            { MessageConstants.SequenceNumber, 5.ToString() },
        };

        _bindingContext.Setup(c => c.BindingData)!.Returns(_bindingData);
        _appSettingMock.Setup(x => x.GetKeyOrDefault(It.IsAny<string>(), null)).Returns("http://google.com");
        //setup
        _httpClientMock = new();
        _clientFactory.Setup(x => x.CreateClient("client")).Returns(new HttpClient(_httpMessageHandlerMock.Object));
    }

    //this is not a test its used to generate data messags to test functionality.
    //[TestMethod]
    public async Task SalesOrder_Send_Valid_TestMessage()
    {
        var messageType = "SalesOrder";
        var correlationId = Guid.NewGuid();
        var envelopeModel = new EntityEnvelopeModel<SalesOrderAckMessage>();
        envelopeModel.EnterpriseId = Guid.NewGuid();
        envelopeModel.Source = "TT";
        envelopeModel.CorrelationId = correlationId.ToString();
        envelopeModel.MessageType = messageType;
        envelopeModel.Operation = "Update";
        Exception exc = null;
        try
        {
            envelopeModel.Payload = new();
            var sessionId = "22222";

            //topicName
            var queueOrTopicName = "enterprise-entity-updates";
            var metadata = new Dictionary<string, string>
            {
                [nameof(envelopeModel.Source)] = envelopeModel.Source,
                [nameof(envelopeModel.MessageType)] = envelopeModel.MessageType,
            };

            for (var i = 0; i < 4; i++)
            {
                envelopeModel.Payload.Message = i.ToString();
                await _integrationsServiceBus.Enqueue(queueOrTopicName, envelopeModel, metadata, sessionId);
            }
        }

        catch (Exception ex)
        {
            exc = ex;
        }

        //assert
        exc.Should().BeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_Success()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        // Assert
        //_entityProcessor.Verify(p => p.Process(It.Is<string>(m => m == message)!), Times.Once);
        //_log.Verify(l => l.Error(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Unable to process a message"))!, It.IsAny<object[]>()!));
        exception.Should().BeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_504()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.GatewayTimeout,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_400()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_500()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_404()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_408()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.RequestTimeout,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_503()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.ServiceUnavailable,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_429()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }

    [TestMethod]
    public async Task SalesOrder_Receive_Message_HTTP_Error_403()
    {
        // Arrange
        var message = typeof(SalesOrderReceiverTest).Assembly.GetResourceAsString("EntityFunctions-Customer-Sample.json", "Resources")!;
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("mocked API response"),
        };

        _httpMessageHandlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>("SendAsync",
                                             ItExpr.IsAny<HttpRequestMessage>(),
                                             ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(() =>
                         {
                             return httpResponse;
                         });

        // Act
        Exception exception = null!;
        try
        {
            await _entityFunctions.Run(message, _functionContext.Object!);
        }
        catch (Exception e)
        {
            exception = e;
        }

        _httpMessageHandlerMock
           .Protected()
           .Verify("SendAsync", Times.Exactly(1),
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>());

        exception.Should().NotBeNull();
    }
}
