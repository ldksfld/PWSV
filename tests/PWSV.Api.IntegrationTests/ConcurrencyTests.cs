using System.Net;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class ConcurrencyTests : IAsyncLifetime
{
    private readonly PwsvWebApplicationFactory _factory = new();
    private ApiTestClient _api = null!;

    public async Task InitializeAsync()
    {
        _api = new ApiTestClient(_factory.CreateClient());
        await _api.RegisterAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeDatabaseAsync();
        _factory.Dispose();
    }

    [Fact]
    public async Task ConcurrentOppositeTransfers_AllSucceedAndKeepBalancesConsistent()
    {
        var first = await _api.CreateAccountAsync("Готівка", "UAH", 10_000m);
        var second = await _api.CreateAccountAsync("Картка", "UAH", 10_000m);
        var occurredAt = DateTime.UtcNow.AddMinutes(-1);

        var statuses = await Task.WhenAll(Enumerable.Range(0, 30).Select(async i =>
        {
            var (source, destination) = i % 2 == 0 ? (first, second) : (second, first);
            var (status, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
                new { sourceAccountId = source, destinationAccountId = destination, amount = 3m, occurredAt });
            return status;
        }));

        statuses.Should().AllSatisfy(status => status.Should().Be(HttpStatusCode.Created));
        (await _api.GetBalanceAsync(first)).Should().Be(10_000m);
        (await _api.GetBalanceAsync(second)).Should().Be(10_000m);
    }

    [Fact]
    public async Task ConcurrentDeletesOfSameTransfer_RemoveItExactlyOnce()
    {
        var first = await _api.CreateAccountAsync("Готівка", "UAH", 1_000m);
        var second = await _api.CreateAccountAsync("Картка", "UAH");
        var (_, transfer) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
            new { sourceAccountId = first, destinationAccountId = second, amount = 250m, occurredAt = DateTime.UtcNow.AddMinutes(-1) });
        var legs = new[] { transfer.GetProperty("source").GetProperty("id").GetInt64(), transfer.GetProperty("destination").GetProperty("id").GetInt64() };

        var statuses = await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            var (status, _) = await _api.SendAsync(HttpMethod.Delete, $"/api/v1/transactions/{legs[i % 2]}");
            return status;
        }));

        statuses.Count(status => status == HttpStatusCode.NoContent).Should().Be(1);
        statuses.Should().AllSatisfy(status => status.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound, HttpStatusCode.Conflict));
        (await _api.GetBalanceAsync(first)).Should().Be(1_000m);
        (await _api.GetBalanceAsync(second)).Should().Be(0m);
    }
}
