using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SE.TruckTicketing.Client.Utilities;

public interface ITextFileExportService
{
    Task Export(string filename, StringBuilder data);
}
