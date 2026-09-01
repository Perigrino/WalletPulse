using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletMovementTests
{
    private static (AppDbContext Context, CustomerWalletRepository Repo) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"mov-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        return (context, new CustomerWalletRepository(context));
    }

    private static async Task<CustomerWallet> SeedWalletAsync(AppDbContext context, decimal balance = 100m)
    {
        var wallet = new CustomerWallet
        {
            Id = Guid.NewGuid(),
            WalletName = "Personal",
            Type = "momo",
            AccountNumber = "0244000001",
            AccountScheme = "MTN",
            CreatedAt = DateTime.UtcNow,
            Owner = "user-1",
            CustomerId = Guid.NewGuid(),
            Balance = balance
        };
        context.CustomerWallets.Add(wallet);
        await context.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task Deposit_IncreasesBalance_AndRecordsTransaction()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        var updated = await repo.ApplyMovementAsync(wallet.Id, TransactionType.Deposit, 50m, "ref-1");

        Assert.NotNull(updated);
        Assert.Equal(150m, updated!.Balance);
        Assert.Equal(1, await context.Transactions.CountAsync());
        var transaction = await context.Transactions.FirstAsync();
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal("ref-1", transaction.Reference);
    }

    [Fact]
    public async Task Withdraw_WithinBalance_DecreasesBalance()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        var updated = await repo.ApplyMovementAsync(wallet.Id, TransactionType.Withdrawal, 30m, null);

        Assert.NotNull(updated);
        Assert.Equal(70m, updated!.Balance);
        Assert.Equal(1, await context.Transactions.CountAsync());
    }

    [Fact]
    public async Task Withdraw_ExceedingBalance_ThrowsAndWritesNothing()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.ApplyMovementAsync(wallet.Id, TransactionType.Withdrawal, 150m, null));

        Assert.Equal(100m, (await context.CustomerWallets.FirstAsync()).Balance);
        Assert.Equal(0, await context.Transactions.CountAsync());
    }

    [Fact]
    public async Task ApplyMovement_MissingWallet_ReturnsNull()
    {
        var (context, repo) = Create();
        await using var _ = context;

        var result = await repo.ApplyMovementAsync(Guid.NewGuid(), TransactionType.Deposit, 50m, null);

        Assert.Null(result);
    }
}
