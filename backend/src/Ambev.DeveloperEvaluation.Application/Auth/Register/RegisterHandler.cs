using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using Ambev.DeveloperEvaluation.Domain.Users;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Auth.Register;

public interface IUserRegistrationService
{
    Task<Result<RegisterResult>> RegisterAsync(
        string email,
        string password,
        string fullName,
        string phoneNumber,
        UserRole role,
        CancellationToken cancellationToken);
}

internal sealed class RegisterHandler(IUserRegistrationService registrationService)
    : IRequestHandler<RegisterCommand, Result<RegisterResult>>
{
    public Task<Result<RegisterResult>> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        registrationService.RegisterAsync(
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            request.Role,
            cancellationToken);
}
