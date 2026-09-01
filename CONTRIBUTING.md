# Contributing to WalletPulse

Thanks for considering a contribution.

## Before you start

1. Fork the repo (already renamed to `Perigrino/WalletPulse`).
2. Create a branch: `git checkout -b feat/your-change`.
3. Open an issue describing the change if it's not a trivial fix.

## Development setup

- .NET 10 SDK (`dotnet --version` should show `10.0.400` or later)
- PostgreSQL (local development DB: `Hubtel.Wallets`; connection string in `WalletPulse.API/appsettings.json`)
- `dotnet restore` → `dotnet build` → `dotnet ef database update --project WalletPulse.Application`

## Making changes

- Follow the existing patterns (repository → service → controller → contract mapping).
- Add or update tests in `WalletPulse.Application.Tests` for any business-logic change.
- Keep commits focused (`build:`, `feat:`, `fix:`, `docs:` — Conventional Commits).
- Make sure `dotnet build` is clean (0 errors, 0 warnings) and `dotnet test` passes.

## Submitting

- Push your branch: `git push origin feat/your-change`.
- Open a PR against `UnderConstruction` (or `main` when ready) describing what changed and why.
- The PR must pass the build and test checks.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md). Be respectful and constructive.
