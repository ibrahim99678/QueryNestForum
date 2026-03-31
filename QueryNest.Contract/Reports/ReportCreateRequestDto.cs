namespace QueryNest.Contract.Reports;

public class ReportCreateRequestDto
{
    public ReportTargetTypeDto TargetType { get; init; }
    public int TargetId { get; init; }
    public string Reason { get; init; } = default!;
    public string? Details { get; init; }
}
