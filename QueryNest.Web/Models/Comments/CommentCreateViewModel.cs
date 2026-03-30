using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Comments;

public class CommentCreateViewModel
{
    public int AnswerId { get; set; }
    public int? ParentCommentId { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 2)]
    public string Content { get; set; } = default!;
}
