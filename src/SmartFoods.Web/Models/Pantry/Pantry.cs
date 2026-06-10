using SmartFoods.Web.Models.Base;
using SmartFoods.Web.Models.Identity;

namespace SmartFoods.Web.Models.Pantry;

public class Pantry : BaseEntity
{
    public string Name { get; set; } = "Main Pantry";

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<FoodItem> FoodItems { get; set; }
        = new List<FoodItem>();
}
