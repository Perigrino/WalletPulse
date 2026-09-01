# WalletPulse Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebrand the solution to WalletPulse and deliver the approved wallet-management upgrades: create-wallet bug fixes, wallet balances with deposit/withdraw + transaction history, wallet search/filter/pagination, and validation/robustness hardening.

**Architecture:** 3-project ASP.NET Core API (API / Application / Contracts). Fixes and features are added in the existing repository/service/controller pattern. One additive EF migration adds `Balance` + `Transactions`. FluentValidation + an action filter + a global `IExceptionHandler` harden the pipeline. New xUnit test project with EF InMemory provider.

**Tech Stack:** .NET 10, EF Core 10.0.4, Npgsql 10.0.3, FluentValidation 12, xUnit, EF InMemory provider.

**Spec:** `docs/superpowers/specs/2026-09-01-walletpulse-design.md`

## Global Constraints

- Target framework: `net10.0` everywhere (already done).
- EF Core stays at `10.0.4` (Npgsql provider dependency floor); Npgsql `10.0.3`. Do not bump independently.
- Database name in connection string (`Hubtel.Wallets`) is NOT renamed — no data migration.
- `FinalResponse<T>` envelope stays the API shape for success responses; ProblemDetails for pipeline errors (validation 400, unhandled 500).
- No commits of secrets; nothing pushed without user request.
- All string comparisons for existence rules are case-sensitive exact (account numbers, scheme names) unless the spec says otherwise.
- Wallet max-count rule: 5 wallets per customer.
- Every task ends with: build clean (0 errors), tests green, commit.

---

### Task 1: Rename solution, projects, folders, namespaces to WalletPulse

**Files:**
- Rename: `Hubtel.Wallets.sln` → `WalletPulse.sln`
- Rename dirs/csprojs: `Hubtel.Wallets.API` → `WalletPulse.API`, `Hubtel.Wallets.Application` → `WalletPulse.Application`, `Hubtel.Wallets.Contracts` → `WalletPulse.Contracts`
- Modify: every `.cs` file's namespace and using statements; all 3 csproj (`RootNamespace`); `AppDbContextFactory` appsettings path; sln project paths.

**Interfaces:**
- Produces: namespaces `WalletPulse`, `WalletPulse.Application`, `WalletPulse.Application.Database/Model/Repository/Service/Interface`, `WalletPulse.Contracts.Request/Response`, `WalletPulse.ContractMappings`, `WalletPulse.Controllers`; sln `WalletPulse.sln`; projects `WalletPulse.*`.

- [ ] **Step 1: Rename directories and files with git mv**

```bash
git mv Hubtel.Wallets.API WalletPulse.API
git mv Hubtel.Wallets.Application WalletPulse.Application
git mv Hubtel.Wallets.Contracts WalletPulse.Contracts
git mv WalletPulse.API/Hubtel.Wallets.API.csproj WalletPulse.API/WalletPulse.API.csproj
git mv WalletPulse.Application/Hubtel.Wallets.Application.csproj WalletPulse.Application/WalletPulse.Application.csproj
git mv WalletPulse.Contracts/Hubtel.Wallets.Contracts.csproj WalletPulse.Contracts/WalletPulse.Contracts.csproj
git mv Hubtel.Wallets.sln WalletPulse.sln
```

- [ ] **Step 2: Rewrite sln references**

In `WalletPulse.sln`, replace the three Project lines' paths and names:
old `Hubtel.Wallets.API\Hubtel.Wallets.API.csproj` → `WalletPulse.API\WalletPulse.API.csproj` (same pattern for the other two projects). Project GUIDs stay the same.

- [ ] **Step 3: Rewrite namespaces and using statements in all .cs files**

```bash
# Order matters: most specific first
grep -rl 'Hubtel.Wallets' --include='*.cs' WalletPulse.API WalletPulse.Application WalletPulse.Contracts | xargs sed -i '' \
  -e 's/Hubtel\.Wallets\.Application/WalletPulse.Application/g' \
  -e 's/Hubtel\.Wallets\.Contracts/WalletPulse.Contracts/g' \
  -e 's/Hubtel\.Wallets\.ContractMappings/WalletPulse.ContractMappings/g' \
  -e 's/Hubtel\.Wallets\.Controllers/WalletPulse.Controllers/g' \
  -e 's/namespace Hubtel\.Wallets;/namespace WalletPulse;/' \
  -e 's/Hubtel\.Wallets/WalletPulse/g'
```

Also fix `AppDbContextFactory`'s hard-coded path: `../Hubtel.Wallets.API/appsettings.json` → `../WalletPulse.API/appsettings.json` (covered by the final sed rule if written exactly as `Hubtel.Wallets.API`).

- [ ] **Step 4: Update csproj RootNamespace**

`WalletPulse.API/WalletPulse.API.csproj`: `<RootNamespace>Hubtel.Wallets</RootNamespace>` → `<RootNamespace>WalletPulse</RootNamespace>`.

- [ ] **Step 5: Rename GitHub repo**

```bash
gh repo rename WalletPulse --repo Perigrino/Hubtel.Wallets --yes
```

Verify: `git remote -v` shows the new URL (gh updates origin automatically).

- [ ] **Step 6: Build + smoke check**

```bash
dotnet build
```
Expected: 0 errors, 0 warnings. Then `dotnet run` in `WalletPulse.API` briefly and curl `/api/wallet` returns 200 (DB need not be up for the rename check — endpoint routing is enough; a 500 from DB is acceptable here, routing verified by swagger 200).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor!: rebrand solution to WalletPulse

