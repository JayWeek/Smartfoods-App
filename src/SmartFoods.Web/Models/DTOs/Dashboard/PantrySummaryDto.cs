namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class PantrySummaryDto
{
    public int TotalFoodItems { get; set; }
    public int TotalCategories { get; set; }
    public int ExpiringSoonCount { get; set; }
    public bool IsEmptyPantry => TotalFoodItems == 0;
}
