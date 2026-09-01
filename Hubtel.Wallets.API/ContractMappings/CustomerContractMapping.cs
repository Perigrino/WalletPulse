using Hubtel.Wallets.Application.Model;
using Hubtel.Wallets.Contracts.Request;
using Hubtel.Wallets.Contracts.Response;

namespace Hubtel.Wallets.ContractMappings;

public static class CustomerContractMapping
{
    public static Customer MapToCustomer(this CreateCustomerRequest request)  //This maps the CreateCustomerDto to Customer
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            BirthDate = DateTime.UtcNow,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
 
        };
    
    }
    
    public static Customer MapToCustomer(this UpdateCustomerRequest request, Guid id) //This maps the UpdateCustomerDto to Customer
    {
        return new Customer()
        {
            Id = id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Email = request.Email,
            BirthDate = DateTime.UtcNow
        };
    }
    
    public static CustomerResponse MapsToResponse(this Customer customer) //This maps the Customer to CustomerResponse Dto
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            Email = customer.Email,
            BirthDate = customer.BirthDate,
            CustomerWallets = customer.CustomerWallets
        };
    }
    
    
    public static CustomersResponse MapsToResponse(this IEnumerable<Customer> customers) //This maps the list of customers to the CustomersResponses
    {
        return new CustomersResponse()
        {
            Customers = customers.Select(MapsToResponse)
        };
    }
}