Rename projects, folders, namespaces, and solution file; GitHub repo
renamed to WalletPulse."
```

---

### Task 2: Fix wallet lookup/update/delete semantics (repository + controller 404s)

**Files:**
- Modify: `WalletPulse.Application/Repository/CustomerWalletRepository.cs`
- Modify: `WalletPulse.Application/Interface/ICustomerWalletRepository.cs`
- Modify: `WalletPulse.API/Controllers/CustomerWalletController.cs`
- Test: `WalletPulse.Application.Tests` (created in this task)

**Interfaces:**
- Consumes: `AppDbContext` (existing).
- Produces: `Task<CustomerWallet?> GetWalletByWalletId(Guid walletId, CancellationToken token = default)` (nullable return); `Task<bool> UpdateCustomerWallet(...)` returns false when wallet missing; `Task<bool> DeleteCustomerWallet(...)` unchanged signature.

- [ ] **Step 1: Create the test project**

```bash
dotnet new xunit -o WalletPulse.Application.Tests
dotnet sln WalletPulse.sln add WalletPulse.Application.Tests/WalletPulse.Application.Tests.csproj
dotnet add WalletPulse.Application.Tests/WalletPulse.Application.Tests.csproj reference WalletPulse.Application/WalletPulse.Application.csproj
dotnet add WalletPulse.Application.Tests/WalletPulse.Application.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 10.0.4
# Delete UnitTest1.cs; tests live in WalletRepositoryTests.cs
rm WalletPulse.Application.Tests/UnitTest1.cs
```

Use this csproj for the test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.4" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
        <PackageReference Include="xunit" Version="2.9.2" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
        <PackageReference Include="coverlet.collector" Version="6.0.2" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\WalletPulse.Application\WalletPulse.Application.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 2: Write failing tests for lookup semantics**

`WalletPulse.Application.Tests/WalletRepositoryTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static CustomerWallet NewWallet(Guid customerId, string accountNumber) => new()
    {
        Id = Guid.NewGuid(),
        WalletName = "Personal",
        Type = "momo",
        AccountNumber = accountNumber,
        AccountScheme = "MTN",
        CreatedAt = DateTime.UtcNow,
        Owner = "user-1",
        CustomerId = customerId
    };

    [Fact]
    public async Task GetWalletByWalletId_Missing_ReturnsNull()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var result = await repository.GetWalletByWalletId(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCustomerWallet_Missing_ReturnsFalse()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var updated = await repository.UpdateCustomerWallet(NewWallet(Guid.NewGuid(), "0244000001"));

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteCustomerWallet_Missing_ReturnsFalse()
    {
        await using var context = CreateContext();
        var repository = new CustomerWalletRepository(context);

        var deleted = await repository.DeleteCustomerWallet(Guid.NewGuid());

        Assert.False(deleted);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: compile FAIL — `Assert.Null(result)` on non-nullable `Task<CustomerWallet>` produces a warning-as-error or the interface still returns non-nullable; at minimum `GetWalletByWalletId_Missing_ReturnsNull` fails because the method throws `InvalidOperationException` today.

- [ ] **Step 4: Fix the repository and interface**

`WalletPulse.Application/Interface/ICustomerWalletRepository.cs` — change signature:

```csharp
Task<CustomerWallet?> GetWalletByWalletId(Guid walletId, CancellationToken token = default);
```

`WalletPulse.Application/Repository/CustomerWalletRepository.cs` — replace `GetWalletByWalletId` and `UpdateCustomerWallet`:

```csharp
public async Task<CustomerWallet?> GetWalletByWalletId(Guid walletId, CancellationToken token = default)
{
    return await _context.CustomerWallets
        .FirstOrDefaultAsync(wallet => wallet.Id == walletId, cancellationToken: token);
}

public async Task<bool> UpdateCustomerWallet(CustomerWallet wallet, CancellationToken token = default)
{
    var result = await _context.CustomerWallets
        .FirstOrDefaultAsync(id => id.Id == wallet.Id, cancellationToken: token);
    if (result == null)
    {
        return false;
    }
    result.WalletName = wallet.WalletName;
    result.Type = wallet.Type;
    result.AccountNumber = wallet.AccountNumber;
    result.AccountScheme = wallet.AccountScheme;
    result.CreatedAt = DateTime.UtcNow;
    result.Owner = wallet.Owner;
    result.CustomerId = wallet.CustomerId;
    return await Save(token);
}
```

- [ ] **Step 5: Controller GET → 404 on null**

`WalletPulse.API/Controllers/CustomerWalletController.cs` — in `Get`, replace the body after fetch:

```csharp
var wallet = await _walletRepository.GetWalletByWalletId(id, token);
if (wallet == null)
{
    return NotFound(new FinalResponse<object>
    {
        StatusCode = 404,
        Message = "Wallet not found."
    });
}
```

- [ ] **Step 6: Run tests — pass**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: all PASS.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "fix: return 404 for missing wallets instead of 500

GetWalletByWalletId returns null when the wallet does not exist;
UpdateCustomerWallet returns false instead of silently succeeding."
```

---

### Task 3: Fix the create-wallet flow (async service + controller conditions)

**Files:**
- Modify: `WalletPulse.Application/Service/CustomerWalletService.cs`
- Modify: `WalletPulse.Application/Interface/ICustomerWalletService.cs`
- Modify: `WalletPulse.Application/Interface/ICustomerRepository.cs`
- Modify: `WalletPulse.Application/Repository/CustomerRepository.cs`
- Modify: `WalletPulse.API/Controllers/CustomerWalletController.cs`
- Test: `WalletPulse.Application.Tests/WalletServiceTests.cs`

**Interfaces:**
- Consumes: `ICustomerRepository.GetCustomerById` (becomes nullable in this task).
- Produces: `Task<bool> HasReachedMaxWallets(Guid customerId, CancellationToken token = default)`; `Task<bool> CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default)`; `Task<Customer?> GetCustomerById(Guid id, CancellationToken token = default)`.

- [ ] **Step 1: Write failing tests**

`WalletPulse.Application.Tests/WalletServiceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;
using WalletPulse.Application.Service;

namespace WalletPulse.Application.Tests;

public class WalletServiceTests
{
    private static (AppDbContext Context, CustomerRepository CustomerRepo) CreateRepos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"svc-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        return (context, new CustomerRepository(context));
    }

    private static async Task<Customer> SeedCustomerAsync(AppDbContext context, int walletCount)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Email = "john@example.com",
            PhoneNumber = "+233200000001",
            Address = "Accra"
        };
        for (var i = 0; i < walletCount; i++)
        {
            context.CustomerWallets.Add(new CustomerWallet
            {
                Id = Guid.NewGuid(),
                WalletName = $"Wallet {i}",
                Type = "momo",
                AccountNumber = $"024400000{i}",
                AccountScheme = "MTN",
                CreatedAt = DateTime.UtcNow,
                Owner = "user-1",
                CustomerId = customer.Id
            });
        }
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task HasReachedMaxWallets_AtLimit_ReturnsTrue()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 5);
        var service = new CustomerWalletService(customerRepo);

        var reached = await service.HasReachedMaxWallets(customer.Id);

        Assert.True(reached);
    }

    [Fact]
    public async Task HasReachedMaxWallets_BelowLimit_ReturnsFalse()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 2);
        var service = new CustomerWalletService(customerRepo);

        var reached = await service.HasReachedMaxWallets(customer.Id);

        Assert.False(reached);
    }

    [Fact]
    public async Task CustomerWalletExists_DuplicateAccountNumber_ReturnsTrue()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 1);
        var service = new CustomerWalletService(customerRepo);

        var exists = await service.CustomerWalletExists(customer.Id, "0244000000");

        Assert.True(exists);
    }

    [Fact]
    public async Task CustomerWalletExists_NewAccountNumber_ReturnsFalse()
    {
        var (context, customerRepo) = CreateRepos();
        var customer = await SeedCustomerAsync(context, walletCount: 1);
        var service = new CustomerWalletService(customerRepo);

        var exists = await service.CustomerWalletExists(customer.Id, "0244999999");

        Assert.False(exists);
    }

    [Fact]
    public async Task GetCustomerById_Missing_ReturnsNull()
    {
        var (context, customerRepo) = CreateRepos();
        await using var _ = context;

        var customer = await customerRepo.GetCustomerById(Guid.NewGuid());

        Assert.Null(customer);
    }
}
```

- [ ] **Step 2: Run tests — fail**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: FAIL/compile errors — service methods are sync (`bool`), `GetCustomerById` non-nullable.

- [ ] **Step 3: Fix interface + service**

`WalletPulse.Application/Interface/ICustomerRepository.cs`:

```csharp
Task<Customer?> GetCustomerById(Guid id, CancellationToken token = default);
```

`WalletPulse.Application/Repository/CustomerRepository.cs` — replace `GetCustomerById` body:

```csharp
public async Task<Customer?> GetCustomerById(Guid id, CancellationToken token = default)
{
    return await _context.Customers
        .Include(wallets => wallets.CustomerWallets)
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken: token);
}
```

`WalletPulse.Application/Interface/ICustomerWalletService.cs` — full file:

```csharp
namespace WalletPulse.Application.Interface;

