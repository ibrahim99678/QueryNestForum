namespace QueryNest.Contract.Votes;

public class CastVoteRequestDto
{
    public VoteTargetTypeDto TargetType { get; init; }
    public int TargetId { get; init; }
    public int VoteType { get; init; }
}
