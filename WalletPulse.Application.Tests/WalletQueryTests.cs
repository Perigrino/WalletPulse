using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Interface;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletQueryTests
{
    private static async Task<AppDbContext> SeedAsync(int count)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"q-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        for (var i = 0; i < count; i++)
        {
            context.CustomerWallets.Add(new CustomerWallet
            {
                Id = Guid.NewGuid(),
                WalletName = i % 2 == 0 ? "Personal" : "Savings",
                Type = i % 3 == 0 ? "card" : "momo",
                AccountNumber = $"02440000{i:00}",
                AccountScheme = i % 2 == 0 ? "MTN" : "VISA",
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                Owner = "user-1",
                CustomerId = Guid.NewGuid()
            });
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task GetPaged_Defaults_ReturnsFirstPageOf20()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 1, 20));

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRemaining5()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 2, 20));

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_FilterByType_ReturnsOnlyMatching()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, "card", null, 1, 20));

        Assert.Equal(9, result.TotalCount);
        Assert.All(result.Items, w => Assert.Equal("card", w.Type));
    }

    [Fact]
    public async Task GetPaged_NameContains_IsCaseInsensitive()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter("personal", null, null, 1, 20));

        Assert.Equal(13, result.TotalCount);
        Assert.All(result.Items, w => Assert.Contains("Personal", w.WalletName));
    }

    [Fact]
    public async Task GetPaged_OrdersNewestFirst()
    {
        var context = await SeedAsync(5);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 1, 20));

        var createdDates = result.Items.Select(w => w.CreatedAt).ToList();
        Assert.Equal(createdDates.OrderByDescending(d => d), createdDates);
    }

    [Fact]
    public async Task GetPaged_PageSizeCappedAt100()
    {
        var context = await SeedAsync(150);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 1, 500));

        Assert.Equal(100, result.Items.Count);
    }
}
