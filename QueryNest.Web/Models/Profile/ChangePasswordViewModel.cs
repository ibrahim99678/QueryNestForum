using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Profile;

public class ChangePasswordViewModel
{
    public bool HasPassword { get; set; }

    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = default!;
}
