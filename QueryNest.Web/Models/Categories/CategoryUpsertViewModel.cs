using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Categories;

public class CategoryUpsertViewModel
{
    public int? CategoryId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(300)]
    public string? Description { get; set; }
}
