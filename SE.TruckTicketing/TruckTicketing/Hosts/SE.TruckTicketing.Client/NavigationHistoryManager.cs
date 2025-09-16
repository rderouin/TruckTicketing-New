using System.Collections.Generic;

namespace SE.TruckTicketing.Client;

public class NavigationHistoryManager
{
    private readonly List<string> _history = new();

    public void AddLocation(string location)
    {
        _history.Add(location);
    }

    public string GetReturnUrl(string defaultReturnUrl = "/")
    {
        if (_history.Count > 1)
        {
            return _history[^2];
        }

        return defaultReturnUrl;
    }
}
