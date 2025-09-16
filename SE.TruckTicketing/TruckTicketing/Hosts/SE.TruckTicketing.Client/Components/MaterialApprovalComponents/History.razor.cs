using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Client.Components.MaterialApprovalComponents;

public partial class History
{

    // TODO: per bug # 9416 to be completed with #9420

    private EditContext _editContext;

    [Parameter]
    public MaterialApproval model { get; set; }

    [Parameter]
    public EventCallback<FieldIdentifier> OnContextChange { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _editContext = new(model);
        _editContext.OnFieldChanged += OnEditContextFieldChanged;

        await base.OnInitializedAsync();
    }

    private void OnEditContextFieldChanged(object sender, FieldChangedEventArgs e)
    {
        OnContextChange.InvokeAsync(e.FieldIdentifier);
    }
}
