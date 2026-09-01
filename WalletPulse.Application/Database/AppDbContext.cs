using WalletPulse.Application.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WalletPulse.Application.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)  
    {
        modelBuilder.Entity<Customer>()
            .HasMany(u => u.CustomerWallets)
            .WithOne(w => w.Customer)
            .HasForeignKey(w => w.CustomerId);
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerWallet> CustomerWallets { get; set; }
}


public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Assuming the appsettings.json file is in the same directory
            .AddJsonFile(@Directory.GetCurrentDirectory() + "/../WalletPulse.API/appsettings.json")
            .Build();
        var connectionString = configuration.GetConnectionString("Default");
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}