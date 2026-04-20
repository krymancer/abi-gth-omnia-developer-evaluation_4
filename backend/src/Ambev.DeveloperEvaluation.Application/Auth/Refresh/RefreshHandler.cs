using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Application.Auth.Login;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Auth.Refresh;

public interface IRefreshTokenService
{
    Task<Result<LoginResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}

internal sealed class RefreshHandler(IRefreshTokenService refreshTokenService)
    : IRequestHandler<RefreshCommand, Result<LoginResult>>
{
    public Task<Result<LoginResult>> Handle(RefreshCommand request, CancellationToken cancellationToken) =>
        refreshTokenService.RefreshAsync(request.RefreshToken, cancellationToken);
}
