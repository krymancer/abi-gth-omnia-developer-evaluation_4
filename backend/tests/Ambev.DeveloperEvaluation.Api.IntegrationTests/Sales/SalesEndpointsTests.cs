using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ambev.DeveloperEvaluation.Api.IntegrationTests.Infrastructure;
using Ambev.DeveloperEvaluation.Application.Auth.Login;
using Ambev.DeveloperEvaluation.Application.Auth.Register;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Domain.Users;
using FluentAssertions;

namespace Ambev.DeveloperEvaluation.Api.IntegrationTests.Sales;

[Collection(nameof(IntegrationTestCollection))]
public sealed class SalesEndpointsTests(ApiWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> AuthenticateAsync()
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var password = "Test@12345!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(
            Email: email,
            Password: password,
            FullName: "Test User",
            PhoneNumber: "+5511999999999",
            Role: UserRole.Customer));
        await EnsureSuccessAsync(registerResponse, "register");

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand(email, password));
        await EnsureSuccessAsync(loginResponse, "login");

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        return loginResult!.AccessToken;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Authentication {operation} failed with {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private static CreateSaleCommand ValidCreateCommand(string? number = null) => new(
        SaleNumber: number ?? $"SALE-{Guid.NewGuid():N}".Substring(0, 13),
        SaleDate: DateTimeOffset.UtcNow,
        CustomerId: Guid.NewGuid(),
        CustomerName: "Integration Customer",
        BranchId: Guid.NewGuid(),
        BranchName: "Integration Branch",
        Currency: "BRL",
        Items: [new CreateSaleItemCommand("PROD-001", "Product One", 3, 100m)]);

    [Fact]
    public async Task PostSale_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sales", ValidCreateCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostSale_ValidCommand_Returns201WithLocation()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/v1/sales", ValidCreateCommand());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<CreateSaleResult>();
        result.Should().NotBeNull();
        result!.TotalAmount.Should().Be(270m);
    }

    [Fact]
    public async Task GetSale_AfterCreate_ReturnsSale()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createCmd = ValidCreateCommand();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/sales", createCmd);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSaleResult>();

        var getResponse = await _client.GetAsync($"/api/v1/sales/{created!.SaleId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sale = await getResponse.Content.ReadFromJsonAsync<GetSaleResult>();
        sale.Should().NotBeNull();
        sale!.SaleNumber.Should().Be(createCmd.SaleNumber);
    }

    [Fact]
    public async Task GetSale_NotFound_Returns404()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/sales/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSale_ExistingSale_Returns204()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/sales", ValidCreateCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSaleResult>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/sales/{created!.SaleId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteSale_AlreadyCancelled_ReturnsProblem()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/sales", ValidCreateCommand());
        var created = await createResponse.Content.ReadFromJsonAsync<CreateSaleResult>();
        await _client.DeleteAsync($"/api/v1/sales/{created!.SaleId}");

        var secondDelete = await _client.DeleteAsync($"/api/v1/sales/{created.SaleId}");

        secondDelete.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PostSale_DuplicateSaleNumber_ReturnsConflict()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cmd = ValidCreateCommand("DUP-001");

        await _client.PostAsJsonAsync("/api/v1/sales", cmd);
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/sales", cmd);

        secondResponse.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task PostSale_InvalidQuantity_Returns422WithProblemDetails()
    {
        var token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var cmd = ValidCreateCommand() with
        {
            Items = [new CreateSaleItemCommand("P1", "Product", 25, 10m)]
        };

        var response = await _client.PostAsJsonAsync("/api/v1/sales", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
