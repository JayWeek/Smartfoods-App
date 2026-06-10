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

            // 5. Construct the HTML Notification Message Template
            var emailBuilder = new StringBuilder();
            emailBuilder.Append($"<h2>Hello, {user.Name}!</h2>");
            emailBuilder.Append("<p>This is a quick summary of items in your pantry needing attention within the next 3 days:</p>");
            emailBuilder.Append("<ul>");
            
            foreach (var item in criticalItems)
            {
                var daysLeft = item.ExpiryDate.DayNumber - today.DayNumber;
                var dayText = daysLeft == 0 ? "Expires today!" : daysLeft == 1 ? "1 day left" : $"{daysLeft} days left";
                emailBuilder.Append($"<li><strong>{item.Name}</strong> ({item.Quantity} {item.Unit}) - <span style='color:red;'>{dayText}</span></li>");
            }
            emailBuilder.Append("</ul>");

            if (matchedRecipe != null)
            {
                emailBuilder.Append("<br/><h3>💡 Suggested Meal Idea</h3>");
                emailBuilder.Append($"<p>To use up your expiring ingredients, consider making: <strong>{matchedRecipe.Title}</strong></p>");
                emailBuilder.Append($"<p><a href='{matchedRecipe.SourceUrl}' style='display:inline-block;padding:8px 12px;background-color:#28a745;color:white;text-decoration:none;border-radius:4px;'>View Recipe Instructions</a></p>");
            }

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
