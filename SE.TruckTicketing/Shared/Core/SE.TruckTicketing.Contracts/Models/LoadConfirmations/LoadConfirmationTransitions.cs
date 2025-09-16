using System.Collections.Generic;
using System.Collections.Immutable;

using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.LoadConfirmations;

public static class LoadConfirmationTransitions
{
    private static readonly ImmutableDictionary<LoadConfirmationAction, HashSet<LoadConfirmationStatus>> Transitions = new Dictionary<LoadConfirmationAction, HashSet<LoadConfirmationStatus>>
    {
        [LoadConfirmationAction.SendLoadConfirmation] = new()
        {
            LoadConfirmationStatus.Open,
        },
        [LoadConfirmationAction.ResendAdvancedLoadConfirmationSignatureEmail] = new()
        {
            LoadConfirmationStatus.Open,
            LoadConfirmationStatus.PendingSignature,
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.Rejected,
            LoadConfirmationStatus.WaitingForInvoice,
        },
        [LoadConfirmationAction.ResendLoadConfirmationSignatureEmail] = new()
        {
            LoadConfirmationStatus.Open,
            LoadConfirmationStatus.PendingSignature,
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.Rejected,
            LoadConfirmationStatus.WaitingForInvoice,
        },
        [LoadConfirmationAction.ResendFieldTickets] = new()
        {
            LoadConfirmationStatus.Open,
            LoadConfirmationStatus.PendingSignature,
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.Rejected,
            LoadConfirmationStatus.WaitingForInvoice,
        },
        [LoadConfirmationAction.ApproveSignature] = new()
        {
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.PendingSignature,
        },
        [LoadConfirmationAction.RejectSignature] = new()
        {
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.PendingSignature,
        },
        [LoadConfirmationAction.MarkLoadConfirmationAsReady] = new()
        {
            LoadConfirmationStatus.Open,
            LoadConfirmationStatus.PendingSignature,
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.Rejected,
        },
        [LoadConfirmationAction.VoidLoadConfirmation] = new()
        {
            LoadConfirmationStatus.Open,
            LoadConfirmationStatus.PendingSignature,
            LoadConfirmationStatus.WaitingSignatureValidation,
            LoadConfirmationStatus.Rejected,
            LoadConfirmationStatus.WaitingForInvoice,
        },
    }.ToImmutableDictionary();

    public static bool IsAllowed(LoadConfirmationStatus currentStatus, LoadConfirmationAction action)
    {
        // the action should be defined in transitions and it is allowed to transition from the current status
        return Transitions.TryGetValue(action, out var allowedStates) && allowedStates.Contains(currentStatus);
    }
}
