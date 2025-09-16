using System.ComponentModel;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public enum LoadConfirmationHashStrategy
{
    Unknown,

    [Description("v.1 - L16")]
    Version1L16,

    [Description("v.2 - L6")]
    Version2L6,
}
