# Unit Tests

This directory contains unit tests for the multiplayer prototype project.

## Test Projects

### SharedUnitTests
Tests for the Shared library components:
- **EntityTests.cs** - Tests for the Entity class and component management
- **EntityIdTests.cs** - Tests for the EntityId struct
- **ComponentTests.cs** - Tests for all component classes (Position, Velocity, Health, etc.)

### ServerUnitTests
Tests for the Server application:
- **SceneLoaderTests.cs** - Tests for scene loading functionality

## Running Tests

### Using .NET CLI
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Tests/Shared/SharedUnitTests.csproj
dotnet test Tests/Server/ServerUnitTests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with verbose output
dotnet test --verbosity normal
```

### Using Visual Studio
1. Open the `Server.sln` solution file
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test > Test Explorer)
4. Run all tests or specific test methods

### Using Visual Studio Code
1. Install the .NET Core Test Explorer extension
2. Open the workspace
3. Use the Test Explorer panel to run tests

## Test Coverage

The tests cover:
- Entity component management (add, remove, query)
- Entity ID generation and uniqueness
- Component instantiation and property access
- Scene loading from JSON files
- Error handling for invalid inputs

## Adding New Tests

When adding new functionality:
1. Create corresponding test files in the appropriate test project
2. Follow the naming convention: `[ClassName]Tests.cs`
3. Use descriptive test method names: `[MethodName]_[Scenario]_[ExpectedResult]`
4. Follow the Arrange-Act-Assert pattern
5. Include both positive and negative test cases

## Test Dependencies

- **xUnit** - Testing framework
- **Microsoft.NET.Test.Sdk** - Test discovery and execution
- **coverlet.collector** - Code coverage collection 