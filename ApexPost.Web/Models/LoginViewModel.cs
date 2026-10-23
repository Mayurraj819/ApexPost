using System.ComponentModel.DataAnnotations;

namespace ApexPost.Web.Models;

public sealed class LoginViewModel
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
