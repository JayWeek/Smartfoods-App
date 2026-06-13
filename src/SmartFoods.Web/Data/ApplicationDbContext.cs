using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Models.Identity;
using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pantry> Pantries => Set<Pantry>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<PantryInventoryLog> PantryInventoryLogs => Set<PantryInventoryLog>();

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

        builder.Entity<Recipe>()
            .HasMany(r => r.Ingredients)
            .WithOne(i => i.Recipe)
            .HasForeignKey(i => i.RecipeId);

        builder.Entity<RecipeIngredient>()
            .HasIndex(i => i.Name);
        
        builder.Entity<Recipe>()
            .HasIndex(r => r.ExternalApiId)
            .IsUnique();

        builder.Entity<NotificationLog>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);

        builder.Entity<NotificationLog>()
            .HasIndex(n => new { n.UserId, n.NotificationDate })
            .IsUnique();

        // Index configurations to optimize future analytics lookups
        builder.Entity<PantryInventoryLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId);

        builder.Entity<PantryInventoryLog>()
            .HasIndex(l => new { l.UserId, l.Resolution });
    }
}
