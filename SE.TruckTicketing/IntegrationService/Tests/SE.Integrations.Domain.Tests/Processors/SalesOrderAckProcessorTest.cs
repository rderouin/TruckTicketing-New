using System;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.Invoices;
using SE.Shared.Domain.Entities.Note;

using Trident.Contracts;
using Trident.Data.Contracts;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SalesOrderAckProcessorTest
{
    private readonly Mock<IProvider<Guid, InvoiceEntity>> _invoiceProvider = new();

    private Mock<ILogger<SalesOrderAckProcessor>> _logger = null!;

    private Mock<IManager<Guid, NoteEntity>> _noteManager = null;

    private SalesOrderAckProcessor _processor = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _logger = new();
        _noteManager = new();
        _processor = new(_invoiceProvider.Object!, _logger.Object!, _noteManager.Object!);
    }

    [TestMethod]
    public async Task SalesOrderAckProcessor_Process_ValidMessage()
    {
        //arrange
        var entityModel = GenFu.GenFu.New<SalesOrderAckMessage>();
        var entityEnvelop = new EntityEnvelopeModel<SalesOrderAckMessage>();
        entityEnvelop.Payload = entityModel;
        Exception exc = null;

        //act
        try
        {
            await _processor.Process(entityEnvelop);
        }
        catch (Exception ex)
        {
            exc = ex;
        }

        //assert
        exc.Should().BeNull();
    }

    [TestMethod]
    public async Task SalesOrderAckProcessor_Process_WithNullInvoice()
    {
        //arrange
        var entityModel = GenFu.GenFu.New<SalesOrderAckMessage>();
        var entityEnvelop = new EntityEnvelopeModel<SalesOrderAckMessage>();
        entityEnvelop.Payload = entityModel;

        _invoiceProvider.Setup(p =>
                                   p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                        .ReturnsAsync(await Task.FromResult<InvoiceEntity>(null));

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(),
                                  It.IsAny<EventId>(),
                                  It.Is<It.IsAnyType>((v, t) => v.ToString() ==
                                                                $"Invoice {entityEnvelop.Payload.Id} with ProformaInvoiceNumber {entityEnvelop.Payload.ProformaInvoiceNumber} does not exist."),
                                  It.IsAny<Exception>(),
                                  It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
    }

    [TestMethod]
    public async Task SalesOrderAckProcessor_Process_WithAlreadyDeliveredToERP()
    {
        //arrange
        var entityModel = GenFu.GenFu.New<SalesOrderAckMessage>();
        var entityEnvelop = new EntityEnvelopeModel<SalesOrderAckMessage>();
        entityEnvelop.Payload = entityModel;

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        invoiceEntity.IsDeliveredToErp = true;

        _invoiceProvider.Setup(p =>
                                   p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                        .ReturnsAsync(invoiceEntity);

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(),
                                  It.IsAny<EventId>(),
                                  It.Is<It.IsAnyType>((v, t) => v.ToString() ==
                                                                $"Invoice {entityEnvelop.Payload.Id} with ProformaInvoiceNumber {entityEnvelop.Payload.ProformaInvoiceNumber} has already been delivered."),
                                  It.IsAny<Exception>(),
                                  It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
    }

    [TestMethod]
    public async Task SalesOrderAckProcessor_Process_WithValidMessage_ShouldCreateSuccessfulNoteMessage()
    {
        //arrange
        var entityModel = GenFu.GenFu.New<SalesOrderAckMessage>();
        entityModel.IsSuccessful = true;

        var entityEnvelop = new EntityEnvelopeModel<SalesOrderAckMessage> { Payload = entityModel };

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        invoiceEntity.IsDeliveredToErp = false;
        _invoiceProvider.Setup(p =>
                                   p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                        .ReturnsAsync(await Task.FromResult<InvoiceEntity>(invoiceEntity));

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _noteManager.Verify(tt => tt.Save(It.IsAny<NoteEntity>(),
                                          true), Times.Once);
    }

    [TestMethod]
    public async Task SalesOrderAckProcessor_Process_WithValidMessage_ShouldUpdateTheAcknowledgeFlag()
    {
        //arrange
        var entityModel = GenFu.GenFu.New<SalesOrderAckMessage>();
        entityModel.IsSuccessful = true;

        var entityEnvelop = new EntityEnvelopeModel<SalesOrderAckMessage> { Payload = entityModel };

        var invoiceEntity = GenFu.GenFu.New<InvoiceEntity>();
        invoiceEntity.IsDeliveredToErp = false;
        _invoiceProvider.Setup(p =>
                                   p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                        .ReturnsAsync(await Task.FromResult<InvoiceEntity>(invoiceEntity));

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _invoiceProvider.Verify(x => x.Update(It.IsAny<InvoiceEntity>(), It.IsAny<bool>()), Times.Once);
    }
}
