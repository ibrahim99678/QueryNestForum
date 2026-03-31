namespace QueryNest.Web.Models.Categories;

public class CategoryListItemViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
}
