using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;
using WalletPulse.Application.Service;

namespace WalletPulse.Application.Tests;

public class WalletServiceTests
{
    private static (AppDbContext Context, CustomerRepository CustomerRepo) CreateRepos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"svc-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        return (context, new CustomerRepository(context));
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext context, int walletCount)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Email = "john@example.com",
            PhoneNumber = "+233200000001",
            Address = "Accra"
        };
        for (var i = 0; i < walletCount; i++)
        {
            context.CustomerWallets.Add(new CustomerWallet
            {
                Id = Guid.NewGuid(),
                WalletName = $"Wallet {i}",
                Type = "momo",
                AccountNumber = $"024400000{i}",
                AccountScheme = "MTN",
                CreatedAt = DateTime.UtcNow,
                Owner = "user-1",
                CustomerId = customer.Id
            });
        }
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task HasReachedMaxWallets_AtLimit_ReturnsTrue()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 5);
        var service = new CustomerWalletService(customerRepo);

        var reached = await service.HasReachedMaxWallets(customer.Id);

        Assert.True(reached);
    }

    [Fact]
    public async Task HasReachedMaxWallets_BelowLimit_ReturnsFalse()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 2);
        var service = new CustomerWalletService(customerRepo);

        var reached = await service.HasReachedMaxWallets(customer.Id);

        Assert.False(reached);
    }

    [Fact]
    public async Task CustomerWalletExists_DuplicateAccountNumber_ReturnsTrue()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 1);
        var service = new CustomerWalletService(customerRepo);

        var exists = await service.CustomerWalletExists(customer.Id, "0244000000");

        Assert.True(exists);
    }

    [Fact]
    public async Task CustomerWalletExists_NewAccountNumber_ReturnsFalse()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 1);
        var service = new CustomerWalletService(customerRepo);

        var exists = await service.CustomerWalletExists(customer.Id, "0244999999");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetCustomerById_Missing_ReturnsNull()
    {
        var (context, customerRepo) = CreateRepos();
        await using var _ = context;

        var customer = await customerRepo.GetCustomerById(Guid.NewGuid());

        Assert.Null(customer);
    }
}
