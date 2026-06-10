using SmartFoods.Web.Models.Base;

namespace SmartFoods.Web.Models.Pantry;

public class FoodItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public Guid PantryId { get; set; }

    public Pantry Pantry { get; set; } = null!;
}
