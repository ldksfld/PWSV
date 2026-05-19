using MediatR;
using PWSV.Application.Auth.Dto;

namespace PWSV.Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Username, string Password, string MasterPassword) : IRequest<AuthResponseDto>;
