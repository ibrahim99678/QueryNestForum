namespace QueryNest.Contract.Reports;

public class ReportListItemDto
{
    public int ReportId { get; init; }
    public ReportTargetTypeDto TargetType { get; init; }
    public int TargetId { get; init; }
    public string Reason { get; init; } = default!;
    public string? Details { get; init; }
    public ReportStatusDto Status { get; init; }
    public DateTime CreatedAt { get; init; }

    public int ReporterUserId { get; init; }
    public string ReporterName { get; init; } = default!;

    public int ContentOwnerUserId { get; init; }
    public string ContentOwnerName { get; init; } = default!;

    public int? QuestionId { get; init; }
    public string? ContentPreview { get; init; }
}
