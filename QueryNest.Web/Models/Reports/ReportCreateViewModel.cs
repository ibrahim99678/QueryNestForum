using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Reports;

public class ReportCreateViewModel
{
    [Required]
    public int TargetType { get; set; }

    [Required]
    public int TargetId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Reason { get; set; } = default!;

    [StringLength(1000)]
    public string? Details { get; set; }

    [Required]
    public int ReturnQuestionId { get; set; }
}
