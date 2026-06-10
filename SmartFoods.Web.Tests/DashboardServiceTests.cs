using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.DTOs.Dashboard;
using SmartFoods.Web.Services.Dashboard;

namespace SmartFoods.Web.Tests;

public class DashboardServiceTests
{
    [Fact]
    public async Task AddFoodItemAsync_CreatesPantry_WhenOneDoesNotExist()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var service = new DashboardService(dbContext);

        var userId = Guid.NewGuid();
        var model = new CreateFoodItemDto
        {
            Name = "Milk",
            Quantity = 2,
            Unit = "Litres",
            Category = "Dairy",
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5))
        };

        await service.AddFoodItemAsync(userId, model);

        var pantry = await dbContext.Pantries.SingleOrDefaultAsync(p => p.UserId == userId);
        var foodItem = await dbContext.FoodItems.SingleOrDefaultAsync();

        Assert.NotNull(pantry);
        Assert.NotNull(foodItem);
        Assert.Equal("Milk", foodItem!.Name);
        Assert.Equal(pantry!.Id, foodItem.PantryId);
    }
}
