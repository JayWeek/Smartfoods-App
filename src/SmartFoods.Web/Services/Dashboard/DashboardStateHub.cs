namespace SmartFoods.Web.Services.Dashboard;

public class DashboardStateHub
{
    // Reactive multicast delegate event
    public event Action? OnDashboardChanged;

    /// <summary>
    /// Notifies all active listening UI panels globally to synchronize their data states.
    /// </summary>
    public void NotifyStateChanged()
    {
        OnDashboardChanged?.Invoke();
    }
}
