using System.Linq;
using System.Threading.Tasks;

using Stateless;
using Stateless.Graph;

namespace SE.TridentContrib.Extensions.Workflows;

public abstract class SelfAdvancingWorkflow<TState, TTrigger>
{
    // the workflow state machine
    private StateMachine<TState, TTrigger> _stateMachine;

    // terminal states beyond which no transition should be done
    protected abstract TState[] TerminalStates { get; }

    // is the current state final?
    private bool IsInTerminalState => TerminalStates.Contains(_stateMachine.State);

    // create and configure a new state machine
    protected abstract Task<StateMachine<TState, TTrigger>> ConfigureStateMachineAsync(TState initialState);

    public string GetUmlDotGraph()
    {
        return UmlDotGraph.Format(_stateMachine.GetInfo());
    }

    protected async Task StartAsync(TState initialState)
    {
        // init the state machine
        _stateMachine = await ConfigureStateMachineAsync(initialState);
        _stateMachine.OnTransitionCompletedAsync(OnTransitionCompletedAsync);

        // start the workflow
        await _stateMachine.ActivateAsync();
        await AdvanceAsync();
        await _stateMachine.DeactivateAsync();
    }

    private async Task AdvanceAsync()
    {
        // finish the workflow if it's in a terminal state
        if (IsInTerminalState)
        {
            return;
        }

        // continue the workflow, only a single path should exist
        await _stateMachine.FireAsync(_stateMachine.PermittedTriggers.Single());
    }

    private async Task OnTransitionCompletedAsync(StateMachine<TState, TTrigger>.Transition transition)
    {
        // auto-advance upon completing the transition
        await AdvanceAsync();
    }
}
