using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Interface;

public sealed record WalletFilter(string? Name, string? Type, string? AccountScheme, int Page, int PageSize);

public interface ICustomerWalletRepository
{
    Task<IEnumerable<CustomerWallet>> GetCustomerWalletsAsync(CancellationToken token = default);
    Task<WalletQueryResult> GetCustomerWalletsPagedAsync(WalletFilter filter, CancellationToken token = default);
    Task<CustomerWallet?> GetWalletByWalletId(Guid walletId, CancellationToken token = default);
    Task<bool> CreateCustomerWallet(CustomerWallet wallet, CancellationToken token = default);
    Task<bool> UpdateCustomerWallet(CustomerWallet wallet, CancellationToken token = default);
    Task<bool> DeleteCustomerWallet(Guid walletId, CancellationToken token = default);
    Task<bool> WalletExists(Guid walletId, CancellationToken token = default);
    Task<bool> CustomerWalletExists(string accountNumber, CancellationToken token = default);
    Task<CustomerWallet?> ApplyMovementAsync(Guid walletId, TransactionType type, decimal amount, string? reference, CancellationToken token = default);
    Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(Guid walletId, CancellationToken token = default);
    Task<bool> Save(CancellationToken token = default);
}