public interface ICustomerWalletService
{
    Task<bool> HasReachedMaxWallets(Guid customerId, CancellationToken token = default);
    Task<bool> CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default);
}
```

`WalletPulse.Application/Service/CustomerWalletService.cs` — full file:

```csharp
using WalletPulse.Application.Interface;

namespace WalletPulse.Application.Service;

public class CustomerWalletService : ICustomerWalletService
{
    private const int MaxWalletsPerCustomer = 5;

    private readonly ICustomerRepository _customerRepository;

    public CustomerWalletService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<bool> HasReachedMaxWallets(Guid customerId, CancellationToken token = default)
    {
        var customer = await _customerRepository.GetCustomerById(customerId, token);
        return customer?.CustomerWallets.Count >= MaxWalletsPerCustomer;
    }

    public async Task<bool> CustomerWalletExists(Guid customerId, string accountNumber, CancellationToken token = default)
    {
        var customer = await _customerRepository.GetCustomerById(customerId, token);
        return customer?.CustomerWallets
            .Any(wallet => wallet.AccountNumber == accountNumber) ?? false;
    }
}
```

- [ ] **Step 4: Fix the controller create flow**

`WalletPulse.API/Controllers/CustomerWalletController.cs` — replace `CreateCustomerWallet` body:

```csharp
var maxedWalletsReached = await _walletService.HasReachedMaxWallets(request.CustomerId, token);
if (maxedWalletsReached)
{
    return BadRequest(new FinalResponse<object>
    {
        StatusCode = 400,
        Message = "Customer already has 5 wallets on account.",
        Data = null
    });
}

