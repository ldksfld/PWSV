using MediatR;

namespace PWSV.Application.Accounts.Commands.DeactivateAccount;

public sealed record DeactivateAccountCommand(int Id) : IRequest<Unit>;
