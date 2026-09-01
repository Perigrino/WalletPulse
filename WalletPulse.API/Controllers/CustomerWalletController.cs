using WalletPulse.Application.Interface;
using WalletPulse.ContractMappings;
using WalletPulse.Contracts.Request;
using WalletPulse.Contracts.Response;
using Microsoft.AspNetCore.Mvc;

namespace WalletPulse.Controllers;


[ApiController]
public class CustomerWalletController : Controller
{
    private readonly ICustomerWalletRepository _walletRepository;
    private readonly ICustomerWalletService _walletService;

    public CustomerWalletController(ICustomerWalletRepository walletRepository, ICustomerWalletService walletService)
    {
        _walletRepository = walletRepository;
        _walletService = walletService;
    }
    
    //GET all Wallets
    [HttpGet(ApiEndpoints.CustomerWallet.GetAll)]
    public async Task<IActionResult> GetCustomerWallets(CancellationToken token)
    {
        var wallets = await _walletRepository.GetCustomerWalletsAsync(token);
        var walletResponse = new FinalResponse<CustomerWalletsResponse>
        {
            StatusCode = 200,
            Message = "Wallets retrieved successfully.",
            Data = wallets.MapsToResponse()
        };
        return Ok(walletResponse);
    }
    
    //GET WalletByWalletsId
    [HttpGet(ApiEndpoints.CustomerWallet.Get)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken token)
    {
        var wallet = await _walletRepository.GetWalletByWalletId(id, token);
        if (wallet == null)
        {
            return NotFound(new FinalResponse<object>
            {
                StatusCode = 404,
                Message = "Wallet not found."
            });
        }
        
        var customerResponse = new FinalResponse<CustomerWalletResponse>
        {
            StatusCode = 200,
            Message = "Wallet retrieved successfully.",
            Data = wallet.MapsToResponse()
        };
        return Ok(customerResponse);
    }
    
    //POST Wallet
    [HttpPost(ApiEndpoints.CustomerWallet.Create)]
    public async Task<IActionResult> CreateCustomerWallet([FromBody] CreateCustomerWalletRequest request, CancellationToken token)
    {
        if (request == null)
        {
            return BadRequest(new FinalResponse<object>() { StatusCode = 400,Message = "Wallet data is invalid." });
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new FinalResponse<object> { StatusCode = 400, Message = "Validation failed.", Data = ModelState });
        }

        var maxedWalletsReached = _walletService.HasReachedMaxWallets(request.CustomerId);
        var accountWalletExists = _walletService.CustomerWalletExists(request.CustomerId, request.AccountNumber);
        if (!maxedWalletsReached)
        {
            if (accountWalletExists)
            {
                var mapToWallet = request.MapToWallet();
                await _walletRepository.CreateCustomerWallet(mapToWallet ?? throw new InvalidOperationException(), token);
                var walletResponse = new FinalResponse<CustomerWalletResponse>
                {
                    StatusCode = 201,
                    Message = "Wallet created successfully.",
                    Data = mapToWallet.MapsToResponse()
                };
                return CreatedAtAction(nameof(Get), new { id = mapToWallet.Id }, walletResponse);
            }
            
            var walletExistsResponse = new FinalResponse<object>
            {
                StatusCode = 400,
                Message = "Wallet already exist on customer's account",
                Data = null
            };
            return BadRequest(walletExistsResponse);
        }
        var walletMaxedResponse = new FinalResponse<object>
        {
            StatusCode = 400,
            Message = "Customer already has 5 wallets on account.",
            Data = null
        };
        return BadRequest(walletMaxedResponse);
    }
    
    //UPDATE Customer Wallet
    [HttpPut(ApiEndpoints.CustomerWallet.Update)]
    public async Task<IActionResult> UpdateCustomerWallet([FromRoute] Guid id, [FromBody] UpdateCustomerWalletRequest request, CancellationToken token)
    {
        if (request == null)
        {
            return BadRequest(new FinalResponse<object>() { StatusCode = 400,Message = "Wallet data is invalid." });
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new FinalResponse<object> { StatusCode = 400, Message = "Validation failed.", Data = ModelState });
        }
        var mapToWallet = request.MapToWallet(id);
        var updateWallet = await _walletRepository.UpdateCustomerWallet(mapToWallet, token);
        if (updateWallet is false)
        {
            return NotFound(new FinalResponse<object>
            {
                StatusCode = 404,
                Message = "Wallet not found."
            });
        }
        var response = new FinalResponse<CustomerWalletResponse>
        {
            StatusCode = 200,
            Message = "Customer details updated successfully.",
            Data = mapToWallet.MapsToResponse()
        };
        return Ok(response);

    }

    //DELETE Customer Wallet
    [HttpDelete(ApiEndpoints.CustomerWallet.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _walletRepository.WalletExists(id, token);
        var deleteCustomerWallet = await _walletRepository.DeleteCustomerWallet(id, token);
        if (!deleteCustomerWallet)
        {
            return NotFound(new FinalResponse<string>
            {
                StatusCode = 404,
                Message = "Customer wallet not found or already deleted",
                Data = null
            });
        }
        
        return Ok(new FinalResponse<string>
            {
                StatusCode = 200,
                Message = "Customer wallet deleted successfully",
                Data = null
            });
    }
}