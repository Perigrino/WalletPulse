# WalletPulse

A .NET 10 wallet-management API for digital wallet operations — create and manage customer wallets, track balances, record deposits and withdrawals, and query wallets with filtering and pagination.

## What it does

- **Customers** — create, read, update, delete customers with basic profile fields.
- **Wallets** — create wallets (with 5-wallet limit per customer, duplicate-account detection), read/update/delete, and list with filtering by name/type/scheme plus pagination.
- **Balances & Transactions** — wallets have a `Balance`; deposit and withdraw endpoints update the balance atomically and write a `Transaction` row (`Deposit`/`Withdrawal`) with a reference note and timestamp.
- **Validation** — request validation via FluentValidation; errors return RFC 7807 `ProblemDetails` with per-field messages.
- **Error handling** — a global exception handler returns 500 `ProblemDetails` without leaking stack traces; missing wallets return 404.
- **Health** — `/health` endpoint for load-balancer / readiness checks.

## Stack

- .NET 10 (SDK 10.0.400)
- ASP.NET Core Web API (`WalletPulse.API`)
- EF Core 10 + Npgsql 10 (PostgreSQL)
- FluentValidation 12
- Swashbuckle 10 (OpenAPI / Swagger)

## Running locally

```bash
dotnet restore
dotnet build
dotnet ef database update --project WalletPulse.Application
dotnet run --project WalletPulse.API
```

Requires PostgreSQL (`Host=localhost;Port=5432;Database=Hubtel.Wallets`; credentials in `appsettings.json`).

## Project layout

```
WalletPulse.API          → Controllers, mappings, endpoints, Program.cs
WalletPulse.Application  → Domain models, EF DbContext, repositories, services, migrations, validators
WalletPulse.Contracts    → DTOs (Request / Response)
WalletPulse.Application.Tests → xUnit tests (InMemory EF provider)
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). All changes should include passing tests (`dotnet test`) and a clean build (`dotnet build`).

## License

MIT — see [LICENSE](LICENSE).
