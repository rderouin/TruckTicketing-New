using System;
using System.ComponentModel;

using Microsoft.AspNetCore.Components;

using Trident.UI.Blazor.Components;

using SE.TruckTicketing.Contracts.Lookups;

using Trident.Api.Search;

using SE.TruckTicketing.Contracts.Models.Operations;

using System.Threading.Tasks;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents
{
    public partial class TruckTicketCloneComponent : BaseRazorComponent
    {
        [Parameter]
        public EventCallback<TruckTicket> CloneTruckTicket { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        [Parameter]
        public TruckTicket Model { get; set; }

        private bool Disable => SelectedCloneOption == default || SelectedCloneOption == TruckTicketCloneList.Stub && SelectedStubTruckTicket == default;

        private Guid StubTicketId { get; set; }

        private TruckTicket SelectedStubTruckTicket { get; set; }

        public enum TruckTicketCloneList
        {
            Unspecified = default,

            [Description("Clone to New")]
            New = 1,

            [Description("Clone to Stub")]
            Stub = 2,
        }

        private bool CloneToStub { get; set; }

        private TruckTicketCloneList SelectedCloneOption { get; set; } = TruckTicketCloneList.New;

        private void LoadStubTruckTickets(SearchCriteriaModel criteria)
        {
            criteria.AddFilter(nameof(TruckTicket.Status), TruckTicketStatus.Stub.ToString());
            criteria.AddFilter(nameof(TruckTicket.TruckTicketType), TruckTicketType.WT.ToString());
        }

        private void OnTruckTicketCloneListStateChange(TruckTicketCloneList selectedOption)
        {
            SelectedCloneOption = selectedOption;
            CloneToStub = (selectedOption == TruckTicketCloneList.Stub);
        }

        private void SelectStubTicket(TruckTicket selectStubTicket)
        {
            SelectedStubTruckTicket = selectStubTicket;
        }

        public async Task HandleCancel()
        {
            await OnCancel.InvokeAsync();
        }

        private void HandleSubmit()
        {
            if (!CloneTruckTicket.HasDelegate)
            {
                return;
            }

            if (SelectedCloneOption == TruckTicketCloneList.Stub && SelectedStubTruckTicket != default)
            {
                CloneTruckTicket.InvokeAsync(SelectedStubTruckTicket);
            }
            else
            {
                CloneTruckTicket.InvokeAsync(null);
            }
        }
    }
}
