// src/SmartFoods.Web/Models/DTOs/LoginDto.cs
using System.ComponentModel.DataAnnotations;

namespace SmartFoods.Web.Models.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
