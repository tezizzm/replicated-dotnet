# Test Coverage Report

This document explains how to generate, run, and view test coverage reports for the Replicated .NET SDK.

## Prerequisites

1. **Coverlet** - Already included in the test projects via NuGet packages
2. **ReportGenerator** - Install as a global .NET tool:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

## Generating Coverage Reports

### Step 1: Clean Old Coverage Data (Optional)

Remove any existing coverage files before generating a new report:

```bash
rm -rf coverage/*
```

### Step 2: Run Unit Tests with Coverage

Run the unit tests and collect coverage data:

```bash
dotnet test Replicated.Tests/Replicated.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory:./coverage \
  --verbosity minimal
```

This will:
- Run all unit tests
- Collect coverage data using Coverlet
- Save coverage files to the `coverage/` directory

### Step 3: Run Integration Tests with Coverage (Optional)

If you want to include integration test coverage, ensure the mock server is running first:

```bash
# Terminal 1: Start the mock server
cd Replicated.IntegrationTests.MockServer
dotnet run
```

Then in another terminal:

```bash
# Terminal 2: Run integration tests with coverage
dotnet test Replicated.IntegrationTests/Replicated.IntegrationTests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory:./coverage \
  --verbosity minimal
```

### Step 4: Generate HTML Report

Generate a combined HTML report from all coverage data:

```bash
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"-*Tests*"
```

**Note:** If `reportgenerator` is not in your PATH, use the full path:
```bash
~/.dotnet/tools/reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:"Html;TextSummary" \
  -classfilters:"-*Tests*"
```

The `-classfilters:"-*Tests*"` option excludes test classes from the coverage report.

## Viewing Coverage Reports

### HTML Report (Recommended)

Open the generated HTML report in your browser:

```bash
# macOS
open coverage/report/index.html

# Linux
xdg-open coverage/report/index.html

# Windows
start coverage/report/index.html
```

Or manually navigate to: `coverage/report/index.html`

The HTML report provides:
- **Summary Page**: Overall coverage statistics (line, branch, method coverage)
- **Class Pages**: Detailed coverage for each class with line-by-line highlighting
- **Interactive Navigation**: Click through classes and files to see covered/uncovered lines
- **Color Coding**: Green for covered lines, red for uncovered lines

### Text Summary

View a quick text summary:

```bash
cat coverage/report/Summary.txt
```

## Coverage Report Files

After generation, you'll find:

- **`coverage/report/index.html`** - Main HTML report (open in browser)
- **`coverage/report/Summary.txt`** - Text summary of coverage
- **`coverage/*/coverage.cobertura.xml`** - Raw coverage data (one per test run)
- **`coverage/*/coverage.json`** - JSON format coverage data
- **`coverage/*/coverage.info`** - Info format coverage data

## Quick Reference

### Generate and View in One Command

```bash
# Clean, run tests, generate report, and open
rm -rf coverage/* && \
dotnet test Replicated.Tests/Replicated.Tests.csproj --collect:"XPlat Code Coverage" --results-directory:./coverage --verbosity minimal && \
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:"Html;TextSummary" -classfilters:"-*Tests*" && \
open coverage/report/index.html
```

### Integration Tests Coverage (Full Workflow)

```bash
# Terminal 1: Start mock server
cd Replicated.IntegrationTests.MockServer && dotnet run

# Terminal 2: Run tests and generate report
cd /path/to/replicated-dotnet
rm -rf coverage/*
dotnet test Replicated.Tests/Replicated.Tests.csproj --collect:"XPlat Code Coverage" --results-directory:./coverage --verbosity minimal
dotnet test Replicated.IntegrationTests/Replicated.IntegrationTests.csproj --collect:"XPlat Code Coverage" --results-directory:./coverage --verbosity minimal
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:"Html;TextSummary" -classfilters:"-*Tests*"
open coverage/report/index.html
```

## Troubleshooting

### ReportGenerator Not Found

If you get a "command not found" error:

1. Verify it's installed: `dotnet tool list -g`
2. Use the full path: `~/.dotnet/tools/reportgenerator`
3. Add to PATH: Add `~/.dotnet/tools` to your shell's PATH

### No Coverage Data Generated

- Ensure `coverlet.collector` package is referenced in the test project
- Check that tests actually ran (look for "Passed!" in test output)
- Verify coverage files exist: `ls coverage/*/coverage.cobertura.xml`

### Integration Tests Fail

- Ensure the mock server is running before running integration tests
- Check that environment variables are set if required
- See [Integration Tests README](../Replicated.IntegrationTests/README.md) for setup instructions