var accountWalletExists = await _walletService.CustomerWalletExists(request.CustomerId, request.AccountNumber, token);
if (accountWalletExists)
{
    return BadRequest(new FinalResponse<object>
    {
        StatusCode = 400,
        Message = "Wallet already exist on customer's account",
        Data = null
    });
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
```

- [ ] **Step 5: Run tests — pass**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: all PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix: correct inverted wallet-creation conditions and async service

Wallets can now actually be created: the service checks are async and
await the repository properly, and the controller creates the wallet
only when no duplicate exists and the customer is under the 5-wallet
limit."
```

---

### Task 4: Balance & transactions (model, migration, repository, endpoints)

**Files:**
- Create: `WalletPulse.Application/Model/Transaction.cs`
- Modify: `WalletPulse.Application/Model/CustomerWallet.cs` (add `Balance`)
- Modify: `WalletPulse.Application/Database/AppDbContext.cs`
- Create: EF migration `Add_Wallet_Balance_And_Transactions` (generated)
- Modify: `WalletPulse.Application/Interface/ICustomerWalletRepository.cs`
- Modify: `WalletPulse.Application/Repository/CustomerWalletRepository.cs`
- Create: `WalletPulse.Contracts/Request/WalletMovementRequest.cs`
- Create: `WalletPulse.Contracts/Response/TransactionResponse.cs`
- Create: `WalletPulse.API/ContractMappings/TransactionContractMapping.cs`
- Modify: `WalletPulse.API/ApiEndpoints.cs`
- Modify: `WalletPulse.API/Controllers/CustomerWalletController.cs`
- Modify: `WalletPulse.API/ContractMappings/WalletContractMapping.cs` (Balance in response)
- Modify: `WalletPulse.Contracts/Response/CustomerWalletResponse.cs` (Balance)
- Test: `WalletPulse.Application.Tests/WalletMovementTests.cs`

**Interfaces:**
- Consumes: fixed service from Task 3.
- Produces:
  - `record WalletQueryResult(IReadOnlyList<CustomerWallet> Items, int TotalCount);`
  - `Task<CustomerWallet?> ApplyMovementAsync(Guid walletId, TransactionType type, decimal amount, string? reference, CancellationToken token = default);` — returns null when wallet missing; returns the updated wallet on success; throws `InvalidOperationException("Insufficient funds.")` when a withdrawal exceeds the balance.
  - `enum TransactionType { Deposit = 1, Withdrawal = 2 }` in `WalletPulse.Application.Model`.
  - Endpoints: `POST /api/wallet/{id:guid}/deposit`, `POST /api/wallet/{id:guid}/withdraw`, `GET /api/wallet/{id:guid}/transactions`.

- [ ] **Step 1: Write failing tests**

`WalletPulse.Application.Tests/WalletMovementTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletMovementTests
{
    private static (AppDbContext Context, CustomerWalletRepository Repo) Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"mov-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        return (context, new CustomerWalletRepository(context));
    }

    private static async Task<CustomerWallet> SeedWalletAsync(AppDbContext context, decimal balance = 100m)
    {
        var wallet = new CustomerWallet
        {
            Id = Guid.NewGuid(),
            WalletName = "Personal",
            Type = "momo",
            AccountNumber = "0244000001",
            AccountScheme = "MTN",
            CreatedAt = DateTime.UtcNow,
            Owner = "user-1",
            CustomerId = Guid.NewGuid(),
            Balance = balance
        };
        context.CustomerWallets.Add(wallet);
        await context.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task Deposit_IncreasesBalance_AndRecordsTransaction()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        var updated = await repo.ApplyMovementAsync(wallet.Id, TransactionType.Deposit, 50m, "ref-1");

        Assert.NotNull(updated);
        Assert.Equal(150m, updated!.Balance);
        Assert.Equal(1, await context.Transactions.CountAsync());
        var transaction = await context.Transactions.FirstAsync();
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal("ref-1", transaction.Reference);
    }

    [Fact]
    public async Task Withdraw_WithinBalance_DecreasesBalance()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        var updated = await repo.ApplyMovementAsync(wallet.Id, TransactionType.Withdrawal, 30m, null);

        Assert.NotNull(updated);
        Assert.Equal(70m, updated!.Balance);
        Assert.Equal(1, await context.Transactions.CountAsync());
    }

    [Fact]
    public async Task Withdraw_ExceedingBalance_ThrowsAndWritesNothing()
    {
        var (context, repo) = Create();
        var wallet = await SeedWalletAsync(context, balance: 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.ApplyMovementAsync(wallet.Id, TransactionType.Withdrawal, 150m, null));

        Assert.Equal(100m, (await context.CustomerWallets.FirstAsync()).Balance);
        Assert.Equal(0, await context.Transactions.CountAsync());
    }

    [Fact]
    public async Task ApplyMovement_MissingWallet_ReturnsNull()
    {
        var (context, repo) = Create();
        await using var _ = context;

        var result = await repo.ApplyMovementAsync(Guid.NewGuid(), TransactionType.Deposit, 50m, null);

        Assert.Null(result);
    }
}
```

- [ ] **Step 2: Run tests — fail**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: compile FAIL — no `Transaction`, no `Balance`, no `ApplyMovementAsync`.

- [ ] **Step 3: Model + context changes**

`WalletPulse.Application/Model/Transaction.cs` (new):

```csharp
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
```

`WalletPulse.Application/Model/CustomerWallet.cs` — add property:

```csharp
public decimal Balance { get; set; }
```

`WalletPulse.Application/Database/AppDbContext.cs` — inside `OnModelCreating`, add:

```csharp
modelBuilder.Entity<Transaction>()
    .HasOne<CustomerWallet>()
    .WithMany()
    .HasForeignKey(t => t.WalletId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Transaction>()
    .Property(t => t.Type)
    .HasConversion<string>()
    .HasMaxLength(20);

modelBuilder.Entity<Transaction>()
    .HasIndex(t => new { t.WalletId, t.CreatedAt });
```

Add to `AppDbContext`:

```csharp
public DbSet<Transaction> Transactions { get; set; }
```

- [ ] **Step 4: Repository ApplyMovementAsync**

Add to `ICustomerWalletRepository`:

```csharp
Task<CustomerWallet?> ApplyMovementAsync(Guid walletId, TransactionType type, decimal amount, string? reference, CancellationToken token = default);
```
(add `using WalletPulse.Application.Model;` — already present.)

Add to `CustomerWalletRepository`:

```csharp
public async Task<CustomerWallet?> ApplyMovementAsync(Guid walletId, TransactionType type, decimal amount, string? reference, CancellationToken token = default)
{
    var wallet = await _context.CustomerWallets
        .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken: token);
    if (wallet == null)
    {
        return null;
    }
    if (type == TransactionType.Withdrawal && amount > wallet.Balance)
    {
        throw new InvalidOperationException("Insufficient funds.");
    }
    wallet.Balance += type == TransactionType.Deposit ? amount : -amount;
    _context.Transactions.Add(new Transaction
    {
        Id = Guid.NewGuid(),
        WalletId = walletId,
        Type = type,
        Amount = amount,
        Reference = reference,
        CreatedAt = DateTime.UtcNow
    });
    await _context.SaveChangesAsync(token);
    return wallet;
}
```

- [ ] **Step 5: Run tests — pass**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: all PASS.

- [ ] **Step 6: Contracts + mappings + endpoints + controller**

`WalletPulse.Contracts/Request/WalletMovementRequest.cs` (new):

```csharp
namespace WalletPulse.Contracts.Request;

public class WalletMovementRequest
{
    public required decimal Amount { get; set; }
    public string? Reference { get; set; }
}
```

`WalletPulse.Contracts/Response/TransactionResponse.cs` (new):

```csharp
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
```

`WalletPulse.Contracts/Response/CustomerWalletResponse.cs` — add:

```csharp
public decimal Balance { get; set; }
```

`WalletPulse.API/ContractMappings/WalletContractMapping.cs` — in `MapsToResponse(this CustomerWallet wallet)` add `Balance = wallet.Balance,`.

`WalletPulse.API/ContractMappings/TransactionContractMapping.cs` (new):

```csharp
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
```

`WalletPulse.API/ApiEndpoints.cs` — inside `CustomerWallet` class add:

```csharp
public const string Deposit = $"{Base}/{{id:guid}}/deposit";
public const string Withdraw = $"{Base}/{{id:guid}}/withdraw";
public const string Transactions = $"{Base}/{{id:guid}}/transactions";
```

`WalletPulse.API/Controllers/CustomerWalletController.cs` — add three actions:

```csharp
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
    try
    {
        var wallet = await _walletRepository.ApplyMovementAsync(id, TransactionType.Withdrawal, request.Amount, request.Reference, token);
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
    catch (InvalidOperationException) when (ex.Message == "Insufficient funds.")
    {
        return BadRequest(new FinalResponse<object>
        {
            StatusCode = 400,
            Message = "Insufficient funds.",
            Data = null
        });
    }
}
```
NOTE: `catch` must be `catch (InvalidOperationException ex) when (ex.Message == "Insufficient funds.")`. Add `using WalletPulse.Application.Model;` and `using WalletPulse.Contracts.Request;` (already there via existing usings; verify).

```csharp
//GET Transaction History
[HttpGet(ApiEndpoints.CustomerWallet.Transactions)]
public async Task<IActionResult> GetTransactions(Guid id, CancellationToken token)
{
    var walletExists = await _walletRepository.WalletExists(id, token);
    if (!walletExists)
    {
        return NotFound(new FinalResponse<object> { StatusCode = 404, Message = "Wallet not found." });
    }
    var transactions = await _context-load-latest: see step below
}
```

For history, add to `ICustomerWalletRepository`:

```csharp
Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(Guid walletId, CancellationToken token = default);
```

Implementation in `CustomerWalletRepository`:

```csharp
public async Task<IEnumerable<Transaction>> GetWalletTransactionsAsync(Guid walletId, CancellationToken token = default)
{
    return await _context.Transactions
        .Where(t => t.WalletId == walletId)
        .OrderByDescending(t => t.CreatedAt)
        .ToListAsync(cancellationToken: token);
}
```

And the controller action:

```csharp
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
```

- [ ] **Step 7: Generate the EF migration**

```bash
dotnet ef migrations add Add_Wallet_Balance_And_Transactions --project WalletPulse.Application
```

Verify the generated migration adds `Balance` column (nullable-safe default 0) and the `Transactions` table; fix the snapshot if the column is created as nullable by adding `.HasDefaultValue(0m)` context config:

In `AppDbContext.OnModelCreating` (add before generating):

```csharp
modelBuilder.Entity<CustomerWallet>()
    .Property(w => w.Balance)
    .HasPrecision(18, 2);
```

- [ ] **Step 8: Build + test**

```bash
dotnet build && dotnet test WalletPulse.Application.Tests
```
Expected: 0 errors; all tests PASS.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat: add wallet balances, deposits, withdrawals, and transaction history"
```

---

### Task 5: Search, filter & pagination on GET /api/wallet

**Files:**
- Modify: `WalletPulse.Application/Interface/ICustomerWalletRepository.cs`
- Modify: `WalletPulse.Application/Repository/CustomerWalletRepository.cs`
- Create: `WalletPulse.Contracts/Response/PagedResponse.cs`
- Modify: `WalletPulse.API/Controllers/CustomerWalletController.cs`
- Test: `WalletPulse.Application.Tests/WalletQueryTests.cs`

**Interfaces:**
- Consumes: `record WalletQueryResult(IReadOnlyList<CustomerWallet> Items, int TotalCount)` (created in this task — place in `WalletPulse.Application.Repository`).
- Produces: `Task<WalletQueryResult> GetCustomerWalletsPagedAsync(WalletFilter filter, CancellationToken token = default)`; `record WalletFilter(string? Name, string? Type, string? AccountScheme, int Page, int PageSize)` in `WalletPulse.Application.Interface`.

- [ ] **Step 1: Write failing tests**

`WalletPulse.Application.Tests/WalletQueryTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using WalletPulse.Application.Database;
using WalletPulse.Application.Interface;
using WalletPulse.Application.Model;
using WalletPulse.Application.Repository;

namespace WalletPulse.Application.Tests;

public class WalletQueryTests
{
    private static async Task<AppDbContext> SeedAsync(int count)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"q-{Guid.NewGuid()}")
            .Options;
        var context = new AppDbContext(options);
        for (var i = 0; i < count; i++)
        {
            context.CustomerWallets.Add(new CustomerWallet
            {
                Id = Guid.NewGuid(),
                WalletName = i % 2 == 0 ? "Personal" : "Savings",
                Type = i % 3 == 0 ? "card" : "momo",
                AccountNumber = $"02440000{i:00}",
                AccountScheme = i % 2 == 0 ? "MTN" : "VISA",
                CreatedAt = DateTime.UtcNow.AddMinutes(i),
                Owner = "user-1",
                CustomerId = Guid.NewGuid()
            });
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task GetPaged_Defaults_ReturnsFirstPageOf20()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 1, 20));

        Assert.Equal(20, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRemaining5()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, null, null, 2, 20));

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetPaged_FilterByType_ReturnsOnlyMatching()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter(null, "card", null, 1, 20));

        Assert.Equal(9, result.TotalCount); // indexes 0,3,6,9,12,15,18,21,24
        Assert.All(result.Items, w => Assert.Equal("card", w.Type));
    }

    [Fact]
    public async Task GetPaged_NameContains_IsCaseInsensitive()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(np: null!);
        // fixed below
    }
