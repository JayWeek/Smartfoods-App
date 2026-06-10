namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class RecentActivityItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}

public class RecentActivityDto
{
    public List<RecentActivityItemDto> RecentItems { get; set; } = new();
    public bool IsEmptyHistory => RecentItems.Count == 0;
}
