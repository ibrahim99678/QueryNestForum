using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Votes;

public class VoteCastViewModel
{
    [Required]
    public int TargetType { get; set; }

    [Required]
    public int TargetId { get; set; }

    [Required]
    public int VoteType { get; set; }

    [Required]
    public int QuestionId { get; set; }
}
