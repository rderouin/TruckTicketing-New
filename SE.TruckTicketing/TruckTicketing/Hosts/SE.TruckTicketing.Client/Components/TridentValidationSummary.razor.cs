using System.Collections.Generic;

using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.Client.Components;

public partial class TridentValidationSummary<TModel> : BaseTruckTicketingComponent where TModel : class
{
    private string _responseContent;

    private List<ValidationResult> _validationErrors = new();

    [Parameter]
    public Response<TModel> Response { get; set; }

    protected override void OnParametersSet()
    {
        SetPropertyValue(ref _responseContent, Response?.ResponseContent,
                         new(this, SetValidationErrors));
    }

    private void SetValidationErrors(string responseContent)
    {
        try
        {
            if (string.IsNullOrEmpty(responseContent))
            {
                _validationErrors = new();
            }
            else
            {
                _responseContent = responseContent;
                _validationErrors = JsonConvert.DeserializeObject<List<ValidationResult>>(responseContent);
            }
        }
        catch
        {
            // ignored
        }
    }

    private class ValidationResult
    {
        public string Message { get; set; }

        public string ErrorCode { get; set; }
    }
}
