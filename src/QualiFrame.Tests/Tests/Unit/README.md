# Unit Tests

This directory contains unit tests for framework components.

## Test Structure

- **LoggerTests.cs** - Tests for Logger class
- **ConfigurationValidatorTests.cs** - Tests for configuration validation
- **DataRepositoryFactoryTests.cs** - Tests for data repository factory
- **SqlDataRepositoryTests.cs** - Tests for SQL repository (no live database required)
- **VideoRecordingManagerTests.cs** - Tests for opt-in video frame capture (no-op when disabled)
- **DriverPoolTests.cs** - Tests for driver pooling
- **EnhancedWebElementTests.cs** - Tests for enhanced web element
- **SecretsManagerTests.cs** - Tests for secrets management
- **HealeniumClientTests.cs** - Tests for Healenium HTTP client and locator mapping
- **HealeniumEvidenceTests.cs** - Tests for versioned locator evidence, scoring, and promotion rules

## Running Tests

```bash
# Run all unit tests
dotnet test --filter "Category=Unit"

# Run specific test class
dotnet test --filter "FullyQualifiedName~LoggerTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

Current coverage includes:
- Logger functionality
- Configuration validation
- Data repository factory
- SQL repository (no live database)
- Video recording manager (disabled path)
- Driver pool management
- Component initialization

## Adding New Tests

1. Create test class in `Tests/Unit/` directory
2. Use `[TestFixture]` attribute
3. Follow naming convention: `{ClassName}Tests.cs`
4. Use `[SetUp]` and `[TearDown]` for test initialization/cleanup
5. Use descriptive test method names

**Example:**
```csharp
[TestFixture]
public class MyClassTests
{
    [SetUp]
    public void SetUp()
    {
        // Initialize test data
    }

    [Test]
    public void MyMethod_WithValidInput_ShouldReturnExpectedResult()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