```

NOTE: the last test above has an intentional typo — replace its body with:

```csharp
    [Fact]
    public async Task GetPaged_NameContains_IsCaseInsensitive()
    {
        var context = await SeedAsync(25);
        var repo = new CustomerWalletRepository(context);

        var result = await repo.GetCustomerWalletsPagedAsync(new WalletFilter("personal", null, null, 1, 20));

        Assert.Equal(13, result.TotalCount); // "Personal" wallets at even indexes
        Assert.All(result.Items, w => Assert.Contains("Personal", w.WalletName));
    }
}
```

- [ ] **Step 2: Run tests — fail**

```bash
dottest test WalletPulse.Application.Tests
```
Expected: compile FAIL — `WalletFilter`, `WalletQueryResult`, `GetCustomerWalletsPagedAsync` do not exist.

- [ ] **Step 1 note:** also fix the typo'd command above: `dotnet test WalletPulse.Application.Tests`.

- [ ] **Step 3: Implement filter record + repository method**

`WalletPulse.Application/Interface/ICustomerWalletRepository.cs` — add at namespace level (outside interface):

```csharp
public sealed record WalletFilter(string? Name, string? Type, string? AccountScheme, int Page, int PageSize);
```

`WalletPulse.Application/Repository/CustomerWalletRepository.cs` — add:

```csharp
public sealed record WalletQueryResult(IReadOnlyList<CustomerWallet> Items, int TotalCount);

