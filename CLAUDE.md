# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

This repository uses Cake Build for cross-platform build automation. Use the following commands:

### Core Development Commands
- **Build the solution**: `./build.sh` or `./build.ps1`
- **Run tests**: `./build.sh --target=Test` or `./build.ps1 --target=Test`
  - Note: running tests also builds the solution.
- **Clean and rebuild**: `./build.sh --clean` or `./build.ps1 --clean`
- **Create NuGet packages**: `./build.sh --target=Pack` or `./build.ps1 --target=Pack`

### Available Build Targets
- `Clean` - Clean build artifacts
- `Restore` - Restore NuGet packages
- `Build` - Build the solution
- `Test` - Run unit tests (default target)
- `Pack` - Create NuGet packages
- `BillOfMaterials` - Generate bill of materials

### Running Specific Tests
Use standard dotnet CLI commands for granular test execution:
- **Run all tests**: `dotnet test`
- **Run tests in specific project**: `dotnet test test/JsonTimeSeriesExtractor.Tests/`
- **Run benchmarks**: `dotnet run --project test/JsonTimeSeriesExtractor.Benchmarks/`

## Architecture Overview

This is a C# library for extracting time series data from JSON using `System.Text.Json`. The library processes JSON objects and extracts key-timestamp-value pairs suitable for time series storage.

### Key Components

**Core Library** (`src/JsonTimeSeriesExtractor/`):
- `TimeSeriesExtractor` - Main entry point with static `GetSamples()` methods
- `TimeSeriesExtractorOptions` - Configuration for extraction behavior
- `TimeSeriesSample` - Represents extracted data points with Key, Timestamp, and Value
- `JsonPointerMatch*` - Pattern matching system for JSON pointer filtering
- `ElementStack` and related classes - Internal state management for recursive processing

**Key Features**:
- Recursive JSON object traversal with configurable depth limits
- Flexible timestamp parsing (ISO strings, Unix timestamps, custom parsers)
- JSON Pointer-based property filtering with wildcard and MQTT-style patterns
- Template-based sample key generation with placeholder substitution
- Support for nested timestamps and array processing

### Project Structure
- **Main library**: `src/JsonTimeSeriesExtractor/` - Core extraction functionality
- **CLI sample**: `samples/JsonTimeSeriesExtractor.Cli/` - Command-line demonstration tool
- **Unit tests**: `test/JsonTimeSeriesExtractor.Tests/` - xUnit v3-based test suite
- **Benchmarks**: `test/JsonTimeSeriesExtractor.Benchmarks/` - Performance testing

## Development Guidelines

### Code Style
- Follow `.editorconfig` formatting rules
- Use British English spelling in documentation and comments
- Async methods must be suffixed with `Async`
- XML documentation should wrap lines after 100 characters
- Use inclusive language (e.g., "allow list" not "whitelist")

### Package Management
- Uses Central Package Management - versions defined in `Directory.Packages.props`
- Do not include version numbers in `<PackageReference>` elements

### Testing Approach
- Uses xUnit v3 framework
- Tests should fail initially, then be made to pass
- Test projects must be executable (`<OutputType>Exe</OutputType>`)
- File system tests should use temporary directories created in class constructors or `IClassFixture<T>`
- Prefer adding tests to existing test projects

### Git Workflow
- Do not work directly on `main` branch
- Create descriptive branch names (e.g., `feature/add-wildcard-matching`)
- Sign commits if user has signing configured
- Create pull requests against `main` with clear descriptions

## Library Usage Patterns

The library is designed for extracting time series data from IoT devices and similar JSON data sources. Common usage involves:

1. **Basic extraction**: `TimeSeriesExtractor.GetSamples(jsonString)`
2. **Custom timestamp property**: Configure `TimestampProperty` in options
3. **Property filtering**: Use `CanProcessElement` delegate or `CreateJsonPointerMatchDelegate()`
4. **Recursive processing**: Enable `Recursive` mode for nested objects
5. **Key templating**: Use `Template` property for custom sample key formats

The main complexity lies in the recursive processing system and the flexible pattern matching for JSON pointer filtering.