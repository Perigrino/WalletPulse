using WalletPulse.Application.Database;
using WalletPulse.Application.Interface;
using WalletPulse.Application.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace WalletPulse.Application.Repository;

public sealed record WalletQueryResult(IReadOnlyList<CustomerWallet> Items, int TotalCount);

public class CustomerWalletRepository : ICustomerWalletRepository
{
    private readonly AppDbContext _context;

    public CustomerWalletRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CustomerWallet>> GetCustomerWalletsAsync(CancellationToken token = default)
    {
        var wallet = await _context.CustomerWallets
            .OrderBy(createdAt => createdAt.CreatedAt).ToListAsync(cancellationToken: token);
        return wallet;
    }

    public async Task<WalletQueryResult> GetCustomerWalletsPagedAsync(WalletFilter filter, CancellationToken token = default)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.CustomerWallets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(w => w.WalletName.ToLower().Contains(filter.Name.ToLower()));
        }
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            query = query.Where(w => w.Type == filter.Type);
        }
        if (!string.IsNullOrWhiteSpace(filter.AccountScheme))
        {
            query = query.Where(w => w.AccountScheme == filter.AccountScheme);
        }

        var totalCount = await query.CountAsync(cancellationToken: token);
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken: token);

        return new WalletQueryResult(items, totalCount);
    }

    public async Task<CustomerWallet?> GetWalletByWalletId(Guid walletId, CancellationToken token = default)
    {
        return await _context.CustomerWallets
            .FirstOrDefaultAsync(wallet => wallet.Id == walletId, cancellationToken: token);
    }

    public async Task<bool> CreateCustomerWallet(CustomerWallet wallet, CancellationToken token = default)
    {
        await _context.AddAsync(wallet, token);
        return await Save(token);
    }

    public async Task<bool> UpdateCustomerWallet(CustomerWallet wallet, CancellationToken token = default)
    {
        var result = await _context.CustomerWallets
            .FirstOrDefaultAsync(id => id.Id == wallet.Id, cancellationToken: token);
        if (result == null)
        {
            return false;
        }
        {
            result.WalletName = wallet.WalletName;
            result.Type = wallet.Type;
            result.AccountNumber = wallet.AccountNumber;
            result.AccountScheme = wallet.AccountScheme;
            result.CreatedAt = DateTime.UtcNow;
            result.Owner = wallet.Owner;
            result.CustomerId = wallet.CustomerId;
        }
        return await Save();
    }

    public async Task<bool> DeleteCustomerWallet(Guid walletId, CancellationToken token = default)
    {
        var result = await _context.CustomerWallets
            .FirstOrDefaultAsync(id => id.Id == walletId, cancellationToken: token);
        if (result == null)
        {
            return false; // Wallet not found or already deleted
        }
        _context.Remove(result);
        return await Save(token);
    }

    public async Task<bool> WalletExists(Guid id, CancellationToken token = default)
    {
        var wallet = await _context.CustomerWallets
            .AnyAsync(w => w.Id == id, cancellationToken: token);
        return wallet;
    }

    public async Task<bool> CustomerWalletExists(string accountNumber, CancellationToken token = default)
    {
        var wallet = await _context.CustomerWallets
            .AnyAsync(an => an.AccountNumber == accountNumber, cancellationToken: token);
        return wallet;
    }

    public async Task<CustomerWallet?> ApplyMovementAsync(Guid walletId, TransactionType type, decimal amount, string? reference, CancellationToken token = default)
    {
        var wallet = await _context.CustomerWallets.FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken: token);
        if (wallet == null)
        {
            return null;
        }
        if (type == TransactionType.Withdrawal && amount > wallet.Balance)
        {
            throw new InvalidOperationException("Insufficient funds.");
        }
        wallet.Balance += type == TransactionType.Deposit ? amount : -amount;
        _context.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Type = type,
            Amount = amount,
            Reference = reference,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(token);
        return wallet;
    }

    public async Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(Guid walletId, CancellationToken token = default)
    {
        return await _context.Transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken: token);
    }

    public async Task<bool> Save(CancellationToken token = default)
    {
        var saved = await _context.SaveChangesAsync(token);
        return saved > 0;
    }
}
