namespace WalletPulse.Contracts.Response;

public class TransactionResponse
{
    public required Guid Id { get; set; }
    public required Guid WalletId { get; set; }
    public required string Type { get; set; }
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
    public required DateTime CreatedAt { get; set; }
}
