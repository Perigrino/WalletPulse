using System.Text.Json.Serialization;

namespace WalletPulse.Application.Model;

public class CustomerWallet
{
    public required Guid Id { get; set; }
    public required string WalletName { get; set; }
    public required string Type { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountScheme { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string Owner { get; set; }
    public decimal Balance { get; set; }
    public Guid CustomerId { get; set; }
    [JsonIgnore] public Customer? Customer { get; set; }

}