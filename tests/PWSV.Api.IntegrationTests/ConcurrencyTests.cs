using System.Net;
using FluentAssertions;

namespace PWSV.Api.IntegrationTests;

public sealed class ConcurrencyTests : IAsyncLifetime
{
    private const int WorkerCount = 24;

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

    [Fact]
    public async Task MixedConcurrentOperations_AllSucceedAndBalancesEqualSumOfTransactions()
    {
        var first = await _api.CreateAccountAsync("Готівка", "UAH", 10_000m);
        var second = await _api.CreateAccountAsync("Картка", "UAH", 10_000m);
        var taxi = await _api.CreateCategoryAsync("Таксі", "Expense");
        var occurredAt = DateTime.UtcNow.AddMinutes(-10);

        var oppositeTransfers = new List<Func<Task<HttpStatusCode>>>();
        for (var i = 0; i < 80; i++)
        {
            var (source, destination) = i % 2 == 0 ? (first, second) : (second, first);
            oppositeTransfers.Add(() => TransferAsync(source, destination, 3m, occurredAt));
        }

        var oppositeStatuses = await RunConcurrentlyAsync(oppositeTransfers);

        oppositeStatuses.Should().AllSatisfy(status => status.Should().Be(HttpStatusCode.Created));
        (await _api.GetBalanceAsync(first)).Should().Be(10_000m);
        (await _api.GetBalanceAsync(second)).Should().Be(10_000m);

        var expenses = new List<(long Id, int AccountId)>();
        var transfers = new List<long[]>();
        for (var i = 0; i < 30; i++)
        {
            var (source, destination) = i % 2 == 0 ? (first, second) : (second, first);
            var (_, expense) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/expense",
                new { accountId = source, categoryId = taxi, amount = 5m, occurredAt });
            expenses.Add((expense.GetProperty("id").GetInt64(), source));
            var (_, transfer) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
                new { sourceAccountId = source, destinationAccountId = destination, amount = 7m, occurredAt });
            transfers.Add([transfer.GetProperty("source").GetProperty("id").GetInt64(), transfer.GetProperty("destination").GetProperty("id").GetInt64()]);
        }

        var totalBefore = await _api.GetBalanceAsync(first) + await _api.GetBalanceAsync(second);
        var random = new Random(7);
        var mixed = new List<Func<Task<HttpStatusCode>>>();
        foreach (var (id, accountId) in expenses)
        {
            var target = accountId == first ? second : first;
            mixed.Add(() => MoveExpenseAsync(id, target, occurredAt));
        }

        foreach (var legs in transfers)
        {
            var legId = legs[random.Next(legs.Length)];
            mixed.Add(() => DeleteTransactionAsync(legId));
        }

        for (var i = 0; i < 30; i++)
        {
            var (source, destination) = i % 2 == 0 ? (first, second) : (second, first);
            mixed.Add(() => TransferAsync(source, destination, 1m, occurredAt));
        }

        var operations = mixed.ToArray();
        random.Shuffle(operations);
        var statuses = await RunConcurrentlyAsync(operations);

        statuses.Should().AllSatisfy(status => status.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.NoContent));
        (await _api.GetBalanceAsync(first) + await _api.GetBalanceAsync(second)).Should().Be(totalBefore);
        foreach (var account in new[] { first, second })
        {
            (await GetSignedSumAsync(account)).Should().Be(await _api.GetBalanceAsync(account));
        }
    }

    private static async Task<HttpStatusCode[]> RunConcurrentlyAsync(IReadOnlyList<Func<Task<HttpStatusCode>>> operations)
    {
        var statuses = new HttpStatusCode[operations.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, operations.Count),
            new ParallelOptions { MaxDegreeOfParallelism = WorkerCount },
            async (index, _) => statuses[index] = await operations[index]());
        return statuses;
    }

    private async Task<HttpStatusCode> TransferAsync(int source, int destination, decimal amount, DateTime occurredAt)
    {
        var (status, _) = await _api.SendAsync(HttpMethod.Post, "/api/v1/transactions/transfer",
            new { sourceAccountId = source, destinationAccountId = destination, amount, occurredAt });
        return status;
    }

    private async Task<HttpStatusCode> MoveExpenseAsync(long id, int accountId, DateTime occurredAt)
    {
        var (status, _) = await _api.SendAsync(HttpMethod.Put, $"/api/v1/transactions/{id}",
            new { id, accountId, categoryId = (int?)null, amount = 5m, occurredAt, description = (string?)null, counterparty = (string?)null });
        return status;
    }

    private async Task<HttpStatusCode> DeleteTransactionAsync(long id)
    {
        var (status, _) = await _api.SendAsync(HttpMethod.Delete, $"/api/v1/transactions/{id}");
        return status;
    }

    private async Task<decimal> GetSignedSumAsync(int accountId)
    {
        var sum = 0m;
        for (var page = 1; ; page++)
        {
            var (_, body) = await _api.SendAsync(HttpMethod.Get, $"/api/v1/transactions?accountId={accountId}&page={page}&pageSize=200");
            foreach (var item in body.GetProperty("items").EnumerateArray())
            {
                var amount = ApiTestClient.ParseMoney(item.GetProperty("amount"));
                sum += item.GetProperty("direction").GetString() == "Incoming" ? amount : -amount;
            }

            if (page >= body.GetProperty("totalPages").GetInt32())
            {
                return sum;
            }
        }
    }
}
