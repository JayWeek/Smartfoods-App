using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Services.Interfaces;

public interface IRecipeIntegrationService
{
    /// <summary>
    /// Connects to Spoonacular, fetches recipes containing the specified ingredient, 
    /// and maps them into our local domain models.
    /// </summary>
    Task<List<Recipe>> FetchAndMapRecipesAsync(string ingredientName);
}
