using MediatR;
using PWSV.Application.Auth.Dto;

namespace PWSV.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Username, string Password, string MasterPassword) : IRequest<AuthResponseDto>;
