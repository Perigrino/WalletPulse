namespace WalletPulse.Application.Model;

public class Transaction
{
    public required Guid Id { get; set; }
    public required Guid WalletId { get; set; }
    public required TransactionType Type { get; set; }
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2
}
