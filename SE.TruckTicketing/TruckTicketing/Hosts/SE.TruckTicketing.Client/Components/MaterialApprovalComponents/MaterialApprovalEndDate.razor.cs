using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using System.Net.Http;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.JSInterop;

using Trident.UI.Blazor;
using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Components.Grid;
using Trident.UI.Blazor.Components.Modals;
using Trident.UI.Blazor.Components.Grid.Filters;
using Trident.UI.Blazor.Components.Forms;
using Trident.UI.Blazor.Components.Security;

using Radzen.Blazor;
using Radzen;

using SE.TruckTicketing.Client;
using SE.TruckTicketing.Client.Components;
using SE.TruckTicketing.Client.Components.UserControls;
using SE.TruckTicketing.Client.Components.GridFilters;
using SE.TruckTicketing.Client.Components.BillingConfigurationComponents;
using SE.TruckTicketing.Client.Components.Facilities;
using SE.TruckTicketing.Client.Components.RadzenExtensions;
using SE.Shared.Common.Extensions;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents
{
    public partial class MaterialApprovalEndDate
    {
        [Parameter]
        public Contracts.Models.Operations.MaterialApproval Model { get; set; }

        private void HandleEndDateChange(DateTimeOffset? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            var dateValue = value.Value;
            var offSetDateValue = new DateTimeOffset(dateValue.Year, dateValue.Month, dateValue.Day, 7, 0, 0, new(0)).ToAlbertaOffset();
            Model.EndDate = offSetDateValue;
        }
    }
}
