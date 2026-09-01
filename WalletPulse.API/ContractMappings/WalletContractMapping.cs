using WalletPulse.Application.Model;
using WalletPulse.Contracts.Request;
using WalletPulse.Contracts.Response;

namespace WalletPulse.ContractMappings;

public static class WalletContractMapping
{
    public static CustomerWallet MapToWallet(this CreateCustomerWalletRequest request)  //This maps the CreateCustomerWalletDto to CustomerWallet
    {
        return new CustomerWallet
        {
            Id = Guid.NewGuid(),
            WalletName = request.WalletName,
            Type = request.Type,
            AccountNumber = request.AccountNumber,
            AccountScheme = request.AccountScheme,
            CreatedAt = request.CreatedAt.ToUniversalTime(),
            Owner = request.Owner,
            CustomerId = request.CustomerId
        };
    }

    public static CustomerWallet MapToWallet(this UpdateCustomerWalletRequest request, Guid id)  //This maps the UpdateCustomerWalletDto to CustomerWallet
    {
        return new CustomerWallet
        {
            Id = id,
            WalletName = request.WalletName,
            Type = request.Type,
            AccountNumber = request.AccountNumber,
            AccountScheme = request.AccountScheme,
            CreatedAt = request.CreatedAt.ToUniversalTime(),
            Owner = request.Owner,
            CustomerId = request.CustomerId
        };
    }

    public static CustomerWalletResponse MapsToResponse(this CustomerWallet wallet) //This maps the Customer to CustomerResponse Dto
    {
        return new CustomerWalletResponse
        {
            Id = wallet.Id,
            WalletName = wallet.WalletName,
            Type = wallet.Type,
            AccountNumber = wallet.AccountNumber,
            AccountScheme = wallet.AccountScheme,
            CreatedAt = wallet.CreatedAt,
            Owner = wallet.Owner,
            Balance = wallet.Balance,
            CustomerId = wallet.CustomerId

        };
    }

    public static CustomerWalletsResponse MapsToResponse(this IEnumerable<CustomerWallet> wallets) //This maps the list of customers to the CustomersResponses
    {
        return new CustomerWalletsResponse()
        {
            Wallet = wallets.Select(MapsToResponse)
        };
    }
}