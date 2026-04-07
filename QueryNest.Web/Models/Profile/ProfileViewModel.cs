using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QueryNest.Web.Models.Profile;

public class ProfileViewModel
{
    [ValidateNever]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [ValidateNever]
    public string? AvatarPath { get; set; }

    [ValidateNever]
    public int Reputation { get; set; }
}
