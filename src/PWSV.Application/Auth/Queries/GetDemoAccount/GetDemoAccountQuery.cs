using MediatR;
using PWSV.Application.Auth.Dto;

namespace PWSV.Application.Auth.Queries.GetDemoAccount;

public sealed record GetDemoAccountQuery : IRequest<DemoAccountDto?>;
