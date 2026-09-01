using WalletPulse.Application.Interface;

namespace WalletPulse.Application.Service;

public class CustomerWalletService : ICustomerWalletService
{
    private const int MaxWalletsPerCustomer = 5;

    private readonly ICustomerRepository _customerRepository;

    public CustomerWalletService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<bool> HasReachedMaxWallets(Guid customerId, CancellationToken token = default)
    {
        var customer = await _customerRepository.GetCustomerById(customerId, token);
        return customer?.CustomerWallets.Count >= MaxWalletsPerCustomer;
    }

    public async Task<bool> CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default)
    {
        var customer = await _customerRepository.GetCustomerById(customerId, token);
        return customer?.CustomerWallets
            .Any(wallet => wallet.AccountNumber == accountNumber) ?? false;
    }
}
