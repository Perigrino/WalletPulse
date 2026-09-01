namespace WalletPulse.Contracts.Request;

public class UpdateCustomerWalletRequest
{
    public required Guid Id { get; set; }
    public required string WalletName { get; set; }
    public required string Type { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountScheme { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Owner { get; set; }
    public Guid CustomerId { get; set; }
}