public async Task<WalletQueryResult> GetCustomerWalletsPagedAsync(WalletFilter filter, CancellationToken token = default)
{
    var page = Math.Max(1, filter.Page);
    var pageSize = Math.Clamp(filter.PageSize, 1, 100);

    var query = _context.CustomerWallets.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(filter.Name))
    {
        query = query.Where(w => w.WalletName.ToLower().Contains(filter.Name.ToLower()));
    }
    if (!string.IsNullOrWhiteSpace(filter.Type))
    {
        query = query.Where(w => w.Type == filter.Type);
    }
    if (!string.IsNullOrWhiteSpace(filter.AccountScheme))
    {
        query = query.Where(w => w.AccountScheme == filter.AccountScheme);
    }

    var totalCount = await query.CountAsync(cancellationToken: token);
    var items = await query
        .OrderByDescending(w => w.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken: token);

    return new WalletQueryResult(items, totalCount);
}
```

Add to the interface:

```csharp
Task<WalletQueryResult> GetCustomerWalletsPagedAsync(WalletFilter filter, CancellationToken token = default);
```

- [ ] **Step 4: PagedResponse contract + controller**

`WalletPulse.Contracts/Response/PagedResponse.cs` (new):

```csharp
namespace WalletPulse.Contracts.Response;

public class PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    [UsedImplicitly] public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
}
```
Remove the `[UsedImplicitly]` attribute line — keep it plain:

```csharp
namespace WalletPulse.Contracts.Response;

public class PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    required AsPlainBelow;
}
```
CORRECTION — write the file exactly as:

```csharp
namespace WalletPulse.Contracts.Response;

