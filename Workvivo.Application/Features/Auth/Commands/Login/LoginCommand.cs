using Workvivo.Application.Common.Messaging;
using Workvivo.Domain.Models.Auth;

namespace Workvivo.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : ICommand<AuthResult>;
