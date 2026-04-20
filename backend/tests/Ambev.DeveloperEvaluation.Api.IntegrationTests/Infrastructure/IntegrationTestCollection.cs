namespace Ambev.DeveloperEvaluation.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiWebApplicationFactory>;
