namespace Ambev.DeveloperEvaluation.Application.Auth.Login;

public sealed record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt);
