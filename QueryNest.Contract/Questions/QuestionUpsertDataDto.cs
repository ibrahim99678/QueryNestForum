using QueryNest.Contract.Categories;
using QueryNest.Contract.Tags;

namespace QueryNest.Contract.Questions;

public class QuestionUpsertDataDto
{
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];
    public IReadOnlyList<TagDto> Tags { get; init; } = [];
}
