namespace WalletPulse.Contracts.Request;

public class WalletMovementRequest
{
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
}