public class PagedResponse<T>
{
    public required IEnumerable<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
}
```

- [ ] **Step 5: Controller GetAll — paged**

Replace `GetCustomerWallets` in `CustomerWalletController.cs`:

```csharp
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
```

- [ ] **Step 6: Run tests — pass**

```bash
dotnet test WalletPulse.Application.Tests
```
Expected: all PASS.

- [ Step 7: Commit]

```bash
git add -A
git commit -m "feat: add search, filter, and pagination to wallet list endpoint"
```

---

### Task 6: Robustness — validation, global exception handler, health, drop Newtonsoft

**Files:**
- Create: `WalletPulse.Application/Validators/CreateCustomerRequestValidator.cs` (and the other four validators)
- Create: `WalletPulse.API/Filters/ValidationFilter.cs` — not used if auto-validation via manual filter approach (see below)
- Create: `WalletPulse.API/GlobalExceptionHandler.cs`
- Modify: `WalletPulse.API/Program.cs`
- Modify: `WalletPulse.Application/ApplicationCollectionExtensions.cs`
- Modify: `WalletPulse.API/WalletPulse.API.csproj`, `WalletPulse.Application/WalletPulse.Application.csproj` (drop Newtonsoft package)
- Test: existing tests keep passing; add `WalletPulse.Application.Tests/ValidatorTests.cs`

**Interfaces:**
- Produces: `AddApplication()` registers validators via `AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>()`; `ValidationFilter` registered globally.
- Consumes: FluentValidation 12 (`FluentValidation.DependencyInjectionExtensions`).

- [ ] **Step 1: Validators**

`WalletPulse.Application/Validators/CreateCustomerRequestValidator.cs`:

```csharp
using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BirthDate).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(255);
    }
}
```

`WalletPulse.Application/Validators/UpdateCustomerRequestValidator.cs`:

```csharp
using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Validators;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        SubstituteBody(); // placeholder removed below
    }
}
```
CORRECTION — full file:

```csharp
using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Validators;

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(255);
    }
}
```

`WalletPulse.Application/Validators/CreateCustomerWalletRequestValidator.cs`:

```csharp
using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Validators;

public class CreateCustomerWalletRequestValidator : AbstractValidator<CreateCustomerWalletRequest>
{
    private static readonly string[] AllowedTypes = ["momo", "card", "bank"];
    private static readonly string[] AllowedSchemes = ["MTN", "Telecel", "AirtelTigo", "VISA", "Mastercard"];

    public CreateCustomerWalletRequestValidator()
    {
        RuleFor(x => x.WalletName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(t => AllowedTypes.Contains(t))
            .WithMessage("Type must be one of: momo, card, bank.");
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AccountScheme).NotEmpty().Must(s => AllowedSchemes.Contains(s))
            .WithMessage("AccountScheme must be one of: MTN, Telecel, AirtelTigo, VISA, Mastercard.");
        RuleFor(x => x.Owner).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
```

`WalletPulse.Application/Validators/UpdateCustomerWalletRequestValidator.cs` — same rules as create (copy rules, change class name and type parameter; skip the `CustomerId` rule only if the update DTO also has it — it does, keep all rules).

`WalletPulse.Application/Validators/WalletMovementRequestValidator.cs`:

```csharp
using FluentValidation;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Validation;

public class WalletMovementRequestValidator : AbstractValidator<WalletMovementRequest>
{
    public WalletMovementRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0)
            .Must(a => decimal.Round(a, 2) == a).WithMessage("Amount must have at most 2 decimal places.");
        RuleFor(x => x.Reference).MaximumLength(200);
    }
}
```

NOTE namespace inconsistency — put ALL validators in `WalletPulse.Application.Validators` (use that namespace for WalletMovementRequestValidator too).

- [ ] **Step 2: Register validators + wire validation filter**

`WalletPulse.Application/ApplicationCollectionExtensions.cs` — add inside `AddApplication`:

```csharp
service.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();
```

`WalletPulse.API/Program.cs` — after `AddApplication()`:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();
builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

Replace the existing `AddControllers().AddNewtonsoftJson(...)` line entirely — the two services lines collapse to:

```csharp
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
```

- [ ] Step 3: ValidationFilter implementation

`WalletPulse.API/Filters/ValidationFilter.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WalletPulse.Filters;

public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IValidatorFactory _validatorFactory;

    public ValidationFilter(IValidatorFactory validatorFactory)
    {
        _validatorFactory = validatorFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (key, value) in context.ActionArguments)
        {
            if (value is null) { continue; }
            var validator = _validatorFactory.GetValidator(value.GetType());
            if (validator is null) { continue; }
            var result = await validator.ValidateAsync(new ValidationContext<object>(value), context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(
                    result.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())))
                {
                    ContentTypes = { "application/problem+json" }
                };
                return;
            }
        }
        await next();
    }
}
```

NOTE: `ValidationContext<object>` — FluentValidation 12 generic `ValidationContext<T>` needs `IValidationContext` — use `new ValidationContext<object>(value)` then `validator.ValidateAsync(context)` — the base `IValidator.ValidateAsync(IValidationContext, CancellationToken)` overload exists. If signature friction arises, cast: `await validator.ValidateAsync(new ValidationContext(value), ct)`.

- [ ] **Step 4: Global exception handler**

`WalletPulse.API/GlobalExceptionHandler.cs`:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WalletPulse;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    try
    {
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        }, cancellationToken);
        return true;
    }
    }
    catch { }
}
```
CORRECTION — no try/catch wrapper; final file:

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WalletPulse;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 500,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        }, cancellationToken);
        return true;
    }
}
```

`Program.cs` additions:

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

and in pipeline after `var app = builder.Build();`:

```csharp
app.UseExceptionHandler();
```

- [ ] **Step 5: Health check + drop Newtonsoft**

`Program.cs`:

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

`WalletPulse.API/WalletPulse.API.csproj` — remove `Microsoft.AspNetCore.Mvc.NewtonsoftJson` PackageReference.
`WalletPulse.Application/WalletPulse.Application.csproj` — remove `Microsoft.AspNetCore.Mvc.NewtonsoftJson` PackageReference.

- [ ] **Step 6: Validator tests**

`WalletPulse.Application.Tests/ValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using WalletPulse.Application.Validators;
using WalletPulse.Contracts.Request;

