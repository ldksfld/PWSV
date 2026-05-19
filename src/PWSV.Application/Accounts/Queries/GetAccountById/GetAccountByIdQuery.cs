using MediatR;
using PWSV.Application.Accounts.Dto;

namespace PWSV.Application.Accounts.Queries.GetAccountById;

public sealed record GetAccountByIdQuery(int Id) : IRequest<AccountDetailsDto>;
