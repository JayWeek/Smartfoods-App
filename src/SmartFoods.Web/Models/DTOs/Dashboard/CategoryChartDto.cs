namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class CategoryPercentageDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CategoryChartDto
{
    public List<CategoryPercentageDto> Categories { get; set; } = new();
    public bool IsEmptyPantry => Categories.Count == 0;
}
