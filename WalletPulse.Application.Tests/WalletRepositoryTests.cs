using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static CustomerWallet NewWallet(Guid customerId, string accountNumber) => new()
    {
        Id = Guid.NewGuid(),
        WalletName = "Personal",
        Type = "momo",
        AccountNumber = accountNumber,
        AccountScheme = "MTN",
        CreatedAt = DateTime.UtcNow,
        Owner = "user-1",
        CustomerId = customerId
    };

    [Fact]
    public async Task GetWalletByWalletId_Missing_ReturnsNull()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var result = await repository.GetWalletByWalletId(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCustomerWallet_Missing_ReturnsFalse()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var updated = await repository.UpdateCustomerWallet(NewWallet(Guid.NewGuid(), "0244000001"));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteCustomerWallet_Missing_ReturnsFalse()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var deleted = await repository.DeleteCustomerWallet(Guid.NewGuid());

        Assert.False(deleted);
    }
}
