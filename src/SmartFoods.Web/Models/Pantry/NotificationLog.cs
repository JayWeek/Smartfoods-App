using SmartFoods.Web.Models.Base;
using SmartFoods.Web.Models.Identity;

namespace SmartFoods.Web.Models.Pantry;

public class NotificationLog : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateOnly NotificationDate { get; set; } // Tracks the specific day (e.g. 2026-06-09)
    public string Title { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public bool IsEmailSent { get; set; }
    public DateTime SentAt { get; set; }
}
