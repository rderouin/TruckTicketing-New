using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.Enterprise.Contracts.Constants;
using SE.Enterprise.Contracts.Models;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.SalesLine;
using SE.TruckTicketing.Contracts.Lookups;

using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;

namespace SE.Integrations.Domain.Processors.EntityProcessors;

[EntityProcessorFor(ServiceBusConstants.EntityMessageTypes.SalesLineAck)]
public class SalesLinesAckProcessor : BaseEntityProcessor<List<SalesLineAckMessage>>
{
    private readonly ILogger<SalesLinesAckProcessor> _logger;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly IProvider<Guid, SalesLineEntity> _salesLineProvider;

    public SalesLinesAckProcessor(IProvider<Guid, SalesLineEntity> salesLineProvider, ILogger<SalesLinesAckProcessor> logger, IManager<Guid, NoteEntity> noteManager)
    {
        _salesLineProvider = salesLineProvider;
        _logger = logger;
        _noteManager = noteManager;
    }

    public override async Task Process(EntityEnvelopeModel<List<SalesLineAckMessage>> entityModel)
    {
        var acks = entityModel.Payload;

        if ((acks?.Count ?? 0) == 0)
        {
            _logger.LogWarning("Message contains no Sales Line acknowledgements.");
            return;
        }

        var salesLineAckMap = acks.DistinctBy(ack => ack.Id).ToDictionary(ack => ack.Id);
        var salesLines = (await _salesLineProvider.GetByIds(salesLineAckMap.Keys)).ToArray(); // PK - TODO: INT 

        var salesLineReconFailed = salesLines.Length != salesLineAckMap.Keys.Count;
        if (salesLineReconFailed)
        {
            _logger.LogWarning("Sales lines recon failed. Further investigation required. {0}", entityModel.ToJson());
        }

        foreach (var salesLine in salesLines)
        {
            // lookup the ack message
            var ack = salesLineAckMap[salesLine.Id];
            
            // extra logging for unmatched statuses
            _logger.LogInformation(new Dictionary<string, object>
            {
                ["AckMessage"] = ack,
                ["SalesLine"] = salesLine,
            }.ToJson());

            // change the status for approved messages upon successful submission/ack
            if (ack.IsSuccessful && ack.Status == salesLine.Status)
            {
                if (salesLine.Status is SalesLineStatus.Approved)
                {
                    salesLine.Status = SalesLineStatus.SentToFo;
                }

                if (salesLine.Status is SalesLineStatus.Void)
                {
                    salesLine.AwaitingRemovalAcknowledgment = false;
                }
            }

            // save it
            await _salesLineProvider.Update(salesLine, true);

            // save the note
            var opForNote = salesLine.Status == SalesLineStatus.Void ? "removal from" : "delivery to";
            await _noteManager.Save(new()
            {
                ThreadId = $"SalesLine|{salesLine.Id}",
                Comment = ack.IsSuccessful ? $"Successful sales line {opForNote} FO" : $"Unsuccessful sales line {opForNote} FO: {ack.Message}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "Integrations",
                CreatedBy = "Integrations",
            }, true);
        }

        // persist to DB
        await _salesLineProvider.SaveDeferred();
    }
}

public class SalesLineAckMessage
{
    public Guid Id { get; set; }

    public string SalesLineNumber { get; set; }

    public SalesLineStatus Status { get; set; }

    public bool IsSuccessful { get; set; }

    public string Message { get; set; }
}
