using MediatR;
using PWSV.Application.Accounts.Dto;

namespace PWSV.Application.Accounts.Commands.CreateAccount;

public sealed record CreateAccountCommand(
    string Name,
    int AccountTypeId,
    int CurrencyId,
    string? AccountNumber,
    decimal InitialBalance) : IRequest<AccountDto>;
