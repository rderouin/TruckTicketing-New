using System.Threading.Tasks;

using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Utilities;

public class PagableGridExporter<TItem>
{
    private readonly IDataExportService _dataExportService;

    private readonly PagableGridView<TItem> _grid;

    public PagableGridExporter(PagableGridView<TItem> grid, IDataExportService dataExportService)
    {
        _grid = grid;
        _dataExportService = dataExportService;
    }

    public async Task Export(string filename)
    {
        var data = _grid.Results.Results
                        .AsExpandoObjects()
                        .SelectForExport(_grid.ColumnDefinitionList);

        await _dataExportService.Export(filename, data);
    }
}
