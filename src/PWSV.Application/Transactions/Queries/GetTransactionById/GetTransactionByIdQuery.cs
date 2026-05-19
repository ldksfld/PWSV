using MediatR;
using PWSV.Application.Transactions.Dto;

namespace PWSV.Application.Transactions.Queries.GetTransactionById;

public sealed record GetTransactionByIdQuery(long Id) : IRequest<TransactionDto>;
