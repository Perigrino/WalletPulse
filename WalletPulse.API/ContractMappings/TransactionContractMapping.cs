using WalletPulse.Application.Model;
using WalletPulse.Contracts.Response;

namespace WalletPulse.ContractMappings;

public static class TransactionContractMapping
{
    public static TransactionResponse MapsToResponse(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        WalletId = transaction.WalletId,
        Type = transaction.Type.ToString(),
        Amount = transaction.Amount,
        Reference = transaction.Reference,
        CreatedAt = transaction.CreatedAt
    };
}
