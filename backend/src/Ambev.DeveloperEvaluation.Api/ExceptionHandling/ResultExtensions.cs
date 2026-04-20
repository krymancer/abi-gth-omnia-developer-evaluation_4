using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Api.ExceptionHandling;

internal static class ResultExtensions
{
    public static IResult ToProblem(this Error error, HttpContext context)
    {
        var (statusCode, type) = error.Code switch
        {
            var c when c.StartsWith("NotFound.") => (StatusCodes.Status404NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            var c when c.StartsWith("Conflict.") => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            var c when c.StartsWith("Validation.") => (StatusCodes.Status422UnprocessableEntity, "https://tools.ietf.org/html/rfc4918#section-11.2"),
            _ => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc9110#section-15.5.1")
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            type: type,
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier });
    }
}
