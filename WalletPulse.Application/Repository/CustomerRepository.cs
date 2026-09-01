using WalletPulse.Application.Database;
using WalletPulse.Application.Interface;
using WalletPulse.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace WalletPulse.Application.Repository;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Customer>> GetCustomerAsync(CancellationToken token = default)
    {
        var customers = await _context.Customers
            .Include(wallets => wallets.CustomerWallets)
            .ToListAsync(cancellationToken: token);
        return customers;
    }

    public async Task<Customer?> GetCustomerById(Guid id, CancellationToken token = default)
    {
        return await _context.Customers
            .Include(wallets => wallets.CustomerWallets)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: token);
    }

    // public async Task<CustomerWallet> GetWalletByCustomerId(Guid id)
    // {
    //     var result = await _context.CustomerWallets
    //         .FindAsync(id);
    //     return result ?? throw new InvalidOperationException();
    //     
    //     //throw new NotImplementedException();
    // }

    public async Task<bool> CreateCustomer(Customer customer, CancellationToken token = default)
    {
        await _context.AddAsync(customer, token);
        return await Save(token);
    }

    public async Task<bool> UpdateCustomer(Customer customer, CancellationToken token = default)
    {
        var result = await _context.Customers.FirstOrDefaultAsync(p =>
            p.Id == customer.Id, cancellationToken: token);

        if (result != null)
        {
            result.FirstName = customer.FirstName;
            result.LastName = customer.LastName;
            result.Address = customer.Address;
            result.Email = customer.Email;
            result.BirthDate = customer.BirthDate;
            result.PhoneNumber = customer.PhoneNumber;
        }
        return await Save(token);
    }

    public async Task<bool> DeleteCustomer(Guid id, CancellationToken token = default)
    {
        var result = await _context.Customers.FirstOrDefaultAsync(i =>
            i.Id == id, cancellationToken: token);

        if (result == null)
        {
            return false; // Customer not found or already deleted
        }
        _context.Remove(result);
        return await Save(token);
    }

    public async Task<bool> CustomerExists(Guid id, CancellationToken token = default)
    {
        var customer = await _context.Customers.AnyAsync(c => c.Id == id, cancellationToken: token);
        return customer;
    }

    public async Task<bool> Save(CancellationToken token = default)
    {
        var saved = await _context.SaveChangesAsync(token);
        return saved > 0;
    }
}