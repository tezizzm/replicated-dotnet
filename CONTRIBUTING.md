# Contributing

Thank you for your interest in contributing. This is a community-maintained project — all
contributions are welcome.

> **Note:** This is not an official Replicated product. See the disclaimer in
> [README.md](README.md) for details.

## Getting started

```bash
git clone https://github.com/replicatedhq/replicated-dotnet
cd replicated-dotnet
dotnet restore
dotnet build
dotnet test Replicated.Tests/
```

## Project layout

| Directory | Purpose |
|---|---|
| `Replicated/` | Main SDK library (the NuGet package) |
| `Replicated.Tests/` | Unit tests |
| `Replicated.IntegrationTests/` | Integration tests (require mock server or live environment) |
| `Replicated.IntegrationTests.MockServer/` | Local mock server for integration testing |
| `Replicated.Example/` | Example application |

## Making changes

1. **Fork** the repository and create a branch from `main`.
2. **Write tests** for any new behavior. Run `dotnet test Replicated.Tests/` before opening a PR.
3. **Keep the format clean**: `dotnet format --verify-no-changes` must pass (CI enforces this).
4. **Follow semver** — see the versioning policy in [CHANGELOG.md](CHANGELOG.md).
5. **Update CHANGELOG.md** under `[Unreleased]` with a summary of your changes.

## Running integration tests locally

```bash
# Terminal 1 — start the mock server
cd Replicated.IntegrationTests.MockServer
dotnet dev-certs https --trust   # first time only
dotnet run

# Terminal 2 — run integration tests
dotnet test Replicated.IntegrationTests/
```

## Code style

- Follow the conventions in `.editorconfig` (enforced by `dotnet format`).
- Private fields use `_camelCase`.
- All public APIs must have XML doc comments.
- All new JSON types must be registered in `ReplicatedJsonContext` for AOT/trim safety.

## Reporting bugs and requesting features

Please open a [GitHub Issue](https://github.com/replicatedhq/replicated-dotnet/issues) using
one of the provided templates. Include as much detail as possible — SDK version, .NET version,
and a minimal reproduction if applicable.

## Pull requests

- Keep PRs focused — one feature or fix per PR.
- Link to the relevant issue in the PR description.
- CI must be green (build, tests, format check, vulnerability scan) before merge.

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
