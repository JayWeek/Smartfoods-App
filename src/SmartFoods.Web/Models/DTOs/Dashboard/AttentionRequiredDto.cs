namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class AttentionRequiredItemDto
{
 public Guid FoodItemId { get; set; }
 public string Name { get; set; } = string.Empty;
 public decimal Quantity { get; set; }
 public string Unit { get; set; } = string.Empty;
 public int DaysRemaining { get; set; }
}

public class AttentionRequiredDto
{
 public List<AttentionRequiredItemDto> UrgentItems { get; set; } = new();
 public bool HasNoUrgentItems => UrgentItems.Count == 0;
}
