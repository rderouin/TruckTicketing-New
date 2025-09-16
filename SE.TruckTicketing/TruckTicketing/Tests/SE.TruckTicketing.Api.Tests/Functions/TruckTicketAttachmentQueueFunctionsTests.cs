using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Azure.Functions.Worker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Api.Functions;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.TruckTicket;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;

namespace SE.TruckTicketing.Api.Tests.Functions;

[TestClass]
public class TruckTicketAttachmentQueueFunctionsTests
{
    private Mock<BindingContext> _bindingContextMock = null!;

    private Dictionary<string, object> _bindingData = null!;

    private TruckTicketAttachmentQueueProcessor _truckTicketAttachmentQueueProcessor = null!;

    private Mock<FunctionContext> _functionContextMock = null!;

    private Mock<ILog> _log = null!;

    private Mock<IProvider<Guid, FacilityEntity>> _facilityProviderMock = null;

    private Mock<ITruckTicketManager> _truckTicketManagerMock;

    private Mock<IMapperRegistry> _mapperMock;

    [TestInitialize]
    public void TestInitialize()
    {
        //_entityProcessor = new Mock<IEntityProcessor<CustomerModel>>();
        _log = new Mock<ILog>();
        //_serviceLocator = new Mock<IIoCServiceLocator>();
        _truckTicketManagerMock = new Mock<ITruckTicketManager>();
        _mapperMock = new Mock<IMapperRegistry>();
        _facilityProviderMock = new Mock<IProvider<Guid, FacilityEntity>>();
        _truckTicketAttachmentQueueProcessor = new TruckTicketAttachmentQueueProcessor(_log.Object!, _mapperMock.Object!, _truckTicketManagerMock.Object!, _facilityProviderMock.Object!);
        _functionContextMock = new Mock<FunctionContext>();
        _bindingContextMock = new Mock<BindingContext>();
        _functionContextMock.Setup(c => c.BindingContext)!.Returns(_bindingContextMock.Object);
        _bindingData = new Dictionary<string, object>();
        _bindingContextMock.Setup(c => c.BindingData)!.Returns(_bindingData);
    }

    [TestMethod("Run TruckTicketAttachmentQueueFunctionsTests should not process a message other than scanned attachment type.")]
    public async Task TruckTicketAttachmentQueueFunctions_Run_OtherThanScannedAttacment()
    {
        // Arrange
        var message = ReadTheFile()!;
        UpdateMetadataBasedOnMessage(_bindingData, message);
        _bindingData[MessageConstants.EntityUpdate.MessageType] = "NonScannedAttachement";

        // Act
        await _truckTicketAttachmentQueueProcessor.Run(message, _functionContextMock.Object!);

        // Assert
        _log.Verify(l => l.Information(It.IsAny<Type>()!, It.IsAny<Exception>()!, It.Is<string>(m => m.Contains("Message Type is not ScannedAttachment"))!, It.IsAny<object[]>()!));
    }

