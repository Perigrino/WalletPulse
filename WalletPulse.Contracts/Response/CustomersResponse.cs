using WalletPulse.Application.Model;

namespace WalletPulse.Contracts.Response;

public class CustomersResponse
{
    public required IEnumerable<CustomerResponse> Customers { get; init; } = Enumerable.Empty<CustomerResponse>();
}