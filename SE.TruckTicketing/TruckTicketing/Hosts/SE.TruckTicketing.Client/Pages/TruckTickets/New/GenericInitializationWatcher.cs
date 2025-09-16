using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.TruckTicket;
using SE.TruckTicketing.UI.Contracts.Services;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public class GenericInitializationWatcher : ComponentBase, ITruckTicketWorkflow
{
    [Inject]
    private ISalesLineWatcher SalesLineWatcher { get; set; }

    [Inject]
    private IBillingConfigurationWatcher BillingConfigurationWatcher { get; set; }

    [Inject]
    private ITruckTicketService TruckTicketService { get; set; }

    public async ValueTask Initialize(TruckTicketExperienceViewModel viewModel)
    {
        //Check ShouldRun for BillingConfiguration; if this is false nothing needs to be called
        //If ShouldRun for BillingConfiguration is true; check if ShouldRun for SalesLine is true
        //Pass variable to function for API to determine which data to return

        BillingConfigurationWatcher.ClearDependencies();
        var shouldRunBillingConfigurations = await BillingConfigurationWatcher.ShouldRun(viewModel);

        var initRequestModel = new TruckTicketInitRequestModel
        {
            TruckTicket = viewModel.TruckTicket,
            ShouldRunBillingConfiguration = shouldRunBillingConfigurations,
            ShouldRunSalesLine = true,
        };

        var truckTicketSalesLineBillingInit = await TruckTicketService.GetTruckTicketInitializationResponse(initRequestModel);
        if (truckTicketSalesLineBillingInit == null)
        {
            return;
        }

        //Initialize BillingConfigurations
        viewModel.BillingConfigurations = truckTicketSalesLineBillingInit.BillingConfigurations;
        if (truckTicketSalesLineBillingInit.IsUpdateBillingConfiguration)
        {
            BillingConfigurationWatcher.SetDependencies(viewModel);
            await BillingConfigurationWatcher.UpdateBillingConfigurations(viewModel);
        }

        //Initialize SalesLines
        viewModel.IsLoadingSalesLines = true;
        viewModel.TriggerStateChanged();
        var existingSalesLines = (ICollection<SalesLine>)truckTicketSalesLineBillingInit.SalesLines;
        existingSalesLines = SalesLineWatcher.DeduplicateSalesLines(existingSalesLines);
        viewModel.IsLoadingSalesLines = false;
        viewModel.TriggerStateChanged();

        var reversedSalesLines = existingSalesLines.Where(salesLine => salesLine.IsReversal || salesLine.IsReversed).ToList();
        if (reversedSalesLines.Any())
        {
            viewModel.SetReversedSalesLines(reversedSalesLines);
        }

        var activeSalesLines = existingSalesLines.Except(reversedSalesLines).ToList();

        if (activeSalesLines.Any())
        {
            await viewModel.SetSalesLines(activeSalesLines);
            return;
        }

        if (await SalesLineWatcher.ShouldRun(viewModel))
        {
            await SalesLineWatcher.Run(viewModel);
        }

        await Task.FromResult(true);
    }

    public ValueTask<bool> ShouldRun(TruckTicketExperienceViewModel viewModel)
    {
        return ValueTask.FromResult(false);
    }

    public ValueTask<bool> Run(TruckTicketExperienceViewModel viewModel)
    {
        return ValueTask.FromResult(false);
    }
}
