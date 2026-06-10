    using System.ComponentModel.DataAnnotations;

namespace SmartFoods.Web.Models.DTOs.Dashboard;

public class CreateFoodItemDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public DateOnly ExpiryDate { get; set; }
}