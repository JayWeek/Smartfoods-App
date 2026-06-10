namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class SuggestedRecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string TargetIngredient { get; set; } = string.Empty; // e.g., "Uses your expiring Milk!"
}

public class SmartSuggestionsDto
{
    public List<SuggestedRecipeDto> Suggestions { get; set; } = new();
    public bool IsEmptyState => Suggestions.Count == 0;
}
