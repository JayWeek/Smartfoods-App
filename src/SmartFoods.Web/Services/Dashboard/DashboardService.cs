using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.DTOs.Dashboard;
using SmartFoods.Web.Models.Pantry;
using SmartFoods.Web.Services.Interfaces;

namespace SmartFoods.Web.Services.Dashboard;

public class DashboardService : IDashboardService, IDisposable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IRecipeIntegrationService _recipeIntegrationService;
    private readonly DashboardStateHub _stateHub;
    private readonly IMemoryCache _memoryCache;

    // Track the current circuit's active user to handle event-driven local evictions cleanly
    private Guid? _currentCircuitUserId;

    public DashboardService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IRecipeIntegrationService recipeIntegrationService,
        DashboardStateHub stateHub,
        IMemoryCache memoryCache)
    {
        _dbContextFactory = dbContextFactory;
        _recipeIntegrationService = recipeIntegrationService;
        _stateHub = stateHub;
        _memoryCache = memoryCache;

        // Subscribe to app-wide state changes to evict memory entries actively
        _stateHub.OnDashboardChanged += HandleDashboardStateChanged;
    }

    private string GetCacheKey(Guid userId) => $"DashboardOverview_{userId}";

    private void HandleDashboardStateChanged()
    {
        if (_currentCircuitUserId.HasValue)
        {
            EvictCache(_currentCircuitUserId.Value);
        }
    }

    private void EvictCache(Guid userId)
    {
        _memoryCache.Remove(GetCacheKey(userId));
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid userId)
    {
        _currentCircuitUserId = userId;
        string cacheKey = GetCacheKey(userId);

        // FAST PATH: Check the Application Memory Backplane
        if (_memoryCache.TryGetValue(cacheKey, out DashboardOverviewDto? cachedOverview) && cachedOverview != null)
        {
            return cachedOverview;
        }

        // SLOW PATH: Query database context and map dto transformations
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user is null) throw new Exception("User not found.");
        
        var foodItems = user.Pantry?.FoodItems ?? Enumerable.Empty<FoodItem>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDays = today.AddDays(7);
        var totalItems = foodItems.Count();
        
        var totalCategories = foodItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Category))
            .Select(x => x.Category)
            .Distinct()
            .Count();
            
        var expiringItems = foodItems
            .Where(x => x.ExpiryDate >= today && x.ExpiryDate <= sevenDays)
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
            UrgentItems = expiringItems.Select(x => new AttentionRequiredItemDto
            {
                FoodItemId = x.Id,
                Name = x.Name,
                Quantity = x.Quantity,
                Unit = x.Unit,
                DaysRemaining = x.ExpiryDate.DayNumber - today.DayNumber
            }).ToList()
        };
        
        result.CategoryChart = new CategoryChartDto
        {
            Categories = foodItems
                .GroupBy(x => x.Category)
                .Select(g => new CategoryPercentageDto
                {
                    CategoryName = g.Key,
                    ItemCount = g.Count(),
                    Percentage = Math.Round(((decimal)g.Count() / totalItems) * 100, 1)
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
        
        result.SmartSuggestions = await GetSmartSuggestionsAsync(userId);

        // Save into memory cache with a 15-minute sliding window to elegantly handle tab sleep
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(15))
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));

        _memoryCache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task AddFoodItemAsync(Guid userId, CreateFoodItemDto model)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var pantry = await dbContext.Pantries.FirstOrDefaultAsync(p => p.UserId == userId);
        
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
        
        var foodItem = new Models.Pantry.FoodItem
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

        // Evict immediately to clear data across all layers synchronously
        EvictCache(userId);
    }

    public async Task ResolveFoodItemAsync(Guid userId, ResolveFoodItemDto model)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        
        var foodItem = await dbContext.FoodItems
            .Include(f => f.Pantry)
            .FirstOrDefaultAsync(f => f.Id == model.FoodItemId && f.Pantry.UserId == userId);

        if (foodItem is null)
        {
            throw new Exception("Target inventory record could not be verified inside your tracking parameters.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var logEntry = new PantryInventoryLog
            {
                UserId = userId,
                ItemName = foodItem.Name,
                Quantity = foodItem.Quantity,
                Unit = foodItem.Unit,
                Category = foodItem.Category,
                OriginalExpiryDate = foodItem.ExpiryDate,
                Resolution = model.Resolution,
                LoggedAt = DateTime.UtcNow
            };

            dbContext.PantryInventoryLogs.Add(logEntry);
            dbContext.FoodItems.Remove(foodItem);

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Evict immediately to clear data across all layers synchronously
            EvictCache(userId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<WelcomePanelDto> GetWelcomePanelAsync(Guid userId)
    {
        var overview = await GetDashboardOverviewAsync(userId);
        return overview.Welcome;
    }

    public async Task<PantrySummaryDto> GetPantrySummaryAsync(Guid userId)
    {
        var overview = await GetDashboardOverviewAsync(userId);
        return overview.PantrySummary;
    }

    public async Task<AttentionRequiredDto> GetAttentionRequiredAsync(Guid userId)
    {
        var overview = await GetDashboardOverviewAsync(userId);
        return overview.AttentionRequired;
    }

    public async Task<CategoryChartDto> GetCategoryChartAsync(Guid userId)
    {
        var overview = await GetDashboardOverviewAsync(userId);
        return overview.CategoryChart;
    }

    public async Task<RecentActivityDto> GetRecentActivityAsync(Guid userId)
    {
        var overview = await GetDashboardOverviewAsync(userId);
        return overview.RecentActivity;
    }

    public async Task<SmartSuggestionsDto> GetSmartSuggestionsAsync(Guid userId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Pantry)
            .ThenInclude(p => p!.FoodItems)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user is null) throw new Exception("User not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
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
            var localCount = await dbContext.RecipeIngredients.CountAsync(i => i.Name == ingredient);
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
                    Console.WriteLine($"[SmartFoods Harvest Error] Failed mapping ingredient '{ingredient}': {ex.Message}");
                }
            }
            
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

    public void Dispose()
    {
        _stateHub.OnDashboardChanged -= HandleDashboardStateChanged;
    }
}