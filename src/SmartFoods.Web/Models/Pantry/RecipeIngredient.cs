using SmartFoods.Web.Models.Base;

namespace SmartFoods.Web.Models.Pantry;

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // e.g., "milk", "carrot"
    public string OriginalText { get; set; } = string.Empty; // e.g., "2 cups of low-fat milk"
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
}
