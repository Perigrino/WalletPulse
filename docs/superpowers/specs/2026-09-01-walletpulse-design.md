# WalletPulse Design Spec

Date: 2026-09-01
Status: Approved (design presented in chat; user approved all sections)
Branch: `UnderConstruction`

## Context

The codebase (formerly Hubtel.Wallets) is a 3-project ASP.NET Core API —
`*.API` (controllers), `*.Application` (domain, EF, repositories, services),
`*.Contracts` (DTOs) — recently upgraded from .NET 7 to .NET 10
(commit a79635d). The user wants: wallet-management improvements
(create-wallet bug fix, balances & transactions, search/filter/pagination,
robustness) plus a full rebrand to **WalletPulse** including the GitHub repo.

Known defects (verified during the .NET 10 smoke test):

1. `CustomerWalletService.CustomerWalletExists` returns `bool` while
   inspecting a `Task<Customer>` for null — it can never detect a
   duplicate, and always reports `true`.
2. `CustomerWalletController.CreateCustomerWallet` has an inverted
   condition: it creates the wallet only when a duplicate *exists*.
3. `CustomerWalletRepository.GetWalletByWalletId` throws
   `InvalidOperationException` on missing rows → GET by id returns 500
   instead of 404.
4. `CustomerWalletRepository.UpdateCustomerWallet` reports success even
   when the wallet does not exist (missing early `return false`).

## Goals

1. Rebrand solution, projects, namespaces, and GitHub repo to WalletPulse.
2. Fix the wallet-creation flow so wallets can actually be created.
3. Add wallet balances with deposit/withdraw endpoints and transaction
   history.
4. Add search, filter, and pagination to the wallet list endpoint.
5. Harden the API: request validation, global error handling, health
   check, single JSON serializer.

## Non-Goals

- Wallet-to-wallet transfers (may be a follow-up).
- Renaming the PostgreSQL database (`Hubtel.Wallets`) — the connection
   string stays as-is; no data migration.
- Fixing the inverted dependency direction (Contracts → Application);
   noted, left alone to avoid churn.
- Authentication/authorization.
- Frontend/UI work.

## Phase 1 — Full rebrand to WalletPulse

**GitHub repo:** `gh repo rename WalletPulse` (gh updates the `origin`
remote URL automatically; no history rewrite).

**Local (via `git mv` to preserve history; the root folder
`/Users/perigrino/Projects/Hubtel.Wallets` is renamed manually by the
user or left as-is — it does not affect the build):**

**Local (via `git mv` to preserve history):**

- `Hubtel.Wallets.sln` → `WalletPulse.sln`
- `Hubtel.Wallets.API/` → `WalletPulse.API/`
- `Hubtel.Wallets.Application/` → `WalletPulse.Application/`
- `Hubtel.Wallets.Contracts/` → `WalletPulse.Contracts/`
- csproj files renamed to match folders.
- Namespaces: `Hubtel.Wallets` and `Hubtel.Wallets.Application`,
  `Hubtel.Wallets.Contracts...`, `Hubtel.Wallets.ContractMappings` →
  `WalletPulse`, `WalletPulse.Application`, `WalletPulse.Contracts...`,
  `WalletPulse.ContractMappings`.
- csproj `RootNamespace` → `WalletPulse`.
- `AssemblyName` stays defaulting to the csproj file name.
- `AppDbContextFactory` hard-codes the path
  `../Hubtel.Wallets.API/appsettings.json` → update to
  `../WalletPulse.API/appsettings.json`.
- Solution file project paths updated.

**Out of scope:** database name, .vscode, .idea.

## Phase 2 — Bug fixes

- `ICustomerWalletService.CustomerWalletExists` → `async Task<bool>`
   awaiting the repository properly; check the customer's wallets for a
   matching account number. `HasReachedMaxWallets` → `async Task<bool>`
   (currently inspects `.Result` — deadlock-prone and sync-over-async).
- `CustomerWalletController.CreateCustomerWallet`:
  - customer reached max (5) wallets → 400.
  - duplicate account number on that customer → 400.
  - otherwise → create, 201 with the wallet response.
- `GetWalletByWalletId` → return `null` on missing; controller GET → 404
   ProblemDetails when null.
- `UpdateCustomerWallet` → `return false` when wallet not found → 404.
- `DeleteCustomerWallet` already returns false correctly.

## Phase 3 — Balance & transactions

**Model changes (one additive EF migration):**

- `CustomerWallet.Balance` : `decimal(18,2)`, default `0`, non-null.
- New `Transaction` entity:
  - `Id : Guid` (PK)
  - `WalletId : Guid` (FK → CustomerWallets, cascade delete)
  - `Type : enum TransactionType { Deposit = 1, Withdrawal = 2 }`,
    stored as string in DB for readability (`HasConversion<string>`).
  - `Amount : decimal(18,2)` (always positive)
  - `Reference : string?` (optional note)
  - `CreatedAt : DateTime` (UTC)
- `DbSet<Transaction> Transactions`.
- Index on `(WalletId, CreatedAt)` for history queries.

**Rules:**

