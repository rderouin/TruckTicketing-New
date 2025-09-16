using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Contracts;
using Trident.Contracts.Api;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public interface ITruckTicketManager : IManager<Guid, TruckTicketEntity>
{
    Task CreatePrePrintedTruckTicketStubs(Guid facilityId, int count);

    Task CreatePrePrintedTruckTicketStubs(Guid facilityId, int count, Func<IEnumerable<TruckTicketEntity>, Task> beforeCreate);

    Task<string> GetAttachmentUploadUri(string blobName);

    Task<string> GetAttachmentDownloadUri(string path);

    Task<Stream> GetFileStream(string blobName);

    Task<List<BillingConfigurationEntity>> GetMatchingBillingConfigurations(TruckTicketEntity truckTicket);

    Task<bool> ConfirmCustomerOnTickets(IEnumerable<TruckTicketEntity> splitTruckTicketList);

    Task<IEnumerable<TruckTicketEntity>> SplitTruckTicket(IEnumerable<TruckTicketEntity> splitTruckTickets, CompositeKey<Guid> truckTicketKey);

    Task<(TruckTicketAttachmentEntity attachment, string uri)> GetUploadUrl(CompositeKey<Guid> truckTicketKey, string filename, string contentType);

    Task<Uri> GetDownloadUrl(CompositeKey<Guid> truckTicketKey, Guid attachmentId);

    Task<TruckTicketEntity> MarkFileUploaded(CompositeKey<Guid> truckTicketKey, Guid attachmentId);

    Task<TruckTicketEntity> RemoveAttachmentOnTruckTicket(CompositeKey<Guid> truckTicketKey, Guid attachmentId);
}