namespace WalletPulse.Application.Tests;

public class ValidatorTests
{
    [Fact]
    public void CreateWallet_InvalidType_HasError()
    {
        var validator = new CreateCustomerWalletRequestValidator();
        var model = new CreateCustomerWalletRequest
        {
            WalletName = "Personal", Type = "crypto", AccountNumber = "0244",
            AccountScheme = "MTN", CreatedAt = DateTime.UtcNow, Owner = "u1",
            CustomerId = Guid.NewGuid()
        };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Movement_NonPositiveAmount_HasError()
    {
        var validator = new WalletMovementRequestValidator();
        var model = new WalletMovementRequest { Amount = 0m };
        var result = validator.TestHelperValidatePlaceholder();
    }
}
```
CORRECTION — second test fixed:

```csharp
    [Fact]
    public void Movement_NonPositiveAmount_HasError()
    {
        var validator = new WalletMovementRequestValidator();
        var model = new WalletMovementRequest { Amount = 0m };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }
}
```

- [ ] **Step 7: Build + all tests**

```bash
dotnet build && dotnet test
```
Expected: 0 errors, all tests PASS.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat: add request validation, global exception handler, and health check

Drop Newtonsoft.Json in favor of System.Text.Json only; validation
errors return RFC 7807 ProblemDetails with per-field messages."
```

---

### Task 7: Full end-to-end verification

**Files:**
- None new (verification only).

- [ ] **Step 1: Clean build + full test run**

```bash
dotnet build && dotnet test
```
Expected: 0 errors/warnings; all tests PASS.

- [ ] **Step 2: Postgres up + migrate + run + curl the whole flow**

```bash
brew services start postgresql@17
/opt/homebrew/opt/postgresql@17/bin/psql -U postgres -h localhost -c 'CREATE DATABASE "Hubtel.Wallets"' # if missing
cd WalletPulse.API && dotnet ef database update --project ../WalletPulse.Application
(dotnet run --no-build > /tmp/api.log 2>&1 &)
sleep 8
# 1. health
curl -s http://localhost:5171/health
# 2. create customer
curl -s -X POST http://localhost:5171/api/customers -H 'Content-Type: application/json' \
  -d '{"firstName":"John","lastName":"Doe","birthDate":"1990-01-01T00:00:00","email":"john@example.com","phoneNumber":"+233200000001","address":"Accra"}'
# 3. create wallet (previously always failed!)
curl -s -X POST http://localhost:5171/api/wallet -H 'Content-Type: application/json' \
  -d '{"walletName":"Personal","type":"momo","accountNumber":"0244000001","accountScheme":"MTN","owner":"user-1","createdAt":"2026-09-01T00:00:00","customerId":"<CUSTOMER_ID>"}'
# 4. deposit / withdraw / history
curl -s -X POST http://localhost:5171/api/wallet/<WALLET_ID>/deposit -H 'Content-Type: application/json' -d '{"amount": 150.50, "reference": "topup"}'
curl -s -X POST http://localhost:5171/api/wallet/<WALLET_ID>/withdraw -H 'Content-Type: application/json' -d '{"amount": 50, "reference": "pay bill"}'
curl -s http://localhost:5171/api/wallet/<WALLET_ID>/transactions
# 5. list w/ filters + paging
curl -s 'http://localhost:5171/api/wallet?name=personal&type=momo&page=1&pageSize=10'
# 6. 404s + validation 400s
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5171/api/wallet/00000000-0000-0000-0000-000000000000
curl -s -X POST http://localhost:5171/api/wallet/<WALLET_ID>/withdraw -H 'Content-Type: application/json' -d '{"amount": 999999}'   # insufficient funds → 400
curl -s -X POST http://localhost:5171/api/wallet -H 'Content-Type: application/json' -d '{"walletName":"","type":"crypto","accountNumber":"x","accountScheme":"MTN","owner":"u","createdAt":"2026-09-01T00:00:00","customerId":"00000000-0000-0000-0000-000000000000"}'  # validation → 400 ProblemDetails
pkill -f WalletPulse.API
```

Expected: health 200; customer 201; wallet 201 (the bug fix working); deposit 200 balance 150.50; withdraw 200 balance 100.50; history lists 2 newest-first; filtered list returns the wallet; 404 for missing; 400 insufficient; 400 ProblemDetails validation shape.

- [ ] **Step 3: Stop Postgres, restore environment**

```bash
pkill -f WalletPulse.API; brew services stop postgresql@17
```

- [ ] **Step 4: Commit any fixes (none expected)**

Working tree should be clean after all phase commits.

---

## Self-Review

**Spec coverage check:** Phase 1 rebrand → Task 1. Phase 2 fixes → Tasks 2–3. Phase 3 balance/transactions → Task 4. Phase 4 search/filter/pagination → Task 5. Phase 5 robustness → Task 6. Phase 6 tests/verification → Tasks 2–6 (inline tests) + Task 7 (smoke). No gaps.

**Placeholder scan:** fixed inline during writing (three marked CORRECTION blocks where first-draft snippets had errors — the corrections are the authoritative versions).

**Type consistency:** `WalletFilter(Name, Type, AccountScheme, Page, PageSize)` and `WalletQueryResult(Items, TotalCount)` used consistently in Task 5 tests and implementation; `ApplyMovementAsync(walletId, type, amount, reference, token)` consistent between Task 4 interface, repo, controller, and tests; validators all in `WalletPulse.Application.Validators`.
