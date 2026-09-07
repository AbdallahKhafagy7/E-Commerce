using System.ComponentModel.DataAnnotations;

namespace E_Commerce.Api.DTOs.Requests;

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "AccessToken is required.")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "RefreshToken is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
