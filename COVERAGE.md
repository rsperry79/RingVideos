# Code Coverage Setup

This project now includes comprehensive code coverage configuration for both the API and main application.

## Quick Start

### 1. Initial Setup
Run the setup script to install ReportGenerator (one-time):
```powershell
.\setup-coverage.ps1
```

### 2. Generate Coverage Reports

**For the Ring API:**
```powershell
.\external\RingApi\coverage.ps1
```

**For the RingVideos App:**
```powershell
.\coverage.ps1
```

Both scripts will:
- Run all unit tests
- Collect code coverage data
- Generate HTML coverage reports in `TestResults\Coverage\`
- Display the path to the HTML report

## What's Included

### Dependencies
- **Coverlet.Collector** (v6.0.x): Collects code coverage data during test execution
- **ReportGenerator** (v5.2.1): Generates human-readable HTML reports from coverage data
- **Updated Test Frameworks**: Latest stable versions of MSTest/xUnit and test SDKs

### Configuration Files
- **`.runsettings`**: Configures coverage collection
  - OpenCover XML format for compatibility
  - Excludes test assemblies from coverage
  - Configured for parallel test execution

### Scripts
- **`coverage.ps1`** (root): Run coverage for the main app
- **`external/RingApi/coverage.ps1`**: Run coverage for the API
- **`setup-coverage.ps1`**: Install required global tools

## Understanding Coverage Reports

The HTML report shows:
- **Line Coverage**: % of lines executed during tests
- **Branch Coverage**: % of decision branches covered
- **Method Coverage**: % of methods called
- **Class Coverage**: % of classes exercised

### Coverage Goals
Typical targets:
- **Critical paths**: 80-100% coverage
- **Business logic**: 70-85% coverage
- **Utilities/Helpers**: 60-75% coverage
- **UI code**: 40-60% coverage (if tested at all)

## CI/CD Integration

To integrate into CI/CD pipelines, use the test commands:

```bash
# API
dotnet test external/RingApi/UnitTest/Unit\ Test.csproj --collect:"XPlat Code Coverage" --settings external/RingApi/.runsettings

# App
dotnet test RingVideos.Tests/RingVideos.Tests.csproj --collect:"XPlat Code Coverage" --settings .runsettings
```

## Troubleshooting

### ReportGenerator not found
Install globally:
```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### No coverage file generated
- Ensure tests are actually running (check for test failures)
- Verify `.runsettings` file is in the correct location
- Run with `--verbose` flag for diagnostics

### Coverage is 0%
- Check that test projects properly reference the projects being tested
- Verify the `IncludeDirectories` in `.runsettings` match your project structure

## Further Reading
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [OpenCover Format](https://github.com/OpenCover/OpenCover/wiki/OpenCover-Documentation)
