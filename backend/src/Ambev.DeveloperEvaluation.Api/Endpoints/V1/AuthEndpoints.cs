using Ambev.DeveloperEvaluation.Api.ExceptionHandling;
using Ambev.DeveloperEvaluation.Application.Auth.Login;
using Ambev.DeveloperEvaluation.Application.Auth.Refresh;
using Ambev.DeveloperEvaluation.Application.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Api.Endpoints.V1;

internal static class AuthEndpoints
{
    internal static RouteGroupBuilder MapAuthV1(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth").AllowAnonymous();

        auth.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithSummary("Register a new user")
            .Produces<RegisterResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        auth.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate and get tokens")
            .Produces<LoginResult>()
            .Produces(StatusCodes.Status422UnprocessableEntity);

        auth.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .Produces<LoginResult>()
            .Produces(StatusCodes.Status422UnprocessableEntity);

        return auth;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterCommand command,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? Results.Created($"/api/v1/users/{result.Value.UserId}", result.Value)
            : result.Error.ToProblem(context);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginCommand command,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem(context);
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshCommand command,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem(context);
    }
}
