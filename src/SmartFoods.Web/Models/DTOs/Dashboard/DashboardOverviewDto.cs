namespace SmartFoods.Web.Models.DTOs.Dashboard;

public sealed class DashboardOverviewDto
{
    public WelcomePanelDto Welcome { get; set; } = new();

    public PantrySummaryDto PantrySummary { get; set; } = new();

    public AttentionRequiredDto AttentionRequired { get; set; } = new();

    public CategoryChartDto CategoryChart { get; set; } = new();

    public RecentActivityDto RecentActivity { get; set; } = new();

    public SmartSuggestionsDto SmartSuggestions { get; set; } = new();
}