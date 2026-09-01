using WalletPulse.Application.Interface;
using WalletPulse.ContractMappings;
using WalletPulse.Contracts.Request;
using WalletPulse.Contracts.Response;
using Microsoft.AspNetCore.Mvc;

namespace WalletPulse.Controllers;

[ApiController]
public class CustomerController :Controller
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    //GET all Customers
    [HttpGet(ApiEndpoints.Customers.GetAll)]
    public async Task<IActionResult> GetCustomers(CancellationToken token)
    {
        var customer = await _customerRepository.GetCustomerAsync(token);
        var customerResponse = new FinalResponse<CustomersResponse>
        {
            StatusCode = 200,
            Message = "Customers retrieved successfully.",
            Data = customer.MapsToResponse()
        };
        return Ok(customerResponse);
    }
    
    //GET CustomerById
    [HttpGet(ApiEndpoints.Customers.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken token)
    {
        var customer = await _customerRepository.GetCustomerById(id, token);
        if (customer == null)
        {
            return NotFound(new FinalResponse<object>
            {
                StatusCode = 404,
                Message = "Customer not found."
            });
        }
        
        var customerResponse = new FinalResponse<CustomerResponse>
        {
            StatusCode = 200,
            Message = "Customer retrieved successfully.",
            Data = customer.MapsToResponse()
        };
        return Ok(customerResponse);
    }
    
    
    //GET CustomerById
    // [HttpGet(ApiEndpoints.Customers.GetWallet)]
    // public async Task<IActionResult> GetWalletsByCustomerId([FromRoute] Guid id)
    // {
    //     var customer = await _customerRepository.GetWalletByCustomerId(id);
    //     if (customer == null)
    //     {
    //         return NotFound(new FinalResponse<object>
    //         {
    //             StatusCode = 404,
    //             Message = "Customer not found."
    //         });
    //     }
    //     
    //     var customerResponse = new FinalResponse<object>
    //     {
    //         StatusCode = 200,
    //         Message = "Customer retrieved successfully.",
    //         Data = customer.MapsToResponse()
    //     };
    //     return Ok(customerResponse);
    // }
    
    //POST Customer
    
    [HttpPost(ApiEndpoints.Customers.Create)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken token)
    {
        if (request == null)
        {
            return BadRequest(new FinalResponse<object>() { StatusCode = 400,Message = "Customer data is invalid." });
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new FinalResponse<object> { StatusCode = 400, Message = "Validation failed.", Data = ModelState });
        }
        
        var mapToCustomer = request.MapToCustomer();
        await _customerRepository.CreateCustomer(mapToCustomer ?? throw new InvalidOperationException(), token);
        var customerResponse = new FinalResponse<CustomerResponse>
        {
            StatusCode = 201,
            Message = "Customer created successfully.",
            Data = mapToCustomer.MapsToResponse()
        };
        return CreatedAtAction(nameof(Get), new { id = mapToCustomer.Id }, customerResponse);
    }
    
    //UPDATE Customer
    [HttpPut(ApiEndpoints.Customers.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken token)
    {
        if (request == null)
        {
            return BadRequest(new FinalResponse<object>() { StatusCode = 400,Message = "Customer data is invalid." });
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new FinalResponse<object> { StatusCode = 400, Message = "Validation failed.", Data = ModelState });
        }
        var mapToCustomer = request.MapToCustomer(id);
        var updatedCustomer = await _customerRepository.UpdateCustomer(mapToCustomer, token);
        if (updatedCustomer is false)
        {
            return NotFound(new FinalResponse<object>
            {
                StatusCode = 404,
                Message = "Customer not found."
            });
        }
        var response = new FinalResponse<CustomerResponse>
        {
            StatusCode = 200,
            Message = "Customer details updated successfully.",
            Data = mapToCustomer.MapsToResponse()
        };
        return Ok(response);

    }

    //DELETE Customer 
    [HttpDelete(ApiEndpoints.Customers.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _customerRepository.CustomerExists(id, token);
        var deleteCustomer = await _customerRepository.DeleteCustomer(id, token);
        if (!deleteCustomer)
        {
            return NotFound(new FinalResponse<string>
            {
                StatusCode = 404,
                Message = "Customer not found or already deleted",
                Data = null
            });
        }
        
        return Ok(new FinalResponse<string>
            {
                StatusCode = 200,
                Message = "Customer deleted successfully",
                Data = null
            });
    }
}