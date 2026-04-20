using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Application.Auth.Login;

namespace Ambev.DeveloperEvaluation.Application.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<LoginResult>;
