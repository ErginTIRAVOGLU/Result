# Copilot Instructions for ErginWebDev.Result

## Project Overview
A lightweight .NET 9.0 NuGet package implementing the Result pattern for functional error handling. This is a **library package** designed for consumption by other .NET projects. Single-file implementation (`Result.cs`) with immutable record types for success/failure state management with HTTP status codes.

**NuGet Package**: `ErginWebDev.Result` - Published to NuGet.org for public consumption

## Architecture & Design Patterns

### Core Types
- `ResultBase`: Abstract base record with `Success`, `Message`, `Errors`, and `HttpStatusCode`
- `Result`: Non-generic sealed record for operations without return data
- `Result<T>`: Generic sealed record for operations returning typed data via `Data` property
- All types are **immutable records** using `init` accessors

### Key Design Decisions
- **Factory pattern**: Use static methods (`Success`, `Fail`) instead of constructors
- **Guard invariant**: Constructor validates that successful results cannot contain errors (throws `InvalidOperationException`)
- **HTTP-aware**: `HttpStatusCode` property defaults to 200 OK for success, 400 BadRequest for failures
- **Null safety**: Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- **Functional programming**: Supports `Match`, `Map`, `Bind` for composable operations
- **Generic errors**: `Result<TData, TError>` allows strongly-typed error objects

## Code Conventions

### Creating Results
```csharp
// Correct: Use static factory methods
var success = Result<User>.Success(user, "User created", HttpStatusCode.Created);
var fail = Result.Fail("Operation failed", errors, HttpStatusCode.NotFound);

// Implicit conversion
Product product = GetProduct();
Result<Product> result = product; // Auto-converts to Success

// Wrong: Don't use constructors directly (they're private)
var result = new Result<User>(...);  // ❌ Won't compile
```

### Success Results
- May omit message (nullable)
- StatusCode defaults to `HttpStatusCode.OK`
- Generic version: `Data` is required (can be `default`)
- Never include errors (enforced by constructor guard)

### Failure Results
- Message is **required** (non-nullable parameter)
- StatusCode defaults to `HttpStatusCode.BadRequest`
- Errors are optional (converted to empty list if null)
- `Data` is always `default` for generic failures

## Development Workflow

### Building
```bash
dotnet build src/Result.sln
```

### NuGet Packaging & Publishing

#### Create Package
```bash
cd src/ErginWebDev.Result
dotnet pack -c Release -o ../../nupkg
```
Outputs `.nupkg` file to `nupkg/` directory at repository root.

#### Publish to NuGet.org
```bash
dotnet nuget push nupkg/ErginWebDev.Result.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

#### Package Metadata (in `.csproj`)
- **PackageId**: `ErginWebDev.Result` - Must be unique on NuGet.org
- **Version**: Semantic versioning (currently 1.0.0) - increment for each release
- **License**: MIT
- **PackageReadmeFile**: `README.md` is included in package
- **RepositoryUrl**: Links to GitHub repository
- **PackageTags**: `result;pattern;error-handling;csharp;dotnet` for discoverability

### Project Structure
```
src/
  Result.sln                      # Single-project solution
  ErginWebDev.Result/
    ErginWebDev.Result.csproj     # Package configuration
    Result.cs                     # All implementation code
    README.md                     # Usage examples for NuGet
```

## Common Patterns

### Validation Results
```csharp
// Multiple validations with error aggregation
var result = Result.Validate(
    () => ValidateEmail(email),
    () => ValidatePassword(password),
    () => ValidateAge(age)
);

// Manual validation
var errors = validationFailures.Select(f => f.ErrorMessage);
return Result<T>.Fail("Validation failed", errors, HttpStatusCode.UnprocessableEntity);
```

### Try Pattern - Exception Handling
```csharp
// For actions
var result = Result.Try(() => File.Delete("file.txt"), "Failed to delete file");

// For functions
var result = Result<User>.Try(() => _repository.GetById(id), "User not found");

