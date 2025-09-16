using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Trident.Contracts;

namespace SE.Shared.Domain.Entities.MaterialApproval;

public interface IMaterialApprovalManager : IManager<Guid, MaterialApprovalEntity>
{
    Task<List<string>> GetWasteCodeByFacility(Guid facilityId);

    Task<byte[]> CreateMaterialApprovalPdf(Guid id);
}
