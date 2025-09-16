using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Routes;

namespace SE.TruckTicketing.Client.Components.SalesManagement;

public partial class SalesManagementButtons : BaseTruckTicketingComponent
{
    [Parameter]
    public SalesManagementButtonFlag SalesManagementButtonFlag { get; set; }

    [Parameter]
    public Guid FacilityId { get; set; } = Guid.Empty;

    [Parameter]
    public IEnumerable<Guid> FacilityIds { get; set; }

    private string SalesLinesButtonClass { get; set; }

    private string LoadConfirmationsButtonClass { get; set; }

    private string InvoicesButtonClass { get; set; }

    protected override void OnParametersSet()
    {
        switch (SalesManagementButtonFlag)
        {
            case SalesManagementButtonFlag.SalesLines:
                SalesLinesButtonClass = "btn-outline-primary active";
                LoadConfirmationsButtonClass = "btn-outline-secondary";
                InvoicesButtonClass = "btn-outline-secondary";
                break;
            case SalesManagementButtonFlag.LoadConfirmations:
                LoadConfirmationsButtonClass = "btn-outline-primary active";
                SalesLinesButtonClass = "btn-outline-secondary";
                InvoicesButtonClass = "btn-outline-secondary";
                break;
            case SalesManagementButtonFlag.Invoices:
                InvoicesButtonClass = "btn-outline-primary active";
                SalesLinesButtonClass = "btn-outline-secondary";
                LoadConfirmationsButtonClass = "btn-outline-secondary";
                break;
        }
    }

    private void OpenSalesLines()
    {
        var url = (FacilityIds != null && FacilityIds.Any()) ?
                      Routes.SalesManagement.Facility.Replace(Routes.SalesManagement.Params.FacilityId, HttpUtility.UrlEncode(JsonConvert.SerializeObject(FacilityIds))) :
                      Routes.SalesManagement.Base;

        NavigationManager.NavigateTo(url);
    }

    private void OpenLoadConfirmation()
    {
        var url = (FacilityIds != null && FacilityIds.Any()) ?
                      Routes.LoadConfirmation.Facility.Replace(Routes.LoadConfirmation.Params.FacilityId, HttpUtility.UrlEncode(JsonConvert.SerializeObject(FacilityIds))) :
                      Routes.LoadConfirmation.Base;

        NavigationManager.NavigateTo(url);
    }

    private void OpenInvoices()
    {
        var url = (FacilityIds != null && FacilityIds.Any()) ?
                      Routes.Invoice.Facility.Replace(Routes.Invoice.Parameters.FacilityId, HttpUtility.UrlEncode(JsonConvert.SerializeObject(FacilityIds))) :
                      Routes.Invoice.Base;

        NavigationManager.NavigateTo(url);
    }
}
