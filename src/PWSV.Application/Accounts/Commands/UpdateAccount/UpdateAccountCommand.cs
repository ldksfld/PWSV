using MediatR;
using PWSV.Application.Accounts.Dto;

namespace PWSV.Application.Accounts.Commands.UpdateAccount;

public sealed record UpdateAccountCommand(int Id, string Name, string? AccountNumber, bool IsActive) : IRequest<AccountDto>;
