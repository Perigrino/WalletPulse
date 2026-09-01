using WalletPulse.Application.Model;

namespace WalletPulse.Contracts.Response;

public class CustomerResponse
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Address { get; set; }
    public List<CustomerWallet> CustomerWallets { get; set; } = new List<CustomerWallet>();
}