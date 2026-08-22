# VideoForensics Development Guidelines

## Architectural Principles

### 1. All Public-Facing Code Under Interfaces

- **Every public API must be defined by an interface**, not direct class implementation
- Interfaces must live in a dedicated `Contracts` folder within the library
- All public-facing classes should implement their corresponding interface
- This enables:
  - Testing through mocking and substitution
  - Future provider implementations without breaking changes
  - Clear contracts between library consumers and implementations
  - Dependency injection patterns

**Example:**
```csharp
// ✅ GOOD: Interface in Contracts folder
public interface IVideoProvider
{
    Task<Device?> GetDeviceAsync(string deviceId);
}

// ✅ GOOD: Implementation uses the interface
public class RingVideoProvider : BaseVideoProvider, IVideoProvider { }
```

### 2. All Interfaces Must Be Tested

- Every public interface contract must have dedicated unit tests
- Tests verify:
  - Records/data types can be instantiated correctly
  - Service implementations satisfy the interface contract
  - Error scenarios are handled appropriately
- Use xUnit + Moq for testing
- Test projects follow naming: `<Library>.Tests`

**Example:**
```csharp
// VideoForensics.Providers.Common.Tests/ContractsTests.cs
[Fact]
public void AuthResult_CanBeCreatedSuccessfully()
{
    var result = new AuthResult(Success: true, AuthToken: "token");
    Assert.True(result.Success);
}

// VideoForensics.Providers.Ring.Tests/RingVideoProviderTests.cs
[Fact]
public void RingVideoProvider_ImplementsIVideoProvider()
{
    var provider = new RingVideoProvider(logger, session);
    Assert.IsAssignableFrom<IVideoProvider>(provider);
}
```

### 3. Platform-Agnostic Code via NuGet Packages

- **Avoid tight coupling to specific platforms or external APIs**
- Use well-maintained, regularly-updated NuGet packages for cross-cutting concerns
- Create abstraction layers when integrating external SDKs (e.g., Ring.Api.*)

**Recommended Packages:**
- **Logging:** `Microsoft.Extensions.Logging` (standard, widely-adopted)
- **Dependency Injection:** `Microsoft.Extensions.DependencyInjection` (standard)
- **Testing:** `xunit`, `Moq` (industry standard, regularly maintained)
- **Configuration:** `Microsoft.Extensions.Configuration` (consistent with DI)

**Anti-Patterns:**
- ❌ Direct dependency on vendor SDK in business logic
- ❌ Hard-coded platform-specific code paths
- ❌ Obsolete or unmaintained packages

**Example of Proper Abstraction:**

```csharp
// ✅ GOOD: Platform-agnostic interface in Common
namespace VideoForensics.Providers.Common.Contracts
{
    public interface IMediaDownloadService
    {
        Task<DownloadResult> DownloadVideosAsync(string deviceId, string outputPath, ...);
    }
}

// ✅ GOOD: Ring-specific implementation bridges the gap
namespace VideoForensics.Providers.Ring.Services
{
    public class RingMediaDownloadService : IMediaDownloadService
    {
        private readonly Session _session; // Ring SDK dependency isolated here
        
        public async Task<DownloadResult> DownloadVideosAsync(...) { ... }
    }
}

// ✅ GOOD: Consumer only knows IMediaDownloadService, not Ring
public class VideoForensicsCore
{
    private readonly IVideoProvider _provider; // Platform-agnostic
}
```

## Project Structure

```
VideoForensics/                          # Console app - UI & bootstrap only
├── Program.cs                           # Entry point, service configuration
├── MenuManager.cs                       # UI presentation

VideoForensics.Core/                     # ❌ DEPRECATED - use Providers layer
├── (being phased out in favor of Providers pattern)

VideoForensics.Providers.Common/         # Platform-agnostic interfaces
├── Contracts/
│   ├── IVideoProvider.cs               # Main provider interface
│   ├── IProviderAuthService.cs         # Auth contracts
│   ├── IDeviceDiscoveryService.cs      # Device contracts
│   ├── IMediaDownloadService.cs        # Download contracts
│   └── IEventAndConfigService.cs       # Event contracts
└── VideoForensics.Providers.Common.csproj

VideoForensics.Providers.Common.Tests/   # Contract verification tests
├── ContractsTests.cs
└── VideoForensics.Providers.Common.Tests.csproj

VideoForensics.Providers.Core/           # Base classes for provider implementations
├── BaseVideoProvider.cs                 # Abstract base with logging
└── VideoForensics.Providers.Core.csproj

VideoForensics.Providers.Core.Tests/     # Base provider tests
├── BaseVideoProviderTests.cs
└── VideoForensics.Providers.Core.Tests.csproj

VideoForensics.Providers.Ring/           # Ring.com provider (current implementation)
├── RingVideoProvider.cs                 # Orchestrates all Ring services
├── Services/
│   ├── RingAuthService.cs              # Implements IProviderAuthService
│   ├── RingDeviceDiscoveryService.cs   # Implements IDeviceDiscoveryService
│   ├── RingMediaDownloadService.cs     # Implements IMediaDownloadService
│   └── RingEventAndConfigService.cs    # Implements IEventAndConfigService
└── VideoForensics.Providers.Ring.csproj

VideoForensics.Providers.Ring.Tests/     # Ring provider integration tests
├── RingVideoProviderTests.cs
└── VideoForensics.Providers.Ring.Tests.csproj

VideoForensics.Providers.Wyze/           # (Future) Wyze provider - same interface pattern
VideoForensics.Providers.Wyze.Tests/     # (Future) Wyze tests
```

