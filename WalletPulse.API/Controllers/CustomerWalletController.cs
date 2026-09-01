using WalletPulse.Application.Interface;
using WalletPulse.Application.Model;
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
    public async Task<IActionResult> GetCustomerWallets(
        CancellationToken token,
        string? name = null, string? type = null, string? accountScheme = null,
        int page = 1, int pageSize = 20)
    {
        var filter = new WalletFilter(name, type, accountScheme, page, pageSize);
        var result = await _walletRepository.GetCustomerWalletsPagedAsync(filter, token);
        var totalPages = (int)Math.Ceiling(result.TotalCount / (double)Math.Clamp(pageSize, 1, 100));

        var response = new FinalResponse<PagedResponse<CustomerWalletResponse>>
        {
            StatusCode = 200,
            Message = "Wallets retrieved successfully.",
            Data = new PagedResponse<CustomerWalletResponse>
            {
                Items = result.Items.Select(w => w.MapsToResponse()),
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 1, 100),
                TotalCount = result.TotalCount,
                TotalPages = totalPages
            }
        };
        return Ok(response);
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
            return BadRequest(new FinalResponse<object>() { StatusCode = 400, Message = "Wallet data is invalid." });
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new FinalResponse<object> { StatusCode = 400, Message = "Validation failed.", Data = ModelState });
        }

        var maxedWalletsReached = await _walletService.HasReachedMaxWallets(request.CustomerId, token);
        if (maxedWalletsReached)
        {
            var walletMaxedResponse = new FinalResponse<object>
            {
                StatusCode = 400,
                Message = "Customer already has 5 wallets on account.",
                Data = null
            };
            return BadRequest(walletMaxedResponse);
        }

        var accountWalletExists = await _walletService.CustomerWalletExists(request.CustomerId, request.AccountNumber, token);
        if (accountWalletExists)
        {
            var walletExistsResponse = new FinalResponse<object>
            {
                StatusCode = 400,
                Message = "Wallet already exist on customer's account",
                Data = null
            };
            return BadRequest(walletExistsResponse);
        }

        var mapToWallet = request.MapToWallet();
        await _walletRepository.CreateCustomerWallet(mapToWallet, token);
        var walletResponse = new FinalResponse<CustomerWalletResponse>
        {
            StatusCode = 201,
            Message = "Wallet created successfully.",
            Data = mapToWallet.MapsToResponse()
        };
        return CreatedAtAction(nameof(Get), new { id = mapToWallet.Id }, walletResponse);
    }

    //UPDATE Customer Wallet
    [HttpPut(ApiEndpoints.CustomerWallet.Update)]
    public async Task<IActionResult> UpdateCustomerWallet([FromRoute] Guid id, [FromBody] UpdateCustomerWalletRequest request, CancellationToken token)
    {
        if (request == null)
        {
            return BadRequest(new FinalResponse<object>() { StatusCode = 400, Message = "Wallet data is invalid." });
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

    //POST Deposit
    [HttpPost(ApiEndpoints.CustomerWallet.Deposit)]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] WalletMovementRequest request, CancellationToken token)
    {
        var wallet = await _walletRepository.ApplyMovementAsync(id, TransactionType.Deposit, request.Amount, request.Reference, token);
        if (wallet == null)
        {
            return NotFound(new FinalResponse<object> { StatusCode = 404, Message = "Wallet not found." });
        }
        return Ok(new FinalResponse<CustomerWalletResponse>
        {
            StatusCode = 200,
            Message = "Deposit successful.",
            Data = wallet.MapsToResponse()
        });
    }

    //POST Withdraw
    [HttpPost(ApiEndpoints.CustomerWallet.Withdraw)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WalletMovementRequest request, CancellationToken token)
    {
        CustomerWallet? wallet;
        try
        {
            wallet = await _walletRepository.ApplyMovementAsync(id, TransactionType.Withdrawal, request.Amount, request.Reference, token);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Insufficient funds.")
        {
            return BadRequest(new FinalResponse<object>
            {
                StatusCode = 400,
                Message = "Insufficient funds.",
                Data = null
            });
        }
        if (wallet == null)
        {
            return NotFound(new FinalResponse<object> { StatusCode = 404, Message = "Wallet not found." });
        }
        return Ok(new FinalResponse<CustomerWalletResponse>
        {
            StatusCode = 200,
            Message = "Withdrawal successful.",
            Data = wallet.MapsToResponse()
        });
    }

    //GET Transaction History
    [HttpGet(ApiEndpoints.CustomerWallet.Transactions)]
    public async Task<IActionResult> GetTransactions(Guid id, CancellationToken token)
    {
        var walletExists = await _walletRepository.WalletExists(id, token);
        if (!walletExists)
        {
            return NotFound(new FinalResponse<object> { StatusCode = 404, Message = "Wallet not found." });
        }
        var transactions = await _walletRepository.GetWalletTransactionsAsync(id, token);
        var response = new FinalResponse<IEnumerable<TransactionResponse>>
        {
            StatusCode = 200,
            Message = "Transactions retrieved successfully.",
            Data = transactions.Select(t => t.MapsToResponse())
        };
        return Ok(response);
    }
}