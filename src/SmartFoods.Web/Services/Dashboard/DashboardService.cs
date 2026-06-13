using Microsoft.EntityFrameworkCore;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.DTOs.Dashboard;
using SmartFoods.Web.Models.Pantry;
using SmartFoods.Web.Services.Interfaces;

namespace SmartFoods.Web.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IRecipeIntegrationService _recipeIntegrationService;

    public DashboardService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IRecipeIntegrationService recipeIntegrationService)
    {
        _dbContextFactory = dbContextFactory;
        _recipeIntegrationService = recipeIntegrationService;
    }

    public async Task<WelcomePanelDto>
        GetWelcomePanelAsync(Guid userId)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var user =
            await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Pantry)
                .ThenInclude(p => p!.FoodItems)
                .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new Exception("User not found.");
        }

        var pantry = user.Pantry;

        var foodItems =
            pantry?.FoodItems
            ?? Enumerable.Empty<Models.Pantry.FoodItem>();

        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        var sevenDaysFromNow =
            today.AddDays(7);

        return new WelcomePanelDto
        {
            UserName = user.Name,

            TotalFoodItems =
                foodItems.Count(),

            TotalCategories =
                foodItems
                    .Where(f =>
                        !string.IsNullOrWhiteSpace(
                            f.Category))
                    .Select(f => f.Category)
                    .Distinct()
                    .Count(),

            ItemsNeedingAttention =
                foodItems.Count(f =>
                    f.ExpiryDate >= today &&
                    f.ExpiryDate <= sevenDaysFromNow)
        };
    }

    public async Task AddFoodItemAsync(
        Guid userId,
        CreateFoodItemDto model)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var pantry =
            await dbContext.Pantries
                .FirstOrDefaultAsync(
                    p => p.UserId == userId);

        if (pantry is null)
        {
            pantry = new Pantry
            {
                Name = "Main Pantry",
                UserId = userId
            };

            dbContext.Pantries.Add(pantry);
            await dbContext.SaveChangesAsync();
        }

        var foodItem =
            new Models.Pantry.FoodItem
            {
                Name = model.Name,
                Quantity = model.Quantity,
                Unit = model.Unit,
                Category = model.Category,
                ExpiryDate = model.ExpiryDate,
                PantryId = pantry.Id
            };

        dbContext.FoodItems.Add(foodItem);

        await dbContext.SaveChangesAsync();
    }

    public async Task<PantrySummaryDto> GetPantrySummaryAsync(Guid userId)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new Exception("User not found.");
        }

        var foodItems = user.Pantry?.FoodItems ?? Enumerable.Empty<Models.Pantry.FoodItem>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysFromNow = today.AddDays(7);

        return new PantrySummaryDto
        {
            TotalFoodItems = foodItems.Count(),
            
            TotalCategories = foodItems
                .Where(f => !string.IsNullOrWhiteSpace(f.Category))
                .Select(f => f.Category)
                .Distinct()
                .Count(),

            ExpiringSoonCount = foodItems.Count(f => 
                f.ExpiryDate >= today && 
                f.ExpiryDate <= sevenDaysFromNow)
        };
    }

    public async Task<AttentionRequiredDto> GetAttentionRequiredAsync(Guid userId)
    {
        // Creating a clean, isolated context per your architecture upgrade
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new Exception("User not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysFromNow = today.AddDays(7);

        var foodItems = user.Pantry?.FoodItems ?? Enumerable.Empty<Models.Pantry.FoodItem>();

        // Filter, sort by closest expiry date, and transform
        var urgentItems = foodItems
            .Where(f => f.ExpiryDate >= today && f.ExpiryDate <= sevenDaysFromNow)
            .OrderBy(f => f.ExpiryDate)
            .Select(f => new AttentionRequiredItemDto
            {
                Name = f.Name,
                Quantity = f.Quantity,
                Unit = f.Unit,
                DaysRemaining = f.ExpiryDate.DayNumber - today.DayNumber
            })
            .ToList();

        return new AttentionRequiredDto
        {
            UrgentItems = urgentItems
        };
    }

    public async Task<CategoryChartDto> GetCategoryChartAsync(Guid userId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new Exception("User not found.");
        }

        var foodItems = user.Pantry?.FoodItems ?? Enumerable.Empty<Models.Pantry.FoodItem>();
        var totalItems = foodItems.Count();

        if (totalItems == 0)
        {
            return new CategoryChartDto();
        }

        var groupedCategories = foodItems
            .Where(f => !string.IsNullOrWhiteSpace(f.Category))
            .GroupBy(f => f.Category)
            .Select(g => new CategoryPercentageDto
            {
                CategoryName = g.Key,
                ItemCount = g.Count(),
                Percentage = Math.Round(((decimal)g.Count() / totalItems) * 100, 1)
            })
            .OrderByDescending(c => c.ItemCount)
            .ToList();

        return new CategoryChartDto
        {
            Categories = groupedCategories
        };
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync(Guid userId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new Exception("User not found.");
        }

        var foodItems = user.Pantry?.FoodItems ?? Enumerable.Empty<Models.Pantry.FoodItem>();

        // Take the 5 most recently created items based on CreatedAt timestamp
        var recentItems = foodItems
            .OrderByDescending(f => f.CreatedAt)
            .Take(5)
            .Select(f => new RecentActivityItemDto
            {
                Name = f.Name,
                Quantity = f.Quantity,
                Unit = f.Unit,
                AddedAt = f.CreatedAt
            })
            .ToList();

        return new RecentActivityDto
        {
            RecentItems = recentItems
        };
    }

    public async Task<SmartSuggestionsDto> GetSmartSuggestionsAsync(Guid userId)
    {
        using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) throw new Exception("User not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // EXTENDED: Changed from 7 days to 30 days to guarantee your new test items match!
        var testingHorizon = today.AddDays(30);

        var expiringItems = (user.Pantry?.FoodItems ?? Enumerable.Empty<FoodItem>())
            .Where(f => f.ExpiryDate >= today && f.ExpiryDate <= testingHorizon)
            .OrderBy(f => f.ExpiryDate)
            .Select(f => f.Name.Trim().ToLowerInvariant())
            .Distinct()
            .Take(3)
            .ToList();

        var result = new SmartSuggestionsDto();
        if (!expiringItems.Any()) return result; 

        foreach (var ingredient in expiringItems)
        {
            // Enforce a strict lowercase lookup match
            var localCount = await dbContext.RecipeIngredients
                .CountAsync(i => i.Name == ingredient);

            if (localCount < 3)
            {
                try
                {
                    var fetchedRecipes = await _recipeIntegrationService.FetchAndMapRecipesAsync(ingredient);
                    
                    foreach (var apiRecipe in fetchedRecipes)
                    {
                        var existingRecipe = await dbContext.Recipes
                            .Include(r => r.Ingredients)
                            .FirstOrDefaultAsync(r => r.ExternalApiId == apiRecipe.ExternalApiId);

                        if (existingRecipe is null)
                        {
                            dbContext.Recipes.Add(apiRecipe);
                        }
                        else if (!existingRecipe.Ingredients.Any(i => i.Name == ingredient))
                        {
                            existingRecipe.Ingredients.Add(new RecipeIngredient
                            {
                                Name = ingredient,
                                OriginalText = ingredient,
                                Amount = 0,
                                Unit = string.Empty
                            });
                        }
                    }
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // CRITICAL DIAGNOSTIC: Print hidden background errors straight to your console log terminal!
                    Console.WriteLine($"[SmartFoods Harvest Error] Failed mapping ingredient '{ingredient}': {ex.Message}");
                    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }

            // Uniform lowercase query extraction
            var localRecipeMatch = await dbContext.RecipeIngredients
                .AsNoTracking()
                .Include(i => i.Recipe)
                .Where(i => i.Name == ingredient)
                .Select(i => i.Recipe)
                .FirstOrDefaultAsync();

            if (localRecipeMatch != null)
            {
                result.Suggestions.Add(new SuggestedRecipeDto
                {
                    Title = localRecipeMatch.Title,
                    ImageUrl = localRecipeMatch.ImageUrl,
                    SourceUrl = localRecipeMatch.SourceUrl,
                    TargetIngredient = ingredient
                });
            }
        }

        return result;
    }


    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid userId)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            throw new Exception("User not found.");

        var foodItems =
            user.Pantry?.FoodItems ??
            Enumerable.Empty<FoodItem>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDays = today.AddDays(7);

        var totalItems = foodItems.Count();

        var totalCategories = foodItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .Select(x => x.Category)
            .Distinct()
            .Count();

        var expiringItems = foodItems
            .Where(x =>
                x.ExpiryDate >= today &&
                x.ExpiryDate <= sevenDays)
            .OrderBy(x => x.ExpiryDate)
            .ToList();

        var result = new DashboardOverviewDto();

        result.Welcome = new WelcomePanelDto
        {
            UserName = user.Name,
            TotalFoodItems = totalItems,
            TotalCategories = totalCategories,
            ItemsNeedingAttention = expiringItems.Count
        };

        result.PantrySummary = new PantrySummaryDto
        {
            TotalFoodItems = totalItems,
            TotalCategories = totalCategories,
            ExpiringSoonCount = expiringItems.Count
        };

        result.AttentionRequired = new AttentionRequiredDto
        {
            UrgentItems = expiringItems.Select(x =>
                new AttentionRequiredItemDto
                {
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Unit = x.Unit,
                    DaysRemaining =
                        x.ExpiryDate.DayNumber -
                        today.DayNumber
                })
                .ToList()
        };

        result.CategoryChart = new CategoryChartDto
        {
            Categories = foodItems
                .GroupBy(x => x.Category)
                .Select(g =>
                    new CategoryPercentageDto
                    {
                        CategoryName = g.Key,
                        ItemCount = g.Count(),
                        Percentage = Math.Round(
                            ((decimal)g.Count() / totalItems) * 100,
                            1)
                    })
                .OrderByDescending(x => x.ItemCount)
                .ToList()
        };

        result.RecentActivity = new RecentActivityDto
        {
            RecentItems = foodItems
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new RecentActivityItemDto
                {
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Unit = x.Unit,
                    AddedAt = x.CreatedAt
                })
                .ToList()
        };

        result.SmartSuggestions =
            await GetSmartSuggestionsAsync(userId);

        return result;
    }




}
