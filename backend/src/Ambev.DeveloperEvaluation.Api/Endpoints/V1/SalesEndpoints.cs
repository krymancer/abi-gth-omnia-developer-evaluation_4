using Ambev.DeveloperEvaluation.Api.ExceptionHandling;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Api.Endpoints.V1;

internal static class SalesEndpoints
{
    internal static RouteGroupBuilder MapSalesV1(this RouteGroupBuilder group)
    {
        var sales = group.MapGroup("/sales").RequireAuthorization();

        sales.MapPost("/", CreateSaleAsync)
            .WithName("CreateSale")
            .WithSummary("Create a new sale")
            .RequireRateLimiting("api")
            .Produces<CreateSaleResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

        sales.MapGet("/{id:guid}", GetSaleAsync)
            .WithName("GetSale")
            .WithSummary("Get sale by ID")
            .CacheOutput("sales")
            .Produces<GetSaleResult>()
            .Produces(StatusCodes.Status404NotFound);

        sales.MapGet("/", ListSalesAsync)
            .WithName("ListSales")
            .WithSummary("List sales with pagination")
            .CacheOutput("salesList")
            .Produces<ListSalesResult>();

        sales.MapPut("/{id:guid}", UpdateSaleAsync)
            .WithName("UpdateSale")
            .WithSummary("Update sale items")
            .RequireRateLimiting("api")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        sales.MapDelete("/{id:guid}", CancelSaleAsync)
            .WithName("CancelSale")
            .WithSummary("Cancel a sale")
            .RequireRateLimiting("api")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        sales.MapDelete("/{saleId:guid}/items/{itemId:guid}", CancelSaleItemAsync)
            .WithName("CancelSaleItem")
            .WithSummary("Cancel a sale item")
            .RequireRateLimiting("api")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return sales;
    }

    private static async Task<IResult> CreateSaleAsync(
        [FromBody] CreateSaleCommand command,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetSale", new { id = result.Value.SaleId }, result.Value)
            : result.Error.ToProblem(context);
    }

    private static async Task<IResult> GetSaleAsync(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetSaleQuery(id), ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }

    private static async Task<IResult> ListSalesAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListSalesQuery(
            page < 1 ? 1 : page,
            pageSize is < 1 or > 100 ? 20 : pageSize,
            sortBy,
            sortDescending), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateSaleAsync(
        Guid id,
        [FromBody] UpdateSaleCommand command,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var cmd = command with { SaleId = id };
        var result = await sender.Send(cmd, ct);
        return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem(context);
    }

    private static async Task<IResult> CancelSaleAsync(
        Guid id,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(new CancelSaleCommand(id), ct);
        return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem(context);
    }

    private static async Task<IResult> CancelSaleItemAsync(
        Guid saleId,
        Guid itemId,
        ISender sender,
        HttpContext context,
        CancellationToken ct)
    {
        var result = await sender.Send(new CancelSaleItemCommand(saleId, itemId), ct);
        return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem(context);
    }
}
