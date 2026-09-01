using WalletPulse.Application.Model;

namespace WalletPulse.Application.Interface;

public interface ICustomerWalletRepository
{
    Task<IEnumerable<CustomerWallet>> GetCustomerWalletsAsync(CancellationToken token = default);
    Task<CustomerWallet> GetWalletByWalletId(Guid walletId, CancellationToken token = default);
    Task<bool> CreateCustomerWallet(CustomerWallet wallet , CancellationToken token = default);
    Task<bool> UpdateCustomerWallet(CustomerWallet wallet , CancellationToken token = default);
    Task<bool> DeleteCustomerWallet(Guid walletId, CancellationToken token = default);
    Task<bool> WalletExists(Guid walletId, CancellationToken token = default);
    Task<bool> CustomerWalletExists(string accountNumber , CancellationToken token = default);
    Task<bool> Save(CancellationToken token = default);
}