using SmartFoods.Web.Models.Base;

namespace SmartFoods.Web.Models.Pantry;

public class Recipe : BaseEntity
{
    public int ExternalApiId { get; set; } // Tracks Spoonacular ID to prevent duplicate imports
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int ReadyInMinutes { get; set; }
    public int Servings { get; set; }

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
