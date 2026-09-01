namespace WalletPulse.Application.Interface;

public interface ICustomerWalletService
{
    bool HasReachedMaxWallets(Guid customerId, CancellationToken token = default);
    bool CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default);
}