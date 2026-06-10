using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SmartFoods.Web.Models.Pantry;
using SmartFoods.Web.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SmartFoods.Web.Services.Infrastructure;

public class SpoonacularIntegrationService : IRecipeIntegrationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;

    // CHANGED: Inject IHttpClientFactory directly to obtain isolated client instances safely
    public SpoonacularIntegrationService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["Spoonacular:ApiKey"] ?? throw new ArgumentNullException("Spoonacular API Key is missing.");
    }

    public async Task<List<Recipe>> FetchAndMapRecipesAsync(string ingredientName)
    {
        var normalizedIngredientName = ingredientName.ToLowerOffsetCleaned();
        var url = $"https://api.spoonacular.com/recipes/findByIngredients?ingredients={Uri.EscapeDataString(normalizedIngredientName)}&number=10&ranking=1&ignorePantry=true&apiKey={_apiKey}";

        // Obtain a standalone, clean execution client
        var httpClient = _httpClientFactory.CreateClient();
        
        var apiResponse = await httpClient.GetFromJsonAsync<List<SpoonacularRecipeResponse>>(url);
        var localRecipes = new List<Recipe>();

        if (apiResponse == null) return localRecipes;

        foreach (var item in apiResponse)
        {
            var recipe = new Recipe
            {
                ExternalApiId = item.Id,
                Title = item.Title,
                ImageUrl = item.Image,
                SourceUrl = $"https://spoonacular.com{item.Title.Replace(" ", "-")}-{item.Id}",
                ReadyInMinutes = 30,
                Servings = 4,
                Ingredients = new List<RecipeIngredient>()
            };

            var allIngredients = (item.UsedIngredients ?? Enumerable.Empty<SpoonacularIngredient>())
                .Concat(item.MissedIngredients ?? Enumerable.Empty<SpoonacularIngredient>());

            foreach (var ing in allIngredients)
            {
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Name = ing.Name.ToLowerOffsetCleaned(),
                    OriginalText = ing.Original ?? $"{ing.Amount} {ing.Unit} {ing.Name}",
                    Amount = (decimal)ing.Amount,
                    Unit = ing.Unit ?? string.Empty
                });
            }

            if (!recipe.Ingredients.Any(i => i.Name == normalizedIngredientName))
            {
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Name = normalizedIngredientName,
                    OriginalText = normalizedIngredientName,
                    Amount = 0,
                    Unit = string.Empty
                });
            }

            localRecipes.Add(recipe);
        }

        return localRecipes;
    }
}


// Internal mapping transfer objects to parse the incoming Spoonacular JSON structure cleanly
internal class SpoonacularRecipeResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("usedIngredients")]
    public List<SpoonacularIngredient>? UsedIngredients { get; set; }

    [JsonPropertyName("missedIngredients")]
    public List<SpoonacularIngredient>? MissedIngredients { get; set; }
}

internal class SpoonacularIngredient
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("original")]
    public string? Original { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
}

// Tiny text sanitizer to optimize database uniformity
internal static class StringExtensions
{
    public static string ToLowerOffsetCleaned(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return input.Trim().ToLowerInvariant();
    }
}


