using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Api.ExceptionHandling;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, problem) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                CreateValidationProblem(httpContext, ve)),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = httpContext.Request.Path,
                })
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateValidationProblem(HttpContext httpContext, ValidationException ve)
    {
        var errors = ve.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Validation Error",
            Status = StatusCodes.Status422UnprocessableEntity,
            Instance = httpContext.Request.Path,
        };
    }
}
