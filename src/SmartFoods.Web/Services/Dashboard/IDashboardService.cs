using SmartFoods.Web.Models.DTOs.Dashboard;

namespace SmartFoods.Web.Services.Dashboard;

public interface IDashboardService
{
    Task<WelcomePanelDto>
        GetWelcomePanelAsync(Guid userId);

    Task AddFoodItemAsync(
        Guid userId,
        CreateFoodItemDto model);

    Task<PantrySummaryDto> GetPantrySummaryAsync(Guid userId);
    Task<AttentionRequiredDto> GetAttentionRequiredAsync(Guid userId);
    Task<CategoryChartDto> GetCategoryChartAsync(Guid userId);
    Task<RecentActivityDto> GetRecentActivityAsync(Guid userId);
    Task<SmartSuggestionsDto> GetSmartSuggestionsAsync(Guid userId);
}