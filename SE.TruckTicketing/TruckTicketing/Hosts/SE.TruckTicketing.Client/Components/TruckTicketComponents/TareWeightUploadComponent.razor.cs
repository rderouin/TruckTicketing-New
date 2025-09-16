using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CsvHelper;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

using Radzen;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class TareWeightUploadComponent
{
    private PagableGridView<Contracts.Models.Operations.TruckTicketTareWeightCsv> _gridUploadedData;

    private PagableGridView<Contracts.Models.Operations.TruckTicketTareWeightCsvResult> _gridResultData;

    private readonly string[] _acceptedExtensions = { "csv" };

    private readonly long _limit = 1024 * 1024;

    private SearchResultsModel<TruckTicketTareWeightCsv, SearchCriteriaModel> _uploadedData = new()
    {
        Info = new() { PageSize = 10 },
        Results = new List<TruckTicketTareWeightCsv>(),
    };

    private SearchResultsModel<TruckTicketTareWeightCsvResult, SearchCriteriaModel> _unProcessedData = new();

    private bool _isUploading;

    private string _uploadErrorMessage;

    private string AcceptedExtensionsString => string.Join(", ", _acceptedExtensions.Select(x => $"*.{x}"));

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Inject]
    private ITruckTicketTareWeightService TruckTicketTareWeightService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private ILogger<TruckTicketTareWeightCsv> Logger { get; set; }

    private bool IsProcessComplete { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

    private List<TruckTicketTareWeightCsv> Records { get; set; }

    private List<TruckTicketTareWeightCsvResult> ResultRecords { get; set; }

    private async Task LoadTareWeight(SearchCriteriaModel searchCriteria)
    {
        var myList = Records.ToList();
        var morePages = (searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault())) < myList.Count;
        var results = new SearchResultsModel<TruckTicketTareWeightCsv, SearchCriteriaModel>()
        {
            Results = myList
                     .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault()))
                     .Take(searchCriteria.PageSize.GetValueOrDefault()),
            Info = new SearchResultInfoModel<SearchCriteriaModel>()
            {
                TotalRecords = myList.Count,
                NextPageCriteria = morePages ? new SearchCriteriaModel() { CurrentPage = (searchCriteria.CurrentPage + 1) } : null
            }
        };

        _uploadedData = results;

        await Task.CompletedTask;
    }

    private async Task LoadTareWeightResult(SearchCriteriaModel searchCriteria)
    {
        var myList = ResultRecords.ToList();
        var morePages = (searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault())) < myList.Count;
        var results = new SearchResultsModel<TruckTicketTareWeightCsvResult, SearchCriteriaModel>()
        {
            Results = myList
                     .Skip(searchCriteria.PageSize.GetValueOrDefault() * (searchCriteria.CurrentPage.GetValueOrDefault()))
                     .Take(searchCriteria.PageSize.GetValueOrDefault()),
            Info = new SearchResultInfoModel<SearchCriteriaModel>()
            {
                TotalRecords = myList.Count,
                NextPageCriteria = morePages ? new SearchCriteriaModel() { CurrentPage = (searchCriteria.CurrentPage + 1) } : null
            }
        };

        _unProcessedData = results;

        await Task.CompletedTask;
    }

    private async Task UploadButtonClicked()
    {
        if (_uploadedData.Results.Any())
        {
            _unProcessedData = new()
            {
                Info = new() { PageSize = 10 },
                Results = new List<TruckTicketTareWeightCsvResult>(),
            };

            var response = await TruckTicketTareWeightService.UploadTruckTicketTareWeight(Records);
            NotificationService.Notify(NotificationSeverity.Success, "Tare Weight Data Processing Completed");
            if (response.Any())
            {
                ResultRecords = response.ToList();
                IsProcessComplete = true;
                await _gridResultData.ReloadGrid();
            }
            else
            {
                await HandleCancel();
            }
        }
    }

    private async Task InputFileChange(InputFileChangeEventArgs args)
    {
        if (!IsAcceptedExtension(args.File.Name))
        {
            _uploadErrorMessage = $"Invalid file type, allowed extensions: {AcceptedExtensionsString}";
            return;
        }

        _uploadErrorMessage = null;

        try
        {
            _isUploading = true;
            // upload the file
            await UploadFile(args.File);
        }
        catch (Exception e)
        {
            //consuming exception to notify.
            Logger.LogError(e, "File upload unsuccessful");
            NotificationService.Notify(NotificationSeverity.Error, "File upload unsuccessful");
        }
        finally
        {
            _uploadErrorMessage = $"File upload unsuccessful";
            _isUploading = false;
        }
    }

    private bool IsAcceptedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).Replace(".", "");
        if (!ext.HasText())
        {
            return false;
        }

        return _acceptedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private async Task UploadFile(IBrowserFile file)
    {
        using var stream2 = new MemoryStream();
        var stream = file.OpenReadStream(_limit);
        await stream.CopyToAsync(stream2);
        stream2.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(stream2);

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        Records = csv.GetRecords<TruckTicketTareWeightCsv>().ToList();
        await _gridUploadedData.ReloadGrid();
    }
}
