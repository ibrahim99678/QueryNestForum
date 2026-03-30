using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Account;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
