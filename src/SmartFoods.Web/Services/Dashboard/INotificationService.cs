using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Services.Dashboard;

public interface INotificationService
{
    Task<List<NotificationLog>> GetLatestNotificationsAsync(Guid userId, int count = 10);
    void ClearCache();
}