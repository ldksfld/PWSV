using System.Data;
using MediatR;
using PWSV.Application.Auth.Dto;
using PWSV.Application.Common.Behaviors;

namespace PWSV.Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Username, string Password, string MasterPassword) : IRequest<AuthResponseDto>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel => IsolationLevel.Serializable;
}
