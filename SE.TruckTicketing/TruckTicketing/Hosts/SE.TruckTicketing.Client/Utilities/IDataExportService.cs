using System.Collections.Generic;
using System.Threading.Tasks;

namespace SE.TruckTicketing.Client.Utilities;

public interface IDataExportService
{
    Task Export(string filename, IEnumerable<dynamic> data);
}
