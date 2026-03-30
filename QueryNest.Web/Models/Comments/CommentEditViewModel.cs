using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Comments;

public class CommentEditViewModel
{
    public int CommentId { get; set; }
    public int QuestionId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public string Content { get; set; } = default!;
}
