using WalletPulse.Application.Model;

namespace WalletPulse.Application.Interface;

public interface ICustomerRepository
{
    Task<IEnumerable<Customer>> GetCustomerAsync(CancellationToken token = default);
    Task<Customer?> GetCustomerById(Guid id, CancellationToken token = default);
    //Task<CustomerWallet> GetWalletByCustomerId(Guid id);
    Task<bool> CreateCustomer(Customer customer, CancellationToken token = default);
    Task<bool> UpdateCustomer(Customer customer, CancellationToken token = default);
    Task<bool> DeleteCustomer(Guid id, CancellationToken token = default);
    Task<bool> CustomerExists(Guid id, CancellationToken token = default);
    Task<bool> Save(CancellationToken token = default);
}