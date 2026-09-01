using WalletPulse.Application.Interface;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WalletPulse.Application.Service;

public class CustomerWalletService : ICustomerWalletService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerWalletService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }
    public bool HasReachedMaxWallets(Guid customerId, CancellationToken token = default)
    {
        var wallets = _customerRepository.GetCustomerById(customerId);
        if (wallets == null)
        {
            throw new Exception("Customer not found");
        }
        var numberOfWallet = wallets.Result.CustomerWallets.Count;
        
        return numberOfWallet >= 5;
    }

    public bool CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default)
    {
        var result = _customerRepository.GetCustomerById(customerId);
        if (result == null)
        {
            var walletExists = result?.Result.CustomerWallets;
            if (walletExists != null)
            {
                var wallet = walletExists.FirstOrDefault(ac => ac.AccountNumber == accountNumber);
            }
            return true;
        }

        return false;
    }
}