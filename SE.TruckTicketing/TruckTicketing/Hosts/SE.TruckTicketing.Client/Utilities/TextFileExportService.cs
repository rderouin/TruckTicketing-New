using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper.Internal;

using BlazorDownloadFile;

using CsvHelper;

using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Client.Utilities;

public class TextFileExportService : ITextFileExportService
{
    private IBlazorDownloadFileService _blazorDownloadFileService;

    public TextFileExportService(IBlazorDownloadFileService blazorDownloadFileService)
    {
        _blazorDownloadFileService = blazorDownloadFileService;
    }

    public async Task Export(string filename, StringBuilder data)
    {
        var bytesData = Encoding.UTF8.GetBytes(data.ToString().HasText() ? data.ToString() : "No data available");
        await _blazorDownloadFileService.DownloadFile(filename, bytesData, "text/plain");
    }

    public async Task Export(string filename, string data)
    {
        await _blazorDownloadFileService.DownloadFileFromText(filename, data, Encoding.UTF8, "text/plain");
    }
}
