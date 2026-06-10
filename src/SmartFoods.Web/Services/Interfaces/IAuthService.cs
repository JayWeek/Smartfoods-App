using SmartFoods.Web.Models.DTOs;

namespace SmartFoods.Web.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, IEnumerable<string> Errors)>
        RegisterAsync(RegisterDto model);

    Task<bool>
        LoginAsync(LoginDto model);

    Task LogoutAsync();
}