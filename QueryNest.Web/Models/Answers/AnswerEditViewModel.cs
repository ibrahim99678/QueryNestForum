using System.ComponentModel.DataAnnotations;

namespace QueryNest.Web.Models.Answers;

public class AnswerEditViewModel
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 10)]
    public string Content { get; set; } = default!;
}