// With custom error types
public record DomainError(string Code, string Message);
var result = Result<Order, DomainError>.Try(
    () => CreateOrder(request),
    errorFactory: ex => new DomainError("ORDER_ERROR", ex.Message)
);
```

### Functional Composition with Map/Bind
```csharp
// Map: Transform data
var emailResult = userResult.Map(user => user.Email);

// Bind: Chain operations that return Result
var orderResult = userResult
    .Bind(user => GetOrders(user.Id))
    .Bind(orders => CalculateTotal(orders))
    .WithStatusCode(HttpStatusCode.OK);
```

### Match Pattern
```csharp
// ASP.NET Core controller
return result.Match(
    onSuccess: data => Ok(data),
    onFailure: errors => BadRequest(new { errors })
);

// Business logic
var message = result.Match(
    onSuccess: user => $"Welcome {user.Name}",
    onFailure: errors => $"Failed: {string.Join(", ", errors)}"
);
```

### Domain Operations
```csharp
public Result<Order> PlaceOrder(OrderRequest request)
{
    if (!IsValid(request))
        return Result<Order>.Fail("Invalid order", GetValidationErrors());
    
    var order = CreateOrder(request);
    return Result<Order>.Success(order, "Order placed", HttpStatusCode.Created);
}
```

### Checking Results
```csharp
var result = await service.GetUserAsync(id);
if (result.Success) // or result.IsFailure
{
    ProcessUser(result.Data);  // Data is T? for Result<T>
}
else
{
    LogErrors(result.Message, result.Errors);
}
```

### Fluent API
```csharp
var result = Result<User>.Success(user)
    .WithStatusCode(HttpStatusCode.Created)
    .WithMessage("User created successfully");
```

### Generic Error Types
```csharp
public record ValidationError(string Field, string Message, string Code);

var errors = new[] {
    new ValidationError("Email", "Required", "REQUIRED"),
    new ValidationError("Password", "Too short", "MIN_LENGTH")
};

var result = Result<User, ValidationError>.Fail("Validation failed", errors);

// Access typed errors
foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.Field}: {error.Message} ({error.Code})");
}
```

## NuGet Package Development

### Before Publishing Checklist
1. **Version Bump**: Update `<Version>` in `.csproj` following SemVer
   - Patch (1.0.X): Bug fixes, no breaking changes
   - Minor (1.X.0): New features, backward compatible
   - Major (X.0.0): Breaking changes to API
2. **README.md**: Keep usage examples current with API changes
3. **Test Locally**: Use `dotnet pack` and test in a local project before publishing
4. **Release Notes**: Document changes in GitHub releases

### Testing Package Locally
```bash
# Pack the library
dotnet pack -c Release -o ../../nupkg

# In a test project
dotnet add package ErginWebDev.Result --source /path/to/nupkg
```

### Public API Surface
- Keep API minimal and backward compatible
- Breaking changes require major version bump
- All public types are sealed records - no inheritance allowed
- Factory methods (`Succeed`, `Fail`) are the only creation API

## API Features

### Core Features
- ✅ `Success` / `Fail` factory methods
- ✅ `IsFailure` property (computed from `Success`)
- ✅ `Try` pattern for exception handling
- ✅ `Match` for pattern matching
- ✅ `Map` / `Bind` for functional composition
- ✅ `WithStatusCode` / `WithMessage` fluent methods
- ✅ `Validate` for aggregating multiple validations
- ✅ Implicit conversion from `T` to `Result<T>`
- ✅ XML documentation for IntelliSense

### Result Types
- `Result` - No data, string errors
- `Result<T>` - Typed data, string errors
- `Result<TData, TError>` - Typed data, typed errors

## Constraints & Gotchas
- No test project exists - library focused on single implementation file
- No async/await patterns - synchronous API only (works with async methods though)
- `Errors` is `IReadOnlyList<string>` or `IReadOnlyList<TError>` - immutable after construction
- Records use structural equality - two results with same values are equal
- `Match`, `Map`, `Bind` handle exceptions internally and convert to Fail results
- **Breaking changes** require major version bump on NuGet
