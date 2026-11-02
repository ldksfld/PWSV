using MediatR;

namespace PWSV.Application.Auth.Commands.DeleteUser;

public sealed record DeleteUserCommand(string Password, string MasterPassword) : IRequest<Unit>;
