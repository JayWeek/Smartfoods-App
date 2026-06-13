using System.ComponentModel.DataAnnotations;
using SmartFoods.Web.Models.Pantry;

namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class ResolveFoodItemDto
{
    [Required]
    public Guid FoodItemId { get; set; }

    [Required]
    public InventoryResolution Resolution { get; set; }
}
