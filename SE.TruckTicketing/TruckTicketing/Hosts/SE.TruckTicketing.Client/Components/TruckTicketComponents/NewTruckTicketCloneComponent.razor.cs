using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using Radzen;

using SE.TruckTicketing.Client.Pages.TruckTickets.New;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Api.Search;
using Trident.Extensions;
using Trident.UI.Blazor.Components;

namespace SE.TruckTicketing.Client.Components.TruckTicketComponents;

public partial class NewTruckTicketCloneComponent : BaseRazorComponent
{
    public enum TruckTicketCloneList
    {
        Unspecified = default,

        [Description("Clone to New")]
        New = 1,

        [Description("Clone to Stub")]
        Stub = 2,
    }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private TruckTicketExperienceViewModel ViewModel { get; set; }

    [Parameter]
    public TruckTicket Model { get; set; }

    private Guid StubTicketId { get; set; }

    private bool Disable => SelectedCloneOption == default || (SelectedCloneOption == TruckTicketCloneList.Stub && SelectedStubTruckTicket == default);

    private TruckTicket SelectedStubTruckTicket { get; set; }

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
        CloneToStub = selectedOption == TruckTicketCloneList.Stub;
    }

    private void SelectStubTicket(TruckTicket selectStubTicket)
    {
        SelectedStubTruckTicket = selectStubTicket;
    }

    private void HandleCancel()
    {
        DialogService.Close();
    }

    private async Task HandleSubmit()
    {
        if (SelectedCloneOption == TruckTicketCloneList.Stub && SelectedStubTruckTicket != default)
        {
            await CloneTruckTicket(SelectedStubTruckTicket);
        }
        else
        {
            await CloneTruckTicket(null);
        }

        DialogService.Close();
    }

    private async Task CloneTruckTicket(TruckTicket finalTruckTicket)
    {
        //clone from source ticket
        var sourceTruckTicket = ViewModel.TruckTicketBackup.Clone();
        TruckTicket destTruckTicket;
        //Clone to new ticket
        if (finalTruckTicket == null)
        {
            destTruckTicket = new();
            NotificationService.Notify(NotificationSeverity.Success, "Truck Ticket Cloned to New Ticket");
        }
        else //clone to existing stub
        {
            destTruckTicket = finalTruckTicket;
            NotificationService.Notify(NotificationSeverity.Success, $"Stub Ticket {finalTruckTicket.TicketNumber} updated");
        }

        CopySelectedValues(sourceTruckTicket, destTruckTicket);
        await ViewModel.Initialize(destTruckTicket);

        StateHasChanged();
    }

    private void CopySelectedValues(TruckTicket sourceTruckTicket, TruckTicket destTruckTicket)
    {
        if (destTruckTicket.Status == TruckTicketStatus.New || (destTruckTicket.Status == TruckTicketStatus.Stub && destTruckTicket.FacilityServiceId == default))
        {
            //Facility
            destTruckTicket.FacilityId = sourceTruckTicket.FacilityId;
            destTruckTicket.FacilityName = sourceTruckTicket.FacilityName;
            destTruckTicket.FacilityType = sourceTruckTicket.FacilityType;
            destTruckTicket.FacilityLocationCode = sourceTruckTicket.FacilityLocationCode;
            destTruckTicket.SiteId = sourceTruckTicket.SiteId;
            destTruckTicket.CountryCode = sourceTruckTicket.CountryCode;
            destTruckTicket.LegalEntity = sourceTruckTicket.LegalEntity;

            //FacilityService
            destTruckTicket.FacilityServiceSubstanceId = sourceTruckTicket.FacilityServiceSubstanceId;
            destTruckTicket.SubstanceId = sourceTruckTicket.SubstanceId;
            destTruckTicket.SubstanceName = sourceTruckTicket.SubstanceName;
            destTruckTicket.WasteCode = sourceTruckTicket.WasteCode;
            destTruckTicket.ServiceTypeId = sourceTruckTicket.ServiceTypeId;
            destTruckTicket.ServiceType = sourceTruckTicket.ServiceType;
            destTruckTicket.FacilityServiceId = sourceTruckTicket.FacilityServiceId;
            destTruckTicket.UnitOfMeasure = sourceTruckTicket.UnitOfMeasure;
            destTruckTicket.Stream = sourceTruckTicket.Stream;
            destTruckTicket.FacilityStreamRegulatoryCode = sourceTruckTicket.FacilityStreamRegulatoryCode;

            destTruckTicket.WellClassification = sourceTruckTicket.WellClassification;
        }

        //SourceLocation
        destTruckTicket.SourceLocationId = sourceTruckTicket.SourceLocationId;
        destTruckTicket.SourceLocationName = sourceTruckTicket.SourceLocationName;
        destTruckTicket.SourceLocationFormatted = sourceTruckTicket.SourceLocationFormatted;
        destTruckTicket.SourceLocationCode = sourceTruckTicket.SourceLocationCode;
        destTruckTicket.GeneratorId = sourceTruckTicket.GeneratorId;
        destTruckTicket.GeneratorName = sourceTruckTicket.GeneratorName;

        //LoadDate
        destTruckTicket.LoadDate = sourceTruckTicket.LoadDate;

        //Trucking Company
        destTruckTicket.TruckingCompanyId = sourceTruckTicket.TruckingCompanyId;
        destTruckTicket.TruckingCompanyName = sourceTruckTicket.TruckingCompanyName;
    }
}
