using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QueryNest.Web.Models.Questions;

public class QuestionUpsertViewModel
{
    public int? QuestionId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string Title { get; set; } = default!;

    [Required]
    [StringLength(4000, MinimumLength = 20)]
    public string Description { get; set; } = default!;

    [Required]
    public int CategoryId { get; set; }

    public List<SelectListItem> Categories { get; set; } = [];

    public int[] SelectedTagIds { get; set; } = [];
    public List<SelectListItem> Tags { get; set; } = [];

    [StringLength(300)]
    public string? NewTagsCsv { get; set; }
}
