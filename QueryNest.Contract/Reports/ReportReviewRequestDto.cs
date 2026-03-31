namespace QueryNest.Contract.Reports;

public class ReportReviewRequestDto
{
    public int ReportId { get; init; }
    public bool Approve { get; init; }
    public string? ReviewNote { get; init; }
}
