using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Tags;

public class TagUpsertViewModel
{
    public int? TagId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = default!;
}
