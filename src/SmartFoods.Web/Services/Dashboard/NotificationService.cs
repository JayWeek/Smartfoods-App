using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Services.Dashboard;

public class NotificationService : INotificationService, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly DashboardStateHub _stateHub;
    private readonly IMemoryCache _memoryCache;

    private Guid? _currentCircuitUserId;

    public NotificationService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        DashboardStateHub stateHub,
        IMemoryCache memoryCache)
    {
        _dbContextFactory = dbContextFactory;
        _stateHub = stateHub;
        _memoryCache = memoryCache;

        _stateHub.OnDashboardChanged += HandleDashboardStateChanged;
    }

    private string GetCacheKey(Guid userId) => $"NotificationLogs_{userId}";

    private void HandleDashboardStateChanged()
    {
        if (_currentCircuitUserId.HasValue)
        {
            ClearCache(_currentCircuitUserId.Value);
        }
    }

    public async Task<List<NotificationLog>> GetLatestNotificationsAsync(Guid userId, int count = 10)
    {
        _currentCircuitUserId = userId;
        string cacheKey = GetCacheKey(userId);

        // FAST PATH: Retrieve instantly from application memory allocation bounds
        if (_memoryCache.TryGetValue(cacheKey, out List<NotificationLog>? cachedLogs) && cachedLogs != null)
        {
            return cachedLogs;
        }

        // SLOW PATH: Hit database context layers directly
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var logs = await dbContext.NotificationLogs
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.NotificationDate)
            .Take(count)
            .ToListAsync();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(15))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        _memoryCache.Set(cacheKey, logs, cacheOptions);

        return logs;
    }

    public void ClearCache()
    {
        if (_currentCircuitUserId.HasValue)
        {
            ClearCache(_currentCircuitUserId.Value);
        }
    }

    private void ClearCache(Guid userId)
    {
        _memoryCache.Remove(GetCacheKey(userId));
    }

    public void Dispose()
    {
        _stateHub.OnDashboardChanged -= HandleDashboardStateChanged;
    }
}