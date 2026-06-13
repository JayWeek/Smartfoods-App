using SmartFoods.Web.Models.Base;
using SmartFoods.Web.Models.Identity;

namespace SmartFoods.Web.Models.Pantry;

public enum InventoryResolution
{
    Consumed = 1,
    Wasted = 2
}

public class PantryInventoryLog : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    public DateOnly OriginalExpiryDate { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    
    public InventoryResolution Resolution { get; set; }
}
