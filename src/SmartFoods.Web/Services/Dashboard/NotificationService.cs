using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Services.Dashboard;

public class NotificationService : INotificationService, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly DashboardStateHub _stateHub;

    // Circuit-scoped private cache storage primitives
    private List<NotificationLog>? _cachedNotifications;
    private Guid? _cachedUserId;

    public NotificationService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        DashboardStateHub stateHub)
    {
        _dbContextFactory = dbContextFactory;
        _stateHub = stateHub;

        // Wire up the state hub event listener to clear cache instantly on changes
        _stateHub.OnDashboardChanged += ClearCache;
    }

    public async Task<List<NotificationLog>> GetLatestNotificationsAsync(Guid userId, int count = 10)
    {
        // FAST PATH: Return memory store instantly if it belongs to this active user session
        if (_cachedNotifications != null && _cachedUserId == userId)
        {
            return _cachedNotifications;
        }

        // SLOW PATH: Query database context and update session cache bounds
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var logs = await dbContext.NotificationLogs
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.NotificationDate)
            .Take(count)
            .ToListAsync();

        _cachedUserId = userId;
        _cachedNotifications = logs;

        return logs;
    }

    public void ClearCache()
    {
        _cachedNotifications = null;
        _cachedUserId = null;
    }

    public void Dispose()
    {
        // Unsubscribe securely to eliminate any potential circuit memory footprint leaks
        _stateHub.OnDashboardChanged -= ClearCache;
    }
}