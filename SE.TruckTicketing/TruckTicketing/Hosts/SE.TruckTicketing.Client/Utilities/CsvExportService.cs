using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using BlazorDownloadFile;

using CsvHelper;

namespace SE.TruckTicketing.Client.Utilities;

public class CsvExportService : ICsvExportService
{
    private readonly IBlazorDownloadFileService _blazorDownloadFileService;

    public CsvExportService(IBlazorDownloadFileService blazorDownloadFileService)
    {
        _blazorDownloadFileService = blazorDownloadFileService;
    }

    public async Task Export(string filename, IEnumerable<dynamic> data)
    {
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(data);

        var bytes = Encoding.UTF8.GetBytes(writer.ToString());

        await _blazorDownloadFileService.DownloadFile(filename, bytes, "application/octet-stream");
    }
}
