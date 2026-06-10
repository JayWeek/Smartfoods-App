using Microsoft.AspNetCore.Identity;

namespace SmartFoods.Web.Models.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Pantry.Pantry? Pantry { get; set; }
}
