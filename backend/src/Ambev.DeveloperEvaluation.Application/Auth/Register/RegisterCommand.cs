using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.Users;

namespace Ambev.DeveloperEvaluation.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber,
    UserRole Role) : ICommand<RegisterResult>;
