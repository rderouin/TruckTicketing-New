using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;

using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Security;

using Trident.UI.Blazor.Components;
using Trident.UI.Blazor.Models;

namespace SE.TruckTicketing.Client.Components;

public class BaseTruckTicketingComponent : BaseRazorComponent
{
    public bool IsLoading { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object> Attributes { get; set; }

    [CascadingParameter(Name = "NavigationHistoryManager")]
    protected NavigationHistoryManager NavigationHistoryManager { get; set; }

    [CascadingParameter(Name = "Application")]
    protected ApplicationContext ApplicationContext { get; set; }

    [Inject]
    protected ITruckTicketingAuthorizationService AuthorizationService { get; set; }

    public async Task WithLoadingScreen(Func<Task> bodyFunc)
    {
        try
        {
            IsLoading = true;
            await bodyFunc();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void WithLoadingScreen(Action bodyAction)
    {
        try
        {
            IsLoading = true;
            bodyAction();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public bool IsAuthorizedFor(string resource, string operation)
    {
        var user = ApplicationContext?.User?.Principal;
        if (user is null)
        {
            return false;
        }

        return AuthorizationService?.HasPermission(user, resource, operation) ?? false;
    }

    public bool HasWritePermission(string resource)
    {
        return IsAuthorizedFor(resource, Permissions.Operations.Write);
    }

    public string GetLink_CssClass(bool isLinkEnabled)
    {
        if (isLinkEnabled)
        {
            return string.Empty;
        }

        return "rz-link-disabled";
    }

    protected void SetPropertyValue<T>(ref T existingValue, T newValue, EventCallback<T> callback)
    {
        if (EqualityComparer<T>.Default.Equals(existingValue, newValue))
        {
            return;
        }

        existingValue = newValue;
        callback.InvokeAsync(newValue);
    }

    protected async Task RunMagicalForceUpdate<T>(Func<T> get, Action<T> set, T actual, T magical)
    {
        // is target value the same?
        if (Comparer<T>.Default.Compare(get(), actual) == 0)
        {
            // let Blazor digest this!
            set(magical);
            await Task.Delay(1);
        }

        // set the real value
        set(actual);
        StateHasChanged();
    }

    protected string ClassNames(string unconditional, params (string className, bool include)[] classNames)
    {
        var classes = string.Join(" ", classNames.Where(p => p.include).Select(p => p.className));
        if (string.IsNullOrWhiteSpace(unconditional))
        {
            return classes;
        }

        return $"{unconditional} {classes}";
    }
}
