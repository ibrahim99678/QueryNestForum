using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Profile;

public class ProfileViewModel
{
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(1000)]
    public string? Bio { get; set; }

    public string? AvatarPath { get; set; }

    public int Reputation { get; set; }
}
