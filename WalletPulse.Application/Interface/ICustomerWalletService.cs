namespace WalletPulse.Application.Interface;

public interface ICustomerWalletService
{
    Task<bool> HasReachedMaxWallets(Guid customerId, CancellationToken token = default);
    Task<bool> CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default);
}
