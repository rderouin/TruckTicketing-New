using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public interface ILoadConfirmationPdfRenderer
{
    Task<byte[]> RenderLoadConfirmationPdf(LoadConfirmationEntity loadConfirmation);

    Task<byte[]> RenderAdHocLoadConfirmationPdf(LoadConfirmationAdhocModel adhocModel);
}
