using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.Note;
using SE.Shared.Domain.Entities.Sequences;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Infrastructure;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.SalesLine;

using Trident.Business;
using Trident.Contracts;
using Trident.Contracts.Api;
using Trident.Contracts.Enums;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Search;
using Trident.Validation;
using Trident.Workflow;

using Stream = System.IO.Stream;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketManager : ManagerBase<Guid, TruckTicketEntity>, ITruckTicketManager
{
    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IMatchPredicateManager _matchPredicateManager;

    private readonly IMatchPredicateRankManager _matchPredicateRankManager;

    private readonly IManager<Guid, NoteEntity> _noteManager;

    private readonly IProvider<Guid, NoteEntity> _noteProvider;

    private readonly ISalesLineManager _salesLinesManager;

    private readonly ISequenceNumberGenerator _sequenceNumberGenerator;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly ITruckTicketUploadBlobStorage _truckTicketUploadBlobStorage;

    private readonly IManager<Guid, TruckTicketWellClassificationUsageEntity> _truckTicketWellClassificationUsageEntity;

    public TruckTicketManager(ILog logger,
                              IProvider<Guid, TruckTicketEntity> provider,
                              ISequenceNumberGenerator sequenceNumberGenerator,
                              IProvider<Guid, FacilityEntity> facilityProvider,
                              ITruckTicketUploadBlobStorage truckTicketUploadBlobStorage,
                              IMatchPredicateManager matchPredicateManager,
                              IMatchPredicateRankManager matchPredicateRankManager,
                              IProvider<Guid, NoteEntity> noteProvider,
                              IManager<Guid, NoteEntity> noteManager,
                              ISalesLineManager salesLinesManager,
                              IManager<Guid, TruckTicketWellClassificationUsageEntity> truckTicketWellClassificationUsageEntity,
                              IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                              IValidationManager<TruckTicketEntity> validationManager = null,
                              IWorkflowManager<TruckTicketEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _sequenceNumberGenerator = sequenceNumberGenerator;
        _facilityProvider = facilityProvider;
        _noteProvider = noteProvider;
        _noteManager = noteManager;
        _salesLinesManager = salesLinesManager;
        _truckTicketUploadBlobStorage = truckTicketUploadBlobStorage;
        _truckTicketWellClassificationUsageEntity = truckTicketWellClassificationUsageEntity;
        _sourceLocationProvider = sourceLocationProvider;
        _matchPredicateManager = matchPredicateManager;
        _matchPredicateRankManager = matchPredicateRankManager;
    }

    public async Task CreatePrePrintedTruckTicketStubs(Guid facilityId, int count)
    {
        await CreatePrePrintedTruckTicketStubs(facilityId, count, null);
    }

    public async Task CreatePrePrintedTruckTicketStubs(Guid facilityId, int count, Func<IEnumerable<TruckTicketEntity>, Task> beforeSave)
    {
        var facility = await _facilityProvider.GetById(facilityId);

        var sequenceType = facility?.Type == FacilityType.Lf ? SequenceTypes.ScaleTicket : SequenceTypes.WorkTicket;

        var truckTickets = await _sequenceNumberGenerator.GenerateSequenceNumbers(sequenceType, facility?.SiteId, count).Select(sequenceNumber => new TruckTicketEntity
        {
            Status = TruckTicketStatus.Stub,
            Date = DateTime.UtcNow,
            TicketNumber = sequenceNumber,
            FacilityId = (facility?.Id).GetValueOrDefault(),
            FacilityName = facility?.Name,
            SiteId = facility?.SiteId,
            FacilityType = facility?.Type,
            CountryCode = facility?.CountryCode ?? CountryCode.Undefined,
            LegalEntity = facility?.LegalEntity,
            LegalEntityId = facility?.LegalEntityId ?? Guid.Empty,
            FacilityLocationCode = facility?.LocationCode,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        }).ToListAsync();

        truckTickets.ForEach(truckTicket => SetTruckTicketType(truckTicket));

        truckTickets.ForEach(entity => AssureUniqueId(entity));

        beforeSave?.Invoke(truckTickets);

        await BulkSave(truckTickets);
    }

    public Task<string> GetAttachmentDownloadUri(string path)
    {
        var uri = _truckTicketUploadBlobStorage.GetDownloadUri(_truckTicketUploadBlobStorage.DefaultContainerName, path, DispositionTypeNames.Attachment, null);
        return Task.FromResult(uri.ToString());
    }

    public Task<string> GetAttachmentUploadUri(string blobName)
    {
        var uri = _truckTicketUploadBlobStorage.GetUploadUri(_truckTicketUploadBlobStorage.DefaultContainerName, blobName);
        return Task.FromResult(uri.ToString());
    }

    public async Task<Stream> GetFileStream(string blobName)
    {
        return await _truckTicketUploadBlobStorage.Download(_truckTicketUploadBlobStorage.DefaultContainerName, blobName);
    }

    public async Task<List<BillingConfigurationEntity>> GetMatchingBillingConfigurations(TruckTicketEntity truckTicket)
    {
        var billingConfigs = await _matchPredicateManager.GetBillingConfigurations(truckTicket);
        if (billingConfigs == null || !billingConfigs.Any())
        {
            return new();
        }

        var matchingBillingConfigs = billingConfigs.Where(billingConfig => billingConfig.IncludeForAutomation is false).ToList();
        var automatedBillingConfig = _matchPredicateManager.SelectAutomatedBillingConfiguration(billingConfigs, truckTicket);
        if (automatedBillingConfig.Id != Guid.Empty)
        {
            matchingBillingConfigs.Add(automatedBillingConfig);
        }

        return matchingBillingConfigs;
    }

    public async Task<bool> ConfirmCustomerOnTickets(IEnumerable<TruckTicketEntity> splitTruckTicketList)
    {
        var customerIds = new HashSet<Guid>();
        foreach (var truckTicket in splitTruckTicketList)
        {
            //set source location and well classification usage
            SetSourceLocationAutoPopulateWellClassification(truckTicket);

            var billingConfigs = await GetMatchingBillingConfigurations(truckTicket);
            var customerId = new Guid();
            if (billingConfigs != null && billingConfigs.Count == 1)
            {
                customerId = billingConfigs.FirstOrDefault()!.BillingCustomerAccountId;
            }

            customerIds.Add(customerId);
        }

        return customerIds.Count == 1;
    }

    public async Task<IEnumerable<TruckTicketEntity>> SplitTruckTicket(IEnumerable<TruckTicketEntity> splitTruckTicketList, CompositeKey<Guid> truckTicketKey)
    {
        var sourceTruckTicket = await GetById(truckTicketKey); // PK - OK
        var splitTruckTickets = splitTruckTicketList.ToList();
        //obtain the ticket number sequences
        var facility = await _facilityProvider.GetById(sourceTruckTicket.FacilityId);
        var sequenceType = facility?.Type == FacilityType.Lf ? SequenceTypes.ScaleTicket : SequenceTypes.WorkTicket;
        var truckTicketNumbers = await _sequenceNumberGenerator.GenerateSequenceNumbers(sequenceType, facility?.SiteId, splitTruckTickets.Count).ToListAsync();

        //Notes from source ticket
        var searchCriteria = new SearchCriteria
        {
            OrderBy = nameof(NoteEntity.CreatedAt),
            SortOrder = SortOrder.Desc,
        };

        searchCriteria.AddFilter("DocumentType", $"NoteEntity|TruckTicket|{truckTicketKey.Id}");
        var noteSearchResult = await _noteProvider.Search(searchCriteria); // PK - OK
        List<NoteEntity> listOfNotesFromSourceTicket = new();
        foreach (var noteEntity in noteSearchResult.Results)
        {
            listOfNotesFromSourceTicket.Add(noteEntity.Clone());
        }

        List<TruckTicketEntity> truckTickets = new();
        for (var count = 0; count < splitTruckTickets.Count; count++)
        {
            var splitTruckTicket = splitTruckTickets[count];
            splitTruckTicket.Id = Guid.NewGuid();
            splitTruckTicket.TicketNumber = truckTicketNumbers[count];

            //clone notes
            foreach (var noteEntity in listOfNotesFromSourceTicket)
            {
                noteEntity.Id = Guid.NewGuid();
                noteEntity.ThreadId = $"TruckTicket|{splitTruckTicket.Id}";
                noteEntity.InitPartitionKey();
                await _noteProvider.Insert(noteEntity, true);
            }

            await AddNote(new()
            {
                Comment = $"Ticket has been split from {sourceTruckTicket.TicketNumber}",
                Id = Guid.NewGuid(),
                ThreadId = $"TruckTicket|{splitTruckTicket.Id}",
            }, true);

            //set source location and well classification usage
            SetSourceLocationAutoPopulateWellClassification(splitTruckTicket);

            //set billing config on split ticket
            var billingConfigs = await GetMatchingBillingConfigurations(splitTruckTicket);
            if (billingConfigs.Count == 1)
            {
                SetBillingConfigOnSplitTicket(splitTruckTicket, billingConfigs.FirstOrDefault());
            }

            splitTruckTicket.ParentTicketID = truckTicketKey.Id;
            splitTruckTicket.Status = TruckTicketStatus.New;
            splitTruckTicket.VolumeChangeReason = VolumeChangeReason.SplitLoad;

            var truckTicket = await Insert(splitTruckTicket, true);
            if (truckTicket is not null)
            {
                truckTickets.Add(truckTicket);
            }
        }

        //Sales lines
        await DeleteNotInvoicedSalesLines(truckTicketKey);

        await AddNote(new()
        {
            Comment = $"{sourceTruckTicket.TicketNumber} has been split into {string.Join(", ", truckTickets.Select(x => x.TicketNumber))}",
            Id = Guid.NewGuid(),
            ThreadId = $"TruckTicket|{truckTicketKey.Id}",
        }, true);

        sourceTruckTicket.Status = TruckTicketStatus.Void;
        sourceTruckTicket.VoidReason = "Split Ticket Source";
        sourceTruckTicket.VolumeChangeReason = VolumeChangeReason.SplitLoad;
        await Update(sourceTruckTicket);
        return truckTickets;
    }

    public async Task<Uri> GetDownloadUrl(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await GetById(truckTicketKey); // PK - OK

        var attachment = truckTicket?.Attachments.FirstOrDefault(attachment => attachment.Id == attachmentId);

        return attachment == null ? null : _truckTicketUploadBlobStorage.GetDownloadUri(attachment.Container, attachment.Path, DispositionTypeNames.Inline, attachment.ContentType);
    }

    public async Task<(TruckTicketAttachmentEntity attachment, string uri)> GetUploadUrl(CompositeKey<Guid> truckTicketKey, string filename, string contentType)
    {
        var attachmentId = Guid.NewGuid();
        var path = $"{truckTicketKey.Id}/{attachmentId}";
        var uri = _truckTicketUploadBlobStorage.GetUploadUri(_truckTicketUploadBlobStorage.DefaultContainerName, path);

        var attachmentEntity = new TruckTicketAttachmentEntity
        {
            Id = attachmentId,
            Container = _truckTicketUploadBlobStorage.DefaultContainerName,
            Path = path,
            File = filename,
            ContentType = contentType,
        };

        var truckTicket = await GetById(truckTicketKey); // PK - OK
        truckTicket.Attachments.Add(attachmentEntity);
        await Update(truckTicket);

        return (attachmentEntity, uri.ToString());
    }

    public async Task<TruckTicketEntity> MarkFileUploaded(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await GetById(truckTicketKey); // PK - OK
        var attachment = truckTicket?.Attachments.FirstOrDefault(a => a.Id == attachmentId);

        if (attachment == null)
        {
            return truckTicket;
        }

        attachment.IsUploaded = true;

        await Update(truckTicket);

        return truckTicket;
    }

    public async Task<TruckTicketEntity> RemoveAttachmentOnTruckTicket(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var truckTicket = await GetById(truckTicketKey); // PK - OK
        var attachment = truckTicket?.Attachments.FirstOrDefault(a => a.Id == attachmentId);

        if (attachment == null)
        {
            return truckTicket;
        }

        truckTicket.Attachments.Remove(attachment);

        await Update(truckTicket);

        return truckTicket;
    }

    private static void SetTruckTicketType(TruckTicketEntity truckTicket)
    {
        // characters after last hyphen is ticket type
        var ticketNumberTicketType = truckTicket.TicketNumber?[(truckTicket.TicketNumber.LastIndexOf('-') + 1)..];
        if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.LF.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.LF;
        }
        else if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.SP.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.SP;
        }
        else if (ticketNumberTicketType != null && ticketNumberTicketType.Equals(TruckTicketType.WT.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            truckTicket.TruckTicketType = TruckTicketType.WT;
        }
        else
        {
            truckTicket.TruckTicketType = TruckTicketType.Undefined;
        }
    }

    private async Task AddNote(NoteEntity noteEntity, bool deferCommit = false)
    {
        await _noteManager.Insert(noteEntity, deferCommit);
    }

    private async Task DeleteNotInvoicedSalesLines(CompositeKey<Guid> truckTicketKey)
    {
        var truckTicketSalesLines = await _salesLinesManager.Get(salesLine => salesLine.TruckTicketId == truckTicketKey.Id && salesLine.Status != SalesLineStatus.Posted); // PK - XP for SL by TT ID
        foreach (var truckTicketSalesLine in truckTicketSalesLines)
        {
            await _salesLinesManager.Delete(truckTicketSalesLine, true);
        }
    }

    private static void SetBillingConfigOnSplitTicket(TruckTicketEntity truckTicketEntity, BillingConfigurationEntity billingConfiguration)
    {
        truckTicketEntity.BillingCustomerId = billingConfiguration?.BillingCustomerAccountId ?? default;

        truckTicketEntity.BillingContact = new()
        {
            AccountContactId = billingConfiguration?.BillingContactId,
        };

        truckTicketEntity.EdiFieldValues = billingConfiguration?
                                          .EDIValueData
                                          .Select(e => e.Clone())
                                          .ToList();

        truckTicketEntity.Signatories = billingConfiguration?
                                       .Signatories
                                       .Where(e => e.IsAuthorized)
                                       .Select(e => new SignatoryEntity
                                        {
                                            AccountContactId = e.AccountContactId,
                                            ContactEmail = e.Email,
                                            ContactPhoneNumber = e.PhoneNumber,
                                            ContactAddress = e.Address,
                                            ContactName = e.FirstName + " " + e.LastName,
                                        })
                                       .ToList();
    }

    private void SetSourceLocationAutoPopulateWellClassification(TruckTicketEntity truckTicket)
    {
        if (truckTicket.FacilityId == default || truckTicket.SourceLocationId == default)
        {
            return;
        }

        var sourceLocation = _sourceLocationProvider.GetById(truckTicket.SourceLocationId).Result;
        truckTicket.SourceLocationFormatted = sourceLocation.FormattedIdentifier;
        truckTicket.SourceLocationName = sourceLocation.SourceLocationName;
        truckTicket.GeneratorId = sourceLocation.GeneratorId;
        truckTicket.GeneratorName = sourceLocation.GeneratorName;

        var criteria = new SearchCriteria
        {
            PageSize = 1,
            SortOrder = SortOrder.Desc,
            OrderBy = nameof(TruckTicketWellClassification.Date),
            Filters = new()
            {
                { nameof(TruckTicketWellClassification.FacilityId), truckTicket.FacilityId },
                { nameof(TruckTicketWellClassification.SourceLocationId), truckTicket.SourceLocationId },
            },
        };

        var response = _truckTicketWellClassificationUsageEntity.Search(criteria);
        var index = response?.Result.Results?.FirstOrDefault();

        if (index is not null)
        {
            truckTicket.WellClassification = index.WellClassification;
        }
    }
}
