using QueryNest.Domain.Enums;

namespace QueryNest.Domain.Entities;

public class Report
{
    public int ReportId { get; set; }
    public int ReporterUserId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int? CommentId { get; set; }
    public string Reason { get; set; } = default!;
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public UserProfile Reporter { get; set; } = default!;
    public UserProfile? ReviewedBy { get; set; }
    public Question? Question { get; set; }
    public Answer? Answer { get; set; }
    public Comment? Comment { get; set; }
}
