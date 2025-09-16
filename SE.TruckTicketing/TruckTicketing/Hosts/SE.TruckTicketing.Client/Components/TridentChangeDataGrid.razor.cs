using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.Shared.Common.Changes;
using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Api.Search;
using Trident.Contracts.Configuration;
using Trident.UI.Blazor.Components.Forms;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentChangeDataGrid : BaseTruckTicketingComponent
{
    private ChangeConfiguration _config;

    private PagableGridView<Change> _grid;

    private GridFiltersContainer _gridFiltersContainer;

    private bool _isLoading;

    private SearchResultsModel<Change, SearchCriteriaModel> _results = new()
    {
        Info = new(),
        Results = new List<Change>(),
    };

    private string Title => $"{EntityName} Change History";

    [Parameter]
    public Guid Id { get; set; }

    [Parameter]
    public string EntityType { get; set; }

    [Parameter]
    public string EntityName { get; set; }

    [Parameter]
    public EventCallback<SearchCriteriaModel> OnDataLoading { get; set; }

    [Inject]
    private IServiceBase<Change, string> ServiceProxy { get; set; }

    [Inject]
    public IAppSettings AppSettings { get; set; }

    protected override Task OnInitializedAsync()
    {
        _config = ChangeConfiguration.Load(AppSettings, EntityType);
        return base.OnInitializedAsync();
    }

    protected virtual async Task OnLoadData(SearchCriteriaModel current)
    {
        try
        {
            _isLoading = true;

            // always apply entity filter
            current.AddFilter("DocumentType", $"Change|{EntityType}|{Id}");

            // trigger extra logic
            if (OnDataLoading.HasDelegate)
            {
                await OnDataLoading.InvokeAsync(current);
            }

            // do the search
            _results = await ServiceProxy.Search(current) ?? _results;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFiltersContainer.Reload();
        }
    }

    private SelectOption[] GetFieldOptions()
    {
        var allFieldNames = _config?.DisplayNames ?? new();

        var options = allFieldNames.Select(fn => new SelectOption
        {
            Id = fn.Key,
            Text = fn.Value,
        }).ToArray();

        return options;
    }

    private string FieldNameToDisplayName(string agnosticPath)
    {
        // ensure the field name is provided
        if (string.IsNullOrWhiteSpace(agnosticPath))
        {
            return agnosticPath;
        }

        // try fetching the display name, otherwise, keep the original name if not configured
        return _config?.DisplayNames.TryGetValue(agnosticPath, out var displayName) == true ? displayName : agnosticPath;
    }

    private string FormatValue(string agnosticPath, string rawValue)
    {
        // try finding a formatter based on the agnostic path
        if (agnosticPath.HasText() && _config?.Formatters?.TryGetValue(agnosticPath, out var formatter) == true)
        {
            // a type that is required for parsing, conversion, and formatting
            var clrType = Type.GetType(formatter.ValueType);

            // formatter arguments
            List<string> args = formatter.Format.HasText()
                                    ? new(formatter.Format.Split("|", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                    : new();

            // pick the target formatter
            switch (formatter.Type)
            {
                case "CUSTOM":
                    return ChangeFormatters.FormatCustom(rawValue, clrType, args);

                case "DOTNET":
                    return ChangeFormatters.FormatDotnet(rawValue, clrType, args);
            }
        }

        // keep the value as is
        return rawValue;
    }

    private string FormatTag(string tag, string fieldLocation, string fieldName)
    {
        var match = Regex.Match(fieldLocation, @"\[\d+\]", RegexOptions.Compiled);
        return match.Success && fieldName.HasText() ? tag : string.Empty;
    }
}
