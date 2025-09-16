using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Security;
using Trident.Api.Search;
using Trident.Contracts.Api.Client;
using Trident.UI.Blazor.Components.Grid;

namespace SE.TruckTicketing.Client.Pages.EmailTemplates;

public partial class Index : BaseTruckTicketingComponent
{
    private SearchResultsModel<EmailTemplate, SearchCriteriaModel> _emailTemplates = new();

    private PagableGridView<EmailTemplate> _grid;

    private GridFiltersContainer _gridFilterContainer;

    private bool _isLoading;

    [Inject]
    private IServiceProxyBase<EmailTemplate, Guid> EmailTemplateService { get; set; }

    private bool HasEmailTemplateWritePermission => HasWritePermission(Permissions.Resources.EmailTemplate);

    private string AddEmailTemplateLink_Css => GetLink_CssClass(HasEmailTemplateWritePermission);

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _gridFilterContainer.Reload();
        }
    }

    private async Task LoadEmailTemplates(SearchCriteriaModel criteria)
    {
        _isLoading = true;
        _emailTemplates = await EmailTemplateService.Search(criteria) ?? _emailTemplates;
        _isLoading = false;
        StateHasChanged();
    }

    private async Task DeleteButton_Click(EmailTemplate model)
    {
        const string msg = "Are you sure you want to delete this record?";
        const string title = "Delete Email Template";
        var deleteConfirmed = await DialogService.Confirm(msg, title,
                                                          new()
                                                          {
                                                              OkButtonText = "Delete",
                                                              CancelButtonText = "Cancel",
                                                          });

        if (deleteConfirmed.GetValueOrDefault())
        {
            await EmailTemplateService.Delete(model);
            await _grid.ReloadGrid();
        }
    }
}
