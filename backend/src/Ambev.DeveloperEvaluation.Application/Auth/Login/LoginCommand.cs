using Ambev.DeveloperEvaluation.Application.Abstractions;

namespace Ambev.DeveloperEvaluation.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResult>;
