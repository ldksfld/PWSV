using FluentValidation;
using PWSV.Application.Common.Validation;

namespace PWSV.Application.Auth.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MasterPassword).NotEmpty().MaximumLength(ValidationRules.MaxSecretLength);
    }
}
