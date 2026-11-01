using FluentValidation;

namespace PWSV.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId.HasValue);
    }
}
