using System;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public interface ITruckTicketAttachmentManager
{
    Task<(TruckTicketAttachmentEntity attachment, string uri)> GetUploadUrl(CompositeKey<Guid> truckTicketKey, string filename, string contentType);

    Task<Uri> GetDownloadUrl(CompositeKey<Guid> truckTicketKey, Guid attachmentId);

    Task<TruckTicketEntity> MarkFileUploaded(CompositeKey<Guid> truckTicketKey, Guid attachmentId);

    Task<TruckTicketEntity> RemoveAttachmentOnTruckTicket(CompositeKey<Guid> truckTicketKey, Guid attachmentId);
}
