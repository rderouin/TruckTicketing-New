using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Trident.Contracts.Changes;

namespace Trident.EFCore.Changes
{
    public interface IChangeObserver
    {
        Task<List<ChangeModel>> GetChanges(DbContext dbContext);

        Task Publish(List<ChangeModel> changes);
    }
}
