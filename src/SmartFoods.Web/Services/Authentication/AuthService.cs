using Microsoft.AspNetCore.Identity;
using SmartFoods.Web.Data;
using SmartFoods.Web.Models.DTOs;
using SmartFoods.Web.Models.Identity;
using SmartFoods.Web.Models.Pantry;
using SmartFoods.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace SmartFoods.Web.Services.Authentication;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _dbContext;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)>
        RegisterAsync(RegisterDto model)
    {
        if (model.Password != model.ConfirmPassword)
        {
            return (
                false,
                new[] { "Passwords do not match." }
            );
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var user = new ApplicationUser
            {
                Name = model.Name,
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                return (
                    false,
                    result.Errors.Select(e => e.Description)
                );
            }

            var pantry = new Pantry
            {
                Name = "Main Pantry",
                UserId = user.Id
            };

            _dbContext.Pantries.Add(pantry);

            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return (
                true,
                Enumerable.Empty<string>()
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return (
                false,
                new[] { ex.Message }
            );
        }
    }

    public async Task<bool> LoginAsync(LoginDto model)
    {
        var result =
            await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

        return result.Succeeded;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}