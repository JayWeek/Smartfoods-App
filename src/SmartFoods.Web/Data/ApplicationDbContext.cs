using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Models.Identity;
using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pantry> Pantries => Set<Pantry>();

    public DbSet<FoodItem> FoodItems => Set<FoodItem>();

    // NEW RECIPE TABLES FOR FEATURE 7
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Pantry)
            .WithOne(p => p.User)
            .HasForeignKey<Pantry>(p => p.UserId);

        builder.Entity<Pantry>()
            .HasMany(p => p.FoodItems)
            .WithOne(f => f.Pantry)
            .HasForeignKey(f => f.PantryId);

        // Configure Recipe Relationships
        builder.Entity<Recipe>()
            .HasMany(r => r.Ingredients)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId);

        // CRITICAL INDEX: Optimizes searching through millions of ingredients locally
        builder.Entity<RecipeIngredient>()
            .HasIndex(i => i.Name);
            
        // UNIQUE INDEX: Prevents importing the same recipe twice from the API
        builder.Entity<Recipe>()
            .HasIndex(r => r.ExternalApiId)
            .IsUnique();

        // Configure Notification History Tracking
        builder.Entity<NotificationLog>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);

        // Index to quickly verify if an email went out today for a user
        builder.Entity<NotificationLog>()
            .HasIndex(n => new { n.UserId, n.NotificationDate })
            .IsUnique();

    }
}
