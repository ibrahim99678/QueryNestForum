namespace QueryNest.Web.Models.Reports;

public class ReportListItemViewModel
{
    public int ReportId { get; set; }
    public string TargetType { get; set; } = default!;
    public int TargetId { get; set; }
    public string Reason { get; set; } = default!;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }

    public string ReporterName { get; set; } = default!;
    public string ContentOwnerName { get; set; } = default!;
    public int ContentOwnerUserId { get; set; }
    public int? QuestionId { get; set; }
    public string? ContentPreview { get; set; }
}
