using FluentValidation;

namespace PWSV.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId.HasValue);
    }
}
