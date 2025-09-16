using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using SE.Enterprise.Contracts.Models;
using SE.Integrations.Domain.Processors.EntityProcessors;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Integrations.Domain.Tests.Processors;

[TestClass]
public class SalesLinesAckProcessorTest
{
    private readonly Mock<IProvider<Guid, SalesLineEntity>> _salesLineProvider = new();

    private Mock<ILogger<SalesLinesAckProcessor>> _logger = null!;

    private Mock<IManager<Guid, NoteEntity>> _noteManager = null;

    private SalesLinesAckProcessor _processor = null!;

    [TestInitialize]
    public void TestInitializer()
    {
        _logger = new();
        _noteManager = new();
        _processor = new(_salesLineProvider.Object!, _logger.Object!, _noteManager.Object!);
    }

    [TestMethod]
    public async Task SalesLineAckProcessor_Process_WithEmptySalesLineList()
    {
        //arrange

        //ensure payload has no sales line item
        var entityModel = new List<SalesLineAckMessage>();
        var entityEnvelop = new EntityEnvelopeModel<List<SalesLineAckMessage>> { Payload = entityModel };

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(),
                                  It.IsAny<EventId>(),
                                  It.Is<It.IsAnyType>((v, t) => v.ToString() ==
                                                                "Message contains no Sales Line acknowledgements."),
                                  It.IsAny<Exception>(),
                                  It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
    }

    [TestMethod]
    public async Task SalesLineAckProcessor_Process_WithSalesLineReconFailed()
    {
        //arrange
        var entityModel = GenFu.GenFu.ListOf<SalesLineAckMessage>(5);
        var entityEnvelop = new EntityEnvelopeModel<List<SalesLineAckMessage>> { Payload = entityModel };

        _salesLineProvider.Setup(p =>
                                     p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(await Task.FromResult<SalesLineEntity>(null));

        var listOfSalesLineEntities = GenFu.GenFu.ListOf<SalesLineEntity>(4);
        for (int i = 0; i < listOfSalesLineEntities.Count; i++)
        {
            entityModel[i].Id = listOfSalesLineEntities[i].Id;
        }

        _salesLineProvider.Setup(x => x.GetByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(listOfSalesLineEntities);

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _logger.Verify(x => x.Log(It.IsAny<LogLevel>(),
                                  It.IsAny<EventId>(),
                                  It.Is<It.IsAnyType>((v, t) => v.ToString() ==
                                                                $"Sales lines recon failed. Further investigation required. {entityEnvelop.ToJson()}"),
                                  It.IsAny<Exception>(),
                                  It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
    }

    [TestMethod]
    public async Task SalesLineAckProcessor_Process_WithSameNumberOfNotesCreated()
    {
        //arrange
        var entityModel = GenFu.GenFu.ListOf<SalesLineAckMessage>(5);
        var entityEnvelop = new EntityEnvelopeModel<List<SalesLineAckMessage>> { Payload = entityModel };

        _salesLineProvider.Setup(p =>
                                     p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(await Task.FromResult<SalesLineEntity>(null));

        var listOfSalesLineEntities = GenFu.GenFu.ListOf<SalesLineEntity>(4);
        for (int i = 0; i < listOfSalesLineEntities.Count; i++)
        {
            entityModel[i].Id = listOfSalesLineEntities[i].Id;
        }

        _salesLineProvider.Setup(x => x.GetByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(listOfSalesLineEntities);

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _noteManager.Verify(tt => tt.Save(It.IsAny<NoteEntity>(),
                                          It.IsAny<bool>()), Times.Exactly(listOfSalesLineEntities.Count));
    }

    [TestMethod]
    public async Task SalesLineAckProcessor_Process_WithSameNumberOfSalesLinesUpdated()
    {
        //arrange
        var entityModel = GenFu.GenFu.ListOf<SalesLineAckMessage>(5);
        var entityEnvelop = new EntityEnvelopeModel<List<SalesLineAckMessage>> { Payload = entityModel };

        _salesLineProvider.Setup(p =>
                                     p.GetById(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(await Task.FromResult<SalesLineEntity>(null));

        var listOfSalesLineEntities = GenFu.GenFu.ListOf<SalesLineEntity>(4);
        for (int i = 0; i < listOfSalesLineEntities.Count; i++)
        {
            entityModel[i].Id = listOfSalesLineEntities[i].Id;
        }

        _salesLineProvider.Setup(x => x.GetByIds(It.IsAny<IEnumerable<Guid>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                          .ReturnsAsync(listOfSalesLineEntities);

        //act
        await _processor.Process(entityEnvelop);

        //assert
        _salesLineProvider.Verify(tt => tt.Update(It.IsAny<SalesLineEntity>(),
                                                  It.IsAny<bool>()), Times.Exactly(listOfSalesLineEntities.Count));
    }
}
