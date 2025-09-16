using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SE.TruckTicketing.Client.Pages.TruckTickets.New;

public class TruckTicketWorkflowManager
{
    private Dictionary<string, CancellationTokenSource> _cancellationTokenSources;

    private string _currentKey = String.Empty;

    private ITruckTicketWorkflow[] _workflows;

    public void RegisterWorkflows(ICollection<ITruckTicketWorkflow> workflows)
    {
        _workflows = workflows.ToArray();
        _cancellationTokenSources = new();
    }

    public async ValueTask InitializeWorkflows(TruckTicketExperienceViewModel viewModel)
    {
        viewModel.IsInitializing = true;
        viewModel.TriggerStateChanged();
        var key = viewModel?.TruckTicket?.TicketNumber + "";

        SetCurrentTicket(key);
        var ranToCompletion = await RunTasksParallel(key, _workflows.Select(workflow =>
                                                                            {
                                                                                Console.WriteLine("Initializing {0} for {1}", workflow.GetType().Name, key);
                                                                                return workflow.Initialize(viewModel);
                                                                            }));

        if (ranToCompletion)
        {
            viewModel.IsInitializing = false;
            viewModel.TriggerStateChanged();
        }
    }

    public async ValueTask TriggerWorkflows(TruckTicketExperienceViewModel viewModel)
    {
        var key = viewModel?.TruckTicket?.TicketNumber + "";

        SetCurrentTicket(key);
        await RunTasks(key, _workflows.Select(workflow =>
                                              {
                                                  Console.WriteLine("Initializing {0} for {1}", workflow.GetType().Name, key);
                                                  return RunWorkflow(workflow, viewModel);
                                              }));
    }

    private async ValueTask RunWorkflow(ITruckTicketWorkflow workflow, TruckTicketExperienceViewModel viewModel)
    {
        Console.WriteLine("WF: Checking to see if we run {0} {1}", workflow.GetType().Name, viewModel.TruckTicket?.TicketNumber);
        if (await workflow.ShouldRun(viewModel))
        {
            Console.WriteLine("WF: Running {0}", workflow.GetType().Name);
            await workflow.Run(viewModel);
        }
    }

    private void SetCurrentTicket(string key)
    {
        Console.WriteLine("Setting current ticket: {0} new: {1}", _currentKey ?? "not-set", key);
        if (_currentKey != key)
        {
            Console.WriteLine("Checking if we need to cancel key {0}", _currentKey ?? "not-set");
            if (_cancellationTokenSources.TryGetValue(_currentKey, out var currentCts))
            {
                Console.WriteLine("Cancelling key {0}", _currentKey ?? "not-set");
                currentCts.Cancel();
            }

            _currentKey = key;
        }

        _cancellationTokenSources[key] = new();
    }

    private async ValueTask<bool> RunTasks(string key, IEnumerable<ValueTask> tasks)
    {
        var cts = _cancellationTokenSources[key];
        var token = cts.Token;

        foreach (var task in tasks)
        {
            if (token.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation executed for key {0}", key);
                return false;
            }

            await task;

            if (token.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation executed for key {0}", key);
                return false;
            }
        }

        return true;
    }

    private async ValueTask<bool> RunTasksParallel(string key, IEnumerable<ValueTask> tasks)
    {
        var cts = _cancellationTokenSources[key];
        var token = cts.Token;

        await Task.WhenAll(tasks.Select(async t => await t));

        return true;
    }
}