    [TestMethod("Run TruckTicketAttachmentQueueFunctionsTests for valid ticket number and without facility information.")]
    public async Task TruckTicketAttachmentQueueFunctions_Run_ValidTicketNumber()
    {
        // Arrange
        var message = ReadTheFile()!;
        var model = JsonConvert.DeserializeObject<EntityEnvelopeModel<TruckTicketAttachment>>(message);
        UpdateMetadataBasedOnMessage(_bindingData, message);
        List<FacilityEntity> listOfFacilityEntities = new List<FacilityEntity>()
        {
            new FacilityEntity
            {
                SiteId = "TAFAT",
                Name = "Facility1",
                Type = FacilityType.Lf,
                LegalEntity = "Canada"
            }
        };

        _facilityProviderMock.Setup(m =>
                                        m.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
                             .ReturnsAsync(new SearchResults<FacilityEntity, SearchCriteria>
                              {
                                  Results = listOfFacilityEntities,
                              });

        // Act
        Exception exception = null;
        try
        {
            await _truckTicketAttachmentQueueProcessor.Run(message, _functionContextMock.Object!);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should()!.BeOfType<Exception>();
    }

    [TestMethod("Run TruckTicketAttachmentQueueFunctionsTests for existing Truck Ticket.")]
    public async Task TruckTicketAttachmentQueueFunctions_Run_ExistingTicketNumber()
    {
        // Arrange
        var message = ReadTheFile()!;
        UpdateMetadataBasedOnMessage(_bindingData, message);
        List<FacilityEntity> listOfFacilityEntities = new List<FacilityEntity>()
        {
            new FacilityEntity
            {
                Id = Guid.NewGuid(),
                SiteId = "TAFAT",
                Name = "Facility1",
                Type = FacilityType.Lf,
                LegalEntity = "Canada"
            }
        };

        var truckTicket = new TruckTicketEntity() { Attachments = new List<TruckTicketAttachmentEntity>() };

        List<TruckTicketEntity> listOfTTEntities = new List<TruckTicketEntity>()
        {
            truckTicket,
        };

        _facilityProviderMock.Setup(m =>
                                        m.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
                             .ReturnsAsync(new SearchResults<FacilityEntity, SearchCriteria>
                              {
                                  Results = listOfFacilityEntities,
                              });

        _truckTicketManagerMock.Setup(m =>
                                          m.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
                               .ReturnsAsync(new SearchResults<TruckTicketEntity, SearchCriteria>
                                {
                                    Results = listOfTTEntities,
                                });

        _truckTicketManagerMock.Setup(m =>
                                          m.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
                               .ReturnsAsync(truckTicket);

        _mapperMock.Setup(m => m.Map<TruckTicketAttachmentEntity>(It.IsAny<object>())).Returns(new TruckTicketAttachmentEntity());

        // Act
        Exception exception = null;
        try
        {
            await _truckTicketAttachmentQueueProcessor.Run(message, _functionContextMock.Object!);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        //Assert
        exception.Should().BeNull();
    }

    [TestMethod("Run TruckTicketAttachmentQueueFunctionsTests for new Truck Ticket.")]
    public async Task TruckTicketAttachmentQueueFunctions_Run_NewTicketNumber()
    {
        // Arrange
        var message = ReadTheFile()!;
        UpdateMetadataBasedOnMessage(_bindingData, message);
        List<FacilityEntity> listOfFacilityEntities = new List<FacilityEntity>()
        {
            new FacilityEntity
            {
                Id = Guid.NewGuid(),
                SiteId = "TAFAT",
                Name = "Facility1",
                Type = FacilityType.Lf,
                LegalEntity = "Canada"
            }
        };

        var truckTicket = new TruckTicketEntity() { Attachments = new List<TruckTicketAttachmentEntity>() };

        List<TruckTicketEntity> listOfTTEntities = new List<TruckTicketEntity>();
        _facilityProviderMock.Setup(m =>
                                        m.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
                             .ReturnsAsync(new SearchResults<FacilityEntity, SearchCriteria>
                              {
                                  Results = listOfFacilityEntities,
                              });

        _truckTicketManagerMock.Setup(m =>
                                          m.Search(It.IsAny<SearchCriteria>(), It.IsAny<bool>()))
                               .ReturnsAsync(new SearchResults<TruckTicketEntity, SearchCriteria>
                                {
                                    Results = listOfTTEntities,
                                });

        _truckTicketManagerMock.Setup(m =>
                                          m.Save(It.IsAny<TruckTicketEntity>(), It.IsAny<bool>()))
                               .ReturnsAsync(truckTicket);


        _mapperMock.Setup(m => m.Map<TruckTicketAttachmentEntity>(It.IsAny<object>())).Returns(new TruckTicketAttachmentEntity());
        // Act
        Exception exception = null;
        try
        {
            await _truckTicketAttachmentQueueProcessor.Run(message, _functionContextMock.Object!);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        //Assert
        exception.Should().BeNull();
    }

    private string ReadTheFile()
    {
        var assemblyName = typeof(TruckTicketAttachmentQueueFunctionsTests).Assembly.GetName().Name;
        var directory = string.Join(".", "Resources");
        var fileName = "Attachment-SampleData.json";
        var fullPath = $@"{assemblyName}.{directory}.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullPath);

        // read contents as string
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void UpdateMetadataBasedOnMessage(Dictionary<string, object> metadata, string message)
    {
        var model = JsonConvert.DeserializeObject<EntityEnvelopeModel<TruckTicketAttachment>>(message)!;
        metadata[MessageConstants.EntityUpdate.MessageId] = $"{Guid.NewGuid():N}";
        metadata[MessageConstants.EntityUpdate.MessageType] = model.MessageType;
        metadata[MessageConstants.CorrelationId] = model.CorrelationId;
    }
}
