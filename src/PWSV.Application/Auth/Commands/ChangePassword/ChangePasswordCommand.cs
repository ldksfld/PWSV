using MediatR;

namespace PWSV.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string OldPassword, string NewPassword) : IRequest<Unit>;
