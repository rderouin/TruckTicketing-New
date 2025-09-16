using Microsoft.AspNetCore.Components;

using CreditStatusEnum = SE.Shared.Common.Lookups.CreditStatus;

namespace SE.TruckTicketing.Client.Components.Accounts;

public partial class CreditStatus
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public CreditStatusEnum Status { get; set; }

    public string CreditStatusStyle(CreditStatusEnum creditStatus)
    {
        switch (creditStatus)
        {
            case CreditStatusEnum.Approved:
                return "text-success";

            case CreditStatusEnum.Denied:
                return "text-danger";

            case CreditStatusEnum.Closed:
                return "text-secondary";

            case CreditStatusEnum.Pending:
                return "text-warning";

            case CreditStatusEnum.ProvisionalApproval:
                return "text-warning";

            case CreditStatusEnum.RequiresRenewal:
                return "text-warning";

            default:
                return "text-secondary";
        }
    }
}
