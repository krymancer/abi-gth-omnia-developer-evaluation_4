using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Auth.Login;

public interface IIdentityService
{
    Task<Result<LoginResult>> LoginAsync(string email, string password, CancellationToken cancellationToken);
}

internal sealed class LoginHandler(IIdentityService identityService)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        identityService.LoginAsync(request.Email, request.Password, cancellationToken);
}