## Multi-Provider Pattern

When adding a new provider (Wyze, Blue Iris, etc.):

1. **Do NOT create new interfaces** - reuse `VideoForensics.Providers.Common.Contracts.*`
2. **Create new provider library:** `VideoForensics.Providers.<Vendor>/`
3. **Implement all four services:**
   - `<Vendor>AuthService : IProviderAuthService`
   - `<Vendor>DeviceDiscoveryService : IDeviceDiscoveryService`
   - `<Vendor>MediaDownloadService : IMediaDownloadService`
   - `<Vendor>EventAndConfigService : IEventAndConfigService`
4. **Implement provider class:** `<Vendor>VideoProvider : BaseVideoProvider`
5. **Add comprehensive tests:** `VideoForensics.Providers.<Vendor>.Tests/`
6. **Consumer code uses `IVideoProvider` only** - never depends on specific vendor implementation

## NuGet Package Strategy

### Dependency Management
- Review and update dependencies quarterly
- Use semantic versioning constraints: `[1.0, 2.0)` not exact pinning
- Check for security advisories: `dotnet list package --vulnerable`
- Prefer packages with active community and regular updates

### Security
- All external packages must come from nuget.org (official source)
- Review package licenses for compliance
- Never use pre-release packages in production libraries
- Report security vulnerabilities to package maintainers

### Current Dependencies
```
Core Packages:
- Microsoft.Extensions.Logging (10.0.11+)
- Microsoft.Extensions.Logging.Abstractions (10.0.11+)
- Spectre.Console (0.57.2+) - for rich console UI

Testing:
- xunit (2.9.2+)
- xunit.runner.visualstudio (2.8.0+)
- Moq (4.20.70+)
- Microsoft.NET.Test.Sdk (17.13.0+)

External APIs:
- VideoForensics.Providers.Ring.* (from external/RingApi submodule)
```

## Development Workflow

### When Adding a New Feature
1. **Start with interface contract** in the appropriate `Contracts/` folder
2. **Write tests first** to verify the interface contract
3. **Implement** the interface in the relevant provider or core library
4. **Run full test suite** to ensure no regressions
5. **Update dependencies** if new packages are needed

### When Modifying Existing Code
1. **Preserve interface contracts** - breaking changes require major version bump
2. **Keep all tests passing** - add tests for new scenarios
3. **Update this CLAUDE.md** if architectural decisions change
4. **Commit with clear messages** explaining the "why"

## Testing Standards

### Unit Tests
- Location: `<Project>.Tests/<Feature>Tests.cs`
- Framework: xUnit
- Mocking: Moq
- Naming: `<Class>_<Scenario>_<Expected>()`

### Test Coverage Targets
- All public interfaces: 100%
- Core business logic: >80%
- External integrations: >70%
- Utilities: >60%

### Running Tests
```bash
# Run all provider tests
dotnet test VideoForensics.Providers.Common.Tests/
dotnet test VideoForensics.Providers.Core.Tests/
dotnet test VideoForensics.Providers.Ring.Tests/

# Run with coverage reporting
dotnet test /p:CollectCoverage=true
```

## Security Considerations

1. **No secrets in code** - Use configuration, environment variables, or credential stores
2. **No plain-text passwords** - Always hash and salt, or use provider APIs
3. **Input validation** - Validate all external input at API boundaries
4. **Least privilege** - Services only access what they need
5. **Logging sanitization** - Never log credentials, tokens, or PII

## Performance Guidelines

1. **Async/await by default** - All I/O operations should be async
2. **Connection pooling** - Reuse HTTP clients, don't create per-request
3. **Caching** - Cache device lists, locations; refresh on demand or periodic schedule
4. **Progress reporting** - Long operations should report progress via `IProgress<>`
5. **Cancellation tokens** - All async operations accept `CancellationToken`

## Documentation Standards

- **Public interfaces** - Include XML documentation comments
- **Complex algorithms** - Add clarifying comments on the "why"
- **Non-obvious behaviors** - Document assumptions and invariants
- **Breaking changes** - Update CLAUDE.md and commit message

Example:
```csharp
/// <summary>Downloads videos for a device within a date range</summary>
/// <param name="deviceId">The ID of the device to download from</param>
/// <param name="outputPath">Local directory to save videos</param>
/// <param name="startDate">Inclusive start date</param>
/// <param name="endDate">Inclusive end date</param>
/// <returns>DownloadResult indicating success and file count</returns>
public Task<DownloadResult> DownloadVideosAsync(
    string deviceId,
    string outputPath,
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken = default);
```

## Quick Checklist for New Code

- [ ] Public code is behind an interface in `Contracts/` folder
- [ ] Interface is tested with xUnit + Moq
- [ ] No direct dependency on external vendor SDK in public code
- [ ] Uses `Microsoft.Extensions.*` for logging/DI/config
- [ ] All async I/O uses `async/await` with cancellation tokens
- [ ] Error messages are logged and user-friendly
- [ ] Dependencies are from nuget.org and regularly maintained
- [ ] Tests pass: `dotnet test`
- [ ] No warnings in build: `dotnet build`
- [ ] Commit message explains the "why"
