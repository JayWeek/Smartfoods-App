namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class WelcomePanelDto
{
    public string UserName { get; set; } = string.Empty;

    public int TotalFoodItems { get; set; }

    public int TotalCategories { get; set; }

    public int ItemsNeedingAttention { get; set; }

    public bool IsEmptyPantry => TotalFoodItems == 0;
}