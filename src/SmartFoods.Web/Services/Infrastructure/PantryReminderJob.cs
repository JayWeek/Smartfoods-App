using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.Pantry;
using SmartFoods.Web.Services.Interfaces;
using System.Text;

namespace SmartFoods.Web.Services.Infrastructure;

public class PantryReminderJob
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEmailService _emailService;

    public PantryReminderJob(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IEmailService emailService)
    {
        _dbContextFactory = dbContextFactory;
        _emailService = emailService;
    }

    /// <summary>
    /// Sweeps all users, checks for critical expiries, links a cache recipe, and delivers alerts safely.
    /// </summary>
    public async Task SendDailyExpiryRemindersAsync()
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var criticalHorizon = today.AddDays(3); // Rule 1: 3-day critical window

        // 1. Fetch all active users containing their pantry food supplies
        var users = await dbContext.Users
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .ToListAsync();

        foreach (var user in users)
        {
            if (user.Pantry == null) continue;

            // 2. Idempotency Check: Verify if an alert went out for this user today
            var alreadyNotified = await dbContext.NotificationLogs
                .AnyAsync(n => n.UserId == user.Id && n.NotificationDate == today);

            if (alreadyNotified) continue;

            // 3. Extract items expiring within 3 days
            var criticalItems = user.Pantry.FoodItems
                .Where(f => f.ExpiryDate >= today && f.ExpiryDate <= criticalHorizon)
                .ToList();

            if (!criticalItems.Any()) continue; // No critical items = no email sent

            // 4. Rule 3: Pick one recipe from local cache matching ANY critical item name
            var itemNames = criticalItems.Select(c => c.Name.Trim().ToLowerInvariant()).ToList();
            
            var matchedRecipe = await dbContext.RecipeIngredients
                .AsNoTracking()
                .Include(i => i.Recipe)
                .Where(i => itemNames.Contains(i.Name))
                .Select(i => i.Recipe)
                .FirstOrDefaultAsync();

            // 5. Construct the Premium HTML Notification Message Template
            // 5. Construct the Premium HTML Notification Message Template (Email-Safe & Blazor-Optimized)
            var emailBuilder = new StringBuilder();

            emailBuilder.Append("<div style=\"font-family: 'Inter', -apple-system, system-ui, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #211c2b; max-width: 600px; margin: 0 auto; padding: 10px;\">");
            emailBuilder.Append($"<h2 style=\"margin: 0 0 8px 0; font-size: 1.4rem; font-weight: 800; color: #4a148c;\">Hello, {user.Name}!</h2>");
            emailBuilder.Append("<p style=\"margin: 0 0 20px 0; font-size: 0.95rem; color: #6e657b; line-height: 1.5;\">This is a summary of tracking items in your pantry requiring consumption within your critical 3-day window:</p>");

            // Use a clean table structures for guaranteed cross-client support instead of flex grids
            emailBuilder.Append("<table cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"width: 100%; border-collapse: separate; margin-bottom: 25px;\">");
            foreach (var item in criticalItems)
            {
                var daysLeft = item.ExpiryDate.DayNumber - today.DayNumber;
                
                string badgeBg = daysLeft == 0 ? "#ffebee" : "#fff3e0";
                string badgeText = daysLeft == 0 ? "#c62828" : "#e65100";
                string dayLabel = daysLeft == 0 ? "Expires Today" : daysLeft == 1 ? "1 Day Left" : $"{daysLeft} Days Remaining";

                emailBuilder.Append($@"
                    <tr>
                        <td style=""padding: 14px 16px; background-color: #fbf9ff; border-top: 1px solid rgba(124, 77, 255, 0.08); border-bottom: 1px solid rgba(124, 77, 255, 0.08); border-left: 1px solid rgba(124, 77, 255, 0.08); border-right: 1px solid rgba(124, 77, 255, 0.08); border-radius: 12px; margin-bottom: 10px; display: block;"">
                            <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%;"">
                                <tr>
                                    <td style=""font-size: 0.95rem; font-weight: 600; color: #211c2b; vertical-align: middle;"">
                                        <strong style=""color: #4a148c; font-weight: 750;"">{item.Name}</strong>
                                        <span style=""font-size: 0.85rem; color: #6e657b; font-weight: 500; margin-left: 4px;"">({item.Quantity} {item.Unit})</span>
                                    </td>
                                    <td style=""text-align: right; vertical-align: middle;"">
                                        <span style=""padding: 4px 12px; border-radius: 20px; font-size: 0.72rem; font-weight: 750; background-color: {badgeBg}; color: {badgeText}; text-transform: uppercase; letter-spacing: 0.03em; display: inline-block;"">
                                            {dayLabel}
                                        </span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr><td style=""height: 8px; font-size: 8px; line-height: 8px;"">&nbsp;</td></tr>");
            }
            emailBuilder.Append("</table>");

            // Premium Recipe Card Section
            if (matchedRecipe != null)
            {
                emailBuilder.Append("<div style=\"border-top: 1px solid rgba(124, 77, 255, 0.12); padding-top: 20px; margin-top: 25px;\">");
                emailBuilder.Append("<h3 style=\"margin: 0 0 14px 0; font-size: 1.1rem; font-weight: 750; color: #4a148c;\">💡 Recommended Meal Idea</h3>");
                
                emailBuilder.Append(@"
                    <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; background-color: #ffffff; border: 1px solid rgba(124, 77, 255, 0.12); border-radius: 14px; overflow: hidden;""><tr>");

                if (!string.IsNullOrWhiteSpace(matchedRecipe.ImageUrl))
                {
                    emailBuilder.Append($@"
                            <td style=""width: 150px; min-width: 150px; max-width: 150px; vertical-align: top;"">
                                <img src=""{matchedRecipe.ImageUrl}"" alt=""{matchedRecipe.Title}"" style=""width: 150px; height: 130px; object-fit: cover; display: block; border-top-left-radius: 13px; border-bottom-left-radius: 13px;"" />
                            </td>");
                }

                emailBuilder.Append($@"
                            <td style=""padding: 18px; vertical-align: middle; text-align: left;"">
                                <h4 style=""margin: 0 0 4px 0; font-size: 1.05rem; font-weight: 750; color: #4a148c; line-height: 1.35;"">{matchedRecipe.Title}</h4>
                                <p style=""margin: 0 0 14px 0; font-size: 0.82rem; color: #6e657b; line-height: 1.4;"">This meal blueprint automatically salvages your expiring supplies to minimize kitchen food waste lines.</p>
                                <a href=""{matchedRecipe.SourceUrl}"" target=""_blank"" rel=""noopener"" style=""display: inline-block; padding: 8px 16px; background: linear-gradient(135deg, #7c4dff, #b388ff); color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 750; font-size: 0.8rem; text-align: center;"">
                                    View Blueprint Instructions →
                                </a>
                            </td>
                        </tr>
                    </table>");
                emailBuilder.Append("</div>");
            }

            emailBuilder.Append("</div>");

            var subject = $"SmartFoods Reminder: {criticalItems.Count} items need attention soon!";
            var bodyText = emailBuilder.ToString();



            try
            {
                // 6. Deliver the alert email message using our SMTP implementation
                await _emailService.SendEmailAsync(user.Email!, subject, bodyText);

                // 7. Permanently track notification state history logs to block duplicate deliveries
                var log = new NotificationLog
                {
                    UserId = user.Id,
                    NotificationDate = today,
                    Title = subject,
                    MessageBody = bodyText,
                    IsEmailSent = true,
                    SentAt = DateTime.UtcNow
                };

                dbContext.NotificationLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log delivery exception errors to terminal streams gracefully without breaking other users
                Console.WriteLine($"[Hangfire Expiry Job Error] Failed delivering email to {user.Email}: {ex.Message}");
            }
        }
    }
}