- Deposit/withdraw `Amount > 0`; two-decimal scale enforced by validation.
- Withdrawal must satisfy `Amount <= Balance`; else 400 ProblemDetails
  "Insufficient funds."
- Balance update + transaction insert are atomic: EF Core wraps a single
  `SaveChangesAsync` in one DB transaction, so mutating the tracked
  wallet's `Balance` and adding the `Transaction` row before one
  `SaveChangesAsync(token)` call is sufficient — no explicit
  `BeginTransactionAsync` needed.

**Endpoints:**

- `POST /api/wallet/{id:guid}/deposit` body `{ amount, reference? }`
  → 200 with updated wallet response.
- `POST /api/wallet/{id:guid}/withdraw` body `{ amount, reference? }`
  → 200 with updated wallet response; 400 insufficient funds; 404 unknown
  wallet.
- `GET /api/wallet/{id:guid}/transactions` → 200 newest-first
  `TransactionResponse` list; 404 unknown wallet.

Contracts: `DepositRequest`, `WithdrawRequest` (or a shared
`WalletMovementRequest`), `TransactionResponse` (id, walletId, type,
amount, reference, createdAt).

Repository/service split: `CustomerWalletRepository.DepositAsync`,
`WithdrawAsync` or a single `ApplyMovementAsync` — implementation detail
left to plan; interface updated accordingly. Business rules enforced in
service, not controller.

## Phase 4 — Search, filter & pagination

`GET /api/wallet` query params:

- `name` (contains, case-insensitive) → filter on WalletName
- `type` (exact) → filter on Type
- `accountScheme` (exact) → filter on AccountScheme
- `page` (1-based, default 1), `pageSize` (default 20, max 100)

Response: `{ items, page, pageSize, totalCount, totalPages }` — new
contract `PagedResponse<T>`. Order by `CreatedAt` newest first.
Implemented via EF `Skip/Take` + `CountAsync` in one repository call
returning a `(IReadOnlyList<CustomerWallet>, int totalCount)` tuple or
small record.

## Phase 5 — Robustness & validation

- FluentValidation wired via `AddValidatorsFromAssemblyContaining<>` in
  `AddApplication()`; validators for `CreateCustomerRequest`,
  `UpdateCustomerRequest`, `CreateCustomerWalletRequest`,
  `UpdateCustomerWalletRequest`, `WalletMovementRequest`:
  - required strings non-empty, length caps (e.g. WalletName ≤ 100)
  - email format on customer requests
  - `Type` ∈ { "momo", "card", "bank" } (existing app semantics)
  - `AccountScheme` ∈ { "MTN", "Telecel", "AirtelTigo", "VISA", "Mastercard" }
  - movement amount > 0, ≤ 2 decimals; reference ≤ 200
- Auto-validation: `FluentValidation.AspNetCore` auto-validation MVC
  integration is deprecated; use a small `IValidatorInterceptor`-free
  approach — an `ValidationFilter`/`IActionFilter` or the
  `AddValidatorsFromAssemblyContaining` + manual validation in a filter
  producing 400 ProblemDetails with field errors dictionary.
- Global exception handler: `IExceptionHandler` (net10) registered with
  `AddExceptionHandler` + `AddProblemDetails`; returns 500 ProblemDetails
  without stack traces; logs via `ILogger`.
- Drop `Microsoft.AspNetCore.Mvc.NewtonsoftJson` package and
  `AddNewtonsoftJson()` call; keep System.Text.Json with
  `ReferenceHandler.IgnoreCycles` (already configured).
- Health check: `AddHealthChecks()` + `MapHealthChecks("/health")`.
  A Postgres-specific check would need the
  `AspNetCore.HealthChecks.Npgsql` package — skipped for YAGNI; `/health`
  returns 200 when the app is up.
- Response DTOs: `FinalResponse<T>` wrapper stays (existing API shape),
  with ProblemDetails only for errors from middleware/filters.

## Phase 6 — Tests & verification

- New `WalletPulse.Application.Tests` (xUnit + EF Core InMemory provider
  `Microsoft.EntityFrameworkCore.InMemory` 10.0.4):
  - wallet create flow: max-5 rule, duplicate account number rule
  - deposit/withdraw: positive amount, insufficient funds, atomicity of
    balance+transaction (row counts)
  - pagination math: page bounds, totalCount, filtering
- Build: `dotnet build` 0 warnings 0 errors.
- Smoke test (same as .NET 10 upgrade): Postgres up, `dotnet ef database
  update`, run API, curl the full flow — create customer → create wallet
  → deposit → withdraw → history → filter/paginate → 404s → validation
  400s.
- All previously verified endpoints still work.

## Risks & Mitigations

- Namespace rename touches every .cs file — mitigated by IDE-free
  scripted `grep/sed` + full build + tests as the safety net.
- Migration on existing DBs adds nullable-default columns only → safe,
  additive.
- FluentValidation 12 API changes (from 11) — validators written against
  v12 docs patterns.

## Rollback

- Each phase is a separate commit; revert phase-by-phase if needed.
- GitHub repo rename is reversible via Settings; `origin` URL updated by
  `gh`.
- EF migration: additive only; to roll back, remove migration and delete
  the `Transactions` table / `Balance` column manually or via a revert
  migration.
