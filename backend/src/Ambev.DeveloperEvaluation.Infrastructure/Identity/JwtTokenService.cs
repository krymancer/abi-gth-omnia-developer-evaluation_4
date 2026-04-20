using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ambev.DeveloperEvaluation.Application.Auth.Login;
using Ambev.DeveloperEvaluation.Application.Auth.Refresh;
using Ambev.DeveloperEvaluation.Application.Auth.Register;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using Ambev.DeveloperEvaluation.Domain.Users;
using Ambev.DeveloperEvaluation.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ambev.DeveloperEvaluation.Infrastructure.Identity;

internal sealed class JwtTokenService(
    UserManager<AppUser> userManager,
    AppDbContext dbContext,
    IOptions<JwtOptions> options)
    : IIdentityService, IRefreshTokenService, IUserRegistrationService
{
    private readonly JwtOptions _jwt = options.Value;

    public async Task<Result<LoginResult>> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return Result.Failure<LoginResult>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        if (user.Status != UserStatus.Active)
            return Result.Failure<LoginResult>(Error.Validation("Auth.Inactive", "Account is not active."));

        return Result.Success(await GenerateTokensAsync(user, cancellationToken));
    }

    public async Task<Result<LoginResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await dbContext.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && !r.IsRevoked, cancellationToken);

        if (stored is null || stored.ExpiresAt < DateTimeOffset.UtcNow)
            return Result.Failure<LoginResult>(Error.Validation("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."));

        stored.IsRevoked = true;
        var result = await GenerateTokensAsync(stored.User, cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<RegisterResult>> RegisterAsync(
        string email,
        string password,
        string fullName,
        string phoneNumber,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result.Failure<RegisterResult>(Error.Conflict("Auth.EmailInUse", "Email is already registered."));

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Role = role,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var error = createResult.Errors.First();
            return Result.Failure<RegisterResult>(Error.Validation(error.Code, error.Description));
        }

        return Result.Success(new RegisterResult(user.Id, user.Email!));
    }

    private async Task<LoginResult> GenerateTokensAsync(AppUser user, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes);
        var accessToken = CreateJwt(user, expiresAt);

        var rawRefresh = GenerateSecureToken();
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays),
        };

        dbContext.Set<RefreshToken>().Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken, rawRefresh, expiresAt);
    }

    private string CreateJwt(AppUser user, DateTimeOffset expiresAt)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("fullName", user.FullName),
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
