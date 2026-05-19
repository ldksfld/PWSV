using FluentValidation;

namespace PWSV.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(1, 128);
        RuleFor(x => x.Kind).IsInEnum();
    }
}
