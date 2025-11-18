# ErginWebDev.Result

[![NuGet](https://img.shields.io/nuget/v/ErginWebDev.Result.svg)](https://www.nuget.org/packages/ErginWebDev.Result/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](https://dotnet.microsoft.com/download)

A lightweight, modern Result pattern implementation for .NET applications with functional programming support. Provides type-safe error handling with HTTP status code integration, eliminating exception-based control flow.

[🇹🇷 Türkçe Dokümantasyon](#turkish-documentation)

## Table of Contents

- [Why Result Pattern?](#why-result-pattern)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
  - [Try Pattern](#try-pattern---exception-handling)
  - [Match Pattern](#match-pattern---functional-style)
  - [Map & Bind](#map--bind---chaining-operations)
  - [Validation](#validation---aggregate-multiple-checks)
  - [Generic Error Types](#generic-error-types)
- [ASP.NET Core Integration](#aspnet-core-integration)
  - [Controller-based Web API](#controller-based-web-api)
  - [Minimal API](#minimal-api)
  - [Service Layer](#service-layer-example)
- [Advanced Patterns](#advanced-patterns)
- [API Reference](#api-reference)
- [Best Practices](#best-practices)
- [Contributing](#contributing)
- [License](#license)

## Why Result Pattern?

### Traditional Exception Handling
```csharp
public User GetUser(int id)
{
    var user = _repository.Find(id);
    if (user == null)
        throw new NotFoundException("User not found"); // Exception for control flow ❌
    
    return user;
}
```

### With Result Pattern
```csharp
public Result<User> GetUser(int id)
{
    var user = _repository.Find(id);
    return user != null
        ? Result<User>.Success(user)
        : Result<User>.Fail("User not found", statusCode: HttpStatusCode.NotFound); // Explicit ✅
}
```

### Benefits

✅ **Explicit Error Handling**: Success/failure is part of the method signature  
✅ **No Hidden Exceptions**: All failure paths are visible  
✅ **Type-Safe**: Compile-time guarantees for error handling  
✅ **Functional Composition**: Chain operations with `Map`/`Bind`  
✅ **Better Testability**: No need to test exception paths  
✅ **Performance**: No exception overhead for expected failures  
✅ **HTTP Integration**: Built-in status code support for web APIs  

## Features

- 🎯 **Strongly-typed result handling** with `Result<T>` and non-generic `Result`
- 🔒 **Immutable records** for Success/Failure states
- 🌐 **HTTP status code integration** for web APIs
- 📝 **Custom messages** and data payload support
- 🎨 **Generic error types** (`Result<TData, TError>`) for domain-specific errors
- ⚡ **Functional programming** support: `Match`, `Map`, `Bind`
- 🛡️ **Exception handling** with `Try` pattern
- ✅ **Validation aggregation** with `Validate`
- 🔗 **Fluent API** with `WithStatusCode`, `WithMessage`
- 🔄 **Implicit conversion** from data to `Result<T>`
- 📚 **XML documentation** for IntelliSense
- 🔐 **Null safety** with nullable reference types
- 🚀 **.NET 8.0 & 9.0** compatible

## Installation

```bash
dotnet add package ErginWebDev.Result
```

**Requirements:**
- .NET 8.0 or later (.NET 8.0 and .NET 9.0 are both supported)
- No additional dependencies required

## Quick Start

### 1. Basic Usage

```csharp
using System.Net;
using ErginWebDev.Result;

// Success with data
var successResult = Result<User>.Success(user, "User created successfully", HttpStatusCode.Created);

// Success with default message and status
var simpleSuccess = Result<User>.Success(user);

// Implicit conversion
Product product = GetProduct();
Result<Product> result = product; // Automatically converts to Success

// Failure with message
var failResult = Result<User>.Fail("User not found", statusCode: HttpStatusCode.NotFound);

// Failure with validation errors
var errors = new List<string> { "Email is required", "Password is too short" };
var validationResult = Result<User>.Fail("Validation failed", errors, HttpStatusCode.UnprocessableEntity);
```

### Try Pattern - Exception Handling

```csharp
// For operations without return value
var result = Result.Try(() => File.Delete("file.txt"), "Failed to delete file");

// For operations with return value
var userResult = Result<User>.Try(() => _repository.GetById(id), "User not found");

if (userResult.Success)
{
    Console.WriteLine($"User found: {userResult.Data.Name}");
}
```

### Match Pattern - Functional Style

```csharp
var message = result.Match(
    onSuccess: user => $"Welcome, {user.Name}!",
    onFailure: errors => $"Error: {string.Join(", ", errors)}"
);

// In ASP.NET Core controllers
return result.Match(
    onSuccess: data => Ok(data),
    onFailure: errors => BadRequest(new { errors })
);
```

### Map & Bind - Chaining Operations

```csharp
// Map: Transform the data
var emailResult = userResult.Map(user => user.Email);

// Bind: Chain operations that return Result
var orderTotal = userResult
    .Bind(user => GetOrders(user.Id))
    .Bind(orders => CalculateTotal(orders))
    .WithStatusCode(HttpStatusCode.OK);
```

### Validation - Aggregate Multiple Checks

```csharp
var validationResult = Result.Validate(
    () => ValidateEmail(request.Email),
    () => ValidatePassword(request.Password),
    () => ValidateAge(request.Age)
);

if (validationResult.IsFailure)
{
    return BadRequest(validationResult.Errors); // All errors collected
}
```

### Fluent API

```csharp
var result = Result<User>.Success(user)
    .WithStatusCode(HttpStatusCode.Created)
    .WithMessage("User successfully registered");
```

### Generic Error Types

```csharp
// Define custom error type
public record ValidationError(string Field, string Message, string Code);

// Use typed errors
var errors = new[]
{
    new ValidationError("Email", "Email is required", "REQUIRED"),
    new ValidationError("Password", "Password too short", "MIN_LENGTH")
};

var result = Result<User, ValidationError>.Fail("Validation failed", errors);

// Access typed errors
foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.Field}: {error.Message} ({error.Code})");
}

// Try with error factory
var orderResult = Result<Order, ValidationError>.Try(
    () => CreateOrder(request),
    errorFactory: ex => new ValidationError("Order", ex.Message, "CREATION_FAILED")
);
```

### Checking Results

```csharp
var result = GetUser(userId);

if (result.Success)
{
    Console.WriteLine($"Success: {result.Message}");
    Console.WriteLine($"Status Code: {result.StatusCode}");
    
    // Access result.Data for Result<T>
    if (result.Data != null)
    {
        Console.WriteLine($"User: {result.Data.Name}");
    }
}
else
{
    Console.WriteLine($"Failed: {result.Message}");
    Console.WriteLine($"Status Code: {result.StatusCode}");
    
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

## ASP.NET Core Integration

### Controller-based Web API

```csharp
using Microsoft.AspNetCore.Mvc;
using ErginWebDev.Result;
using System.Net;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id}")]
    public IActionResult GetProduct(int id)
    {
        var result = _productService.GetProduct(id);
        
        // Using Match pattern
        return result.Match(
            onSuccess: product => Ok(product),
            onFailure: errors => NotFound(new { message = result.Message, errors })
        );
    }

    [HttpPost]
    public IActionResult CreateProduct(CreateProductRequest request)
    {
        var result = _productService.CreateProduct(request);
        
        if (result.Success)
            return StatusCode((int)result.StatusCode, result.Data);
        
        return StatusCode((int)result.StatusCode, new 
        { 
            message = result.Message, 
            errors = result.Errors 
        });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateProduct(int id, UpdateProductRequest request)
    {
        // Chain validations and operations
        var result = Result.Validate(
                () => ValidateProductId(id),
                () => ValidateRequest(request)
            )
            .Bind(_ => _productService.UpdateProduct(id, request));

        return result.Match(
            onSuccess: () => Ok(result.Message),
            onFailure: errors => BadRequest(new { errors })
        );
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteProduct(int id)
    {
        var result = _productService.DeleteProduct(id);
        
        if (result.IsFailure)
            return StatusCode((int)result.StatusCode, new { result.Message, result.Errors });
        
        return NoContent();
    }
}
```

### Minimal API

```csharp
using ErginWebDev.Result;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// GET endpoint
app.MapGet("/api/products/{id}", (int id, IProductService service) =>
{
    var result = service.GetProduct(id);
    
    return result.Match(
        onSuccess: product => Results.Ok(product),
        onFailure: errors => Results.NotFound(new { message = result.Message, errors })
    );
});

// POST endpoint
app.MapPost("/api/products", (CreateProductRequest request, IProductService service) =>
{
    var result = service.CreateProduct(request);
    
    if (result.Success)
        return Results.Created($"/api/products/{result.Data.Id}", result.Data);
    
    return Results.BadRequest(new { result.Message, result.Errors });
});

// PUT endpoint with validation
app.MapPut("/api/products/{id}", (int id, UpdateProductRequest request, IProductService service) =>
{
    var result = Result<string>.Try(
        () => service.UpdateProduct(id, request),
        "Failed to update product"
    );

    return result.Match(
        onSuccess: message => Results.Ok(new { message }),
        onFailure: errors => Results.BadRequest(new { errors })
    );
});

// DELETE endpoint
app.MapDelete("/api/products/{id}", (int id, IProductService service) =>
{
    var result = service.DeleteProduct(id);
    
    return result.IsFailure 
        ? Results.BadRequest(new { result.Message, result.Errors })
        : Results.NoContent();
});

app.Run();
```

### Service Layer Example

```csharp
public interface IProductService
{
    Result<Product> GetProduct(int id);
    Result<Product> CreateProduct(CreateProductRequest request);
    Result<string> UpdateProduct(int id, UpdateProductRequest request);
    Result DeleteProduct(int id);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public Result<Product> GetProduct(int id)
    {
        // Using Try pattern for exception handling
        return Result<Product>.Try(
            () => _repository.GetById(id),
            $"Product with ID {id} not found"
        );
    }

    public Result<Product> CreateProduct(CreateProductRequest request)
    {
        // Validation first
        var validationResult = ValidateProduct(request);
        if (validationResult.IsFailure)
            return Result<Product>.Fail(validationResult.Message, validationResult.Errors);

        // Create product
        var product = new Product 
        { 
            Name = request.Name, 
            Price = request.Price 
        };
        
        _repository.Add(product);
        
        return Result<Product>.Success(
            product, 
            "Product created successfully", 
            HttpStatusCode.Created
        );
    }

    public Result<string> UpdateProduct(int id, UpdateProductRequest request)
    {
        var product = _repository.GetById(id);
        if (product == null)
            return Result<string>.Fail("Product not found", statusCode: HttpStatusCode.NotFound);

        product.Name = request.Name;
        product.Price = request.Price;
        _repository.Update(product);

        return Result<string>.Success(message: "Product updated successfully");
    }

    public Result DeleteProduct(int id)
    {
        return Result.Try(
            () => _repository.Delete(id),
            "Failed to delete product"
        );
    }

    private Result ValidateProduct(CreateProductRequest request)
    {
        return Result.Validate(
            () => string.IsNullOrEmpty(request.Name) 
                ? Result.Fail("Name is required") 
                : Result.Success(),
            () => request.Price <= 0 
                ? Result.Fail("Price must be greater than zero") 
                : Result.Success()
        );
    }
}
```

## Advanced Patterns

### Repository Pattern Integration

```csharp
public class UserRepository : IUserRepository
{
    private readonly DbContext _context;

    public Result<User> GetById(int id)
    {
        var user = _context.Users.Find(id);
        
        return user != null
            ? Result<User>.Success(user)
            : Result<User>.Fail("User not found", statusCode: HttpStatusCode.NotFound);
    }

    public Result<User> Create(User user)
    {
        return Result<User>.Try(
            () =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return user;
            },
            "Failed to create user"
        );
    }
}
```

### Domain-Driven Design (DDD) Example

```csharp
public record DomainError(string Code, string Message, string? Field = null);

public class Order
{
    public Result<Order, DomainError> Ship()
    {
        if (Status != OrderStatus.Paid)
        {
            var error = new DomainError("ORDER_NOT_PAID", "Cannot ship unpaid order");
            return Result<Order, DomainError>.Fail("Shipping failed", new[] { error });
        }

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        
        return Result<Order, DomainError>.Success(this, "Order shipped successfully");
    }

    public Result<Order, DomainError> AddItem(OrderItem item)
    {
        if (item.Quantity <= 0)
        {
            var error = new DomainError("INVALID_QUANTITY", "Quantity must be positive", "Quantity");
            return Result<Order, DomainError>.Fail("Invalid item", new[] { error });
        }

        Items.Add(item);
        return Result<Order, DomainError>.Success(this);
    }
}
```

### Async Operations

```csharp
public class UserService
{
    public async Task<Result<User>> GetUserAsync(int id)
    {
        return await Result<User>.Try(
            async () => await _repository.GetByIdAsync(id),
            "User not found"
        );
    }

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        // Validate
        var validationResult = await ValidateUserAsync(request);
        if (validationResult.IsFailure)
            return Result<User>.Fail(validationResult.Message, validationResult.Errors);

        // Create
        var user = new User { Email = request.Email };
        await _repository.AddAsync(user);
        
        return Result<User>.Success(user, "User created", HttpStatusCode.Created);
    }
}
```

## Properties & Methods

### Properties
- `Success` (bool): Indicates if the operation was successful
- `IsFailure` (bool): Indicates if the operation failed (computed from `!Success`)
- `Message` (string?): Optional message describing the result
- `Data` (T?): Payload data (only in `Result<T>` and `Result<TData, TError>`)
- `Errors` (IReadOnlyList<string> or IReadOnlyList<TError>): Immutable collection of errors
- `StatusCode` (HttpStatusCode): HTTP status code (defaults to 200 OK for success, 400 BadRequest for failures)

### Methods
- `Match<TResult>`: Pattern matching for success/failure cases
- `Map<TNew>`: Transform the data if successful
- `Bind<TNew>`: Chain operations that return Result
- `WithStatusCode`: Create new Result with different status code
- `WithMessage`: Create new Result with different message
- `Try`: Static method for exception handling
- `Validate`: Static method for aggregating multiple validations (Result only)

## API Reference

### Result (Non-Generic)

**Factory Methods:**
- `Result.Success(string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)` - Creates a successful result
- `Result.Fail(string message, IEnumerable<string>? errors = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)` - Creates a failed result
- `Result.Try(Action action, string? errorMessage = null)` - Executes action and returns Success/Fail based on exceptions
- `Result.Validate(params Func<Result>[] validations)` - Aggregates multiple validation results

**Instance Methods:**
- `Match<TResult>(Func<TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)` - Pattern matching
- `WithStatusCode(HttpStatusCode statusCode)` - Returns new Result with different status code
- `WithMessage(string? message)` - Returns new Result with different message

### Result\<T\> (Generic with Data)

**Factory Methods:**
- `Result<T>.Success(T? data = default, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)` - Creates a successful result with data
- `Result<T>.Fail(string message, IEnumerable<string>? errors = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)` - Creates a failed result
- `Result<T>.Try(Func<T> func, string? errorMessage = null)` - Executes function and returns Success/Fail based on exceptions

**Instance Methods:**
- `Match<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyList<string>, TResult> onFailure)` - Pattern matching
- `Map<TNew>(Func<T, TNew> mapper)` - Transforms data if successful
- `Bind<TNew>(Func<T, Result<TNew>> binder)` - Chains Result-returning operations
- `WithStatusCode(HttpStatusCode statusCode)` - Returns new Result with different status code
- `WithMessage(string? message)` - Returns new Result with different message

**Operators:**
- `implicit operator Result<T>(T data)` - Implicitly converts data to Success result

### Result\<TData, TError\> (Generic with Typed Errors)

**Factory Methods:**
- `Result<TData, TError>.Success(TData? data = default, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)` - Creates a successful result
- `Result<TData, TError>.Fail(string message, IEnumerable<TError>? errors = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest)` - Creates a failed result with typed errors
- `Result<TData, TError>.Try(Func<TData> func, string? errorMessage = null, Func<Exception, TError>? errorFactory = null)` - Executes function with typed error conversion

**Instance Methods:**
- `Match<TResult>(Func<TData, TResult> onSuccess, Func<IReadOnlyList<TError>, TResult> onFailure)` - Pattern matching
- `Map<TNewData>(Func<TData, TNewData> mapper)` - Transforms data if successful
- `Bind<TNewData>(Func<TData, Result<TNewData, TError>> binder)` - Chains operations
- `WithStatusCode(HttpStatusCode statusCode)` - Returns new Result with different status code
- `WithMessage(string? message)` - Returns new Result with different message

**Operators:**
- `implicit operator Result<TData, TError>(TData data)` - Implicitly converts data to Success result

## Best Practices

### ✅ DO
- Use `Match` for cleaner controller actions
- Use `Try` for operations that might throw exceptions
- Use `Validate` for aggregating multiple validation errors
- Use `Map`/`Bind` for chaining operations
- Use typed errors (`Result<T, TError>`) for domain-specific error handling
- Return specific HTTP status codes for different failure scenarios

### ❌ DON'T
- Don't catch exceptions manually when `Try` can handle it
- Don't check `result.Success` when `Match` is more appropriate
- Don't create results with constructors (they're private)
- Don't mutate results (they're immutable records)

## Usage Examples

All the examples above demonstrate the library's capabilities. See [Quick Start](#quick-start) section for immediate usage.

## Contributing

Contributions are welcome! This is an open-source project.

### How to Contribute
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development
```bash
# Clone the repository
git clone https://github.com/ErginWebDev/Result.git

# Build the project
cd Result
dotnet build src/Result.sln

# Create NuGet package
cd src/ErginWebDev.Result
dotnet pack -c Release
```

## Repository

- **GitHub**: [https://github.com/ErginWebDev/Result](https://github.com/ErginWebDev/Result)
- **NuGet**: [https://www.nuget.org/packages/ErginWebDev.Result](https://www.nuget.org/packages/ErginWebDev.Result)
- **Issues**: [https://github.com/ErginWebDev/Result/issues](https://github.com/ErginWebDev/Result/issues)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<a name="turkish-documentation"></a>

# 🇹🇷 Türkçe Dokümantasyon

## İçindekiler

- [Neden Result Pattern?](#neden-result-pattern-tr)
- [Özellikler](#özellikler-tr)
- [Kurulum](#kurulum-tr)
- [Hızlı Başlangıç](#hızlı-başlangıç-tr)
- [ASP.NET Core Entegrasyonu](#aspnet-core-entegrasyonu-tr)
- [Gelişmiş Kullanım](#gelişmiş-kullanım-tr)
- [En İyi Pratikler](#en-iyi-pratikler-tr)

## <a name="neden-result-pattern-tr"></a>Neden Result Pattern?

### Geleneksel Exception Handling
```csharp
public User GetUser(int id)
{
    var user = _repository.Find(id);
    if (user == null)
        throw new NotFoundException("Kullanıcı bulunamadı"); // Kontrol akışı için exception ❌
    
    return user;
}
```

### Result Pattern İle
```csharp
public Result<User> GetUser(int id)
{
    var user = _repository.Find(id);
    return user != null
        ? Result<User>.Success(user)
        : Result<User>.Fail("Kullanıcı bulunamadı", statusCode: HttpStatusCode.NotFound); // Açık ✅
}
```

### Faydaları

✅ **Açık Hata Yönetimi**: Başarı/başarısızlık metod imzasının bir parçası  
✅ **Gizli Exception Yok**: Tüm hata yolları görünür  
✅ **Tip Güvenli**: Derleme zamanı hata yönetimi garantisi  
✅ **Fonksiyonel Kompozisyon**: `Map`/`Bind` ile operasyon zincirleme  
✅ **Daha İyi Test Edilebilirlik**: Exception yollarını test etmeye gerek yok  
✅ **Performans**: Beklenen hatalar için exception maliyeti yok  
✅ **HTTP Entegrasyonu**: Web API'ler için yerleşik status code desteği  

## <a name="özellikler-tr"></a>Özellikler

- 🎯 **Strongly-typed result yönetimi** `Result<T>` ve non-generic `Result` ile
- 🔒 **Immutable record'lar** ile Başarı/Başarısızlık durumları
- 🌐 **HTTP status code entegrasyonu** web API'ler için
- 📝 **Özel mesajlar** ve data payload desteği
- 🎨 **Generic error tipleri** (`Result<TData, TError>`) domain-specific hatalar için
- ⚡ **Fonksiyonel programlama** desteği: `Match`, `Map`, `Bind`
- 🛡️ **Exception yönetimi** `Try` pattern ile
- ✅ **Validation birleştirme** `Validate` ile
- 🔗 **Fluent API** `WithStatusCode`, `WithMessage` ile
- 🔄 **Implicit dönüşüm** data'dan `Result<T>`'ye
- 📚 **XML dokümantasyon** IntelliSense için
- 🔐 **Null güvenlik** nullable reference types ile
- 🚀 **.NET 8.0 & 9.0** uyumlu

## <a name="kurulum-tr"></a>Kurulum

```bash
dotnet add package ErginWebDev.Result
```

**Gereksinimler:**
- .NET 8.0 veya üzeri (.NET 8.0 ve .NET 9.0 her ikisi de desteklenir)
- Ek bağımlılık gerekmez

## <a name="hızlı-başlangıç-tr"></a>Hızlı Başlangıç

### Temel Kullanım

```csharp
using System.Net;
using ErginWebDev.Result;

// Veri ile başarılı sonuç
var successResult = Result<User>.Success(user, "Kullanıcı başarıyla oluşturuldu", HttpStatusCode.Created);

// Basit başarı
var simpleSuccess = Result<User>.Success(user);

// Implicit dönüşüm
Product product = GetProduct();
Result<Product> result = product; // Otomatik Success'e dönüşür

// Hata mesajı ile başarısız sonuç
var failResult = Result<User>.Fail("Kullanıcı bulunamadı", statusCode: HttpStatusCode.NotFound);

// Validation hataları ile
var errors = new List<string> { "Email gerekli", "Şifre çok kısa" };
var validationResult = Result<User>.Fail("Validation başarısız", errors, HttpStatusCode.UnprocessableEntity);
```

### Try Pattern - Exception Yönetimi

```csharp
// Dönüş değeri olmayan operasyonlar için
var result = Result.Try(() => File.Delete("file.txt"), "Dosya silinemedi");

// Dönüş değeri olan operasyonlar için
var userResult = Result<User>.Try(() => _repository.GetById(id), "Kullanıcı bulunamadı");

if (userResult.Success)
{
    Console.WriteLine($"Kullanıcı bulundu: {userResult.Data.Name}");
}
```

### Match Pattern - Fonksiyonel Stil

```csharp
var message = result.Match(
    onSuccess: user => $"Hoşgeldin, {user.Name}!",
    onFailure: errors => $"Hata: {string.Join(", ", errors)}"
);

// ASP.NET Core controller'larda
return result.Match(
    onSuccess: data => Ok(data),
    onFailure: errors => BadRequest(new { errors })
);
```

### Map & Bind - Operasyon Zincirleme

```csharp
// Map: Veriyi dönüştür
var emailResult = userResult.Map(user => user.Email);

// Bind: Result dönen operasyonları zincirle
var orderTotal = userResult
    .Bind(user => GetOrders(user.Id))
    .Bind(orders => CalculateTotal(orders))
    .WithStatusCode(HttpStatusCode.OK);
```

### Validation - Çoklu Kontrol Birleştirme

```csharp
var validationResult = Result.Validate(
    () => ValidateEmail(request.Email),
    () => ValidatePassword(request.Password),
    () => ValidateAge(request.Age)
);

if (validationResult.IsFailure)
{
    return BadRequest(validationResult.Errors); // Tüm hatalar toplandı
}
```

### Fluent API

```csharp
var result = Result<User>.Success(user)
    .WithStatusCode(HttpStatusCode.Created)
    .WithMessage("Kullanıcı başarıyla kaydedildi");
```

### Generic Error Tipleri

```csharp
// Özel error tipi tanımla
public record ValidationError(string Field, string Message, string Code);

// Typed error'ları kullan
var errors = new[]
{
    new ValidationError("Email", "Email gerekli", "REQUIRED"),
    new ValidationError("Password", "Şifre çok kısa", "MIN_LENGTH")
};

var result = Result<User, ValidationError>.Fail("Validation başarısız", errors);

// Typed error'lara eriş
foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.Field}: {error.Message} ({error.Code})");
}
```

## <a name="aspnet-core-entegrasyonu-tr"></a>ASP.NET Core Entegrasyonu

### Controller-based Web API

```csharp
using Microsoft.AspNetCore.Mvc;
using ErginWebDev.Result;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id}")]
    public IActionResult GetProduct(int id)
    {
        var result = _productService.GetProduct(id);
        
        return result.Match(
            onSuccess: product => Ok(product),
            onFailure: errors => NotFound(new { message = result.Message, errors })
        );
    }

    [HttpPost]
    public IActionResult CreateProduct(CreateProductRequest request)
    {
        var result = _productService.CreateProduct(request);
        
        if (result.Success)
            return StatusCode((int)result.StatusCode, result.Data);
        
        return StatusCode((int)result.StatusCode, new 
        { 
            message = result.Message, 
            errors = result.Errors 
        });
    }
}
```

### Minimal API

```csharp
using ErginWebDev.Result;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/products/{id}", (int id, IProductService service) =>
{
    var result = service.GetProduct(id);
    
    return result.Match(
        onSuccess: product => Results.Ok(product),
        onFailure: errors => Results.NotFound(new { message = result.Message, errors })
    );
});

app.MapPost("/api/products", (CreateProductRequest request, IProductService service) =>
{
    var result = service.CreateProduct(request);
    
    if (result.Success)
        return Results.Created($"/api/products/{result.Data.Id}", result.Data);
    
    return Results.BadRequest(new { result.Message, result.Errors });
});

app.Run();
```

### Service Layer Örneği

```csharp
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public Result<Product> GetProduct(int id)
    {
        return Result<Product>.Try(
            () => _repository.GetById(id),
            $"ID {id} olan ürün bulunamadı"
        );
    }

    public Result<Product> CreateProduct(CreateProductRequest request)
    {
        // Önce validation
        var validationResult = ValidateProduct(request);
        if (validationResult.IsFailure)
            return Result<Product>.Fail(validationResult.Message, validationResult.Errors);

        var product = new Product { Name = request.Name, Price = request.Price };
        _repository.Add(product);
        
        return Result<Product>.Success(
            product, 
            "Ürün başarıyla oluşturuldu", 
            HttpStatusCode.Created
        );
    }

    private Result ValidateProduct(CreateProductRequest request)
    {
        return Result.Validate(
            () => string.IsNullOrEmpty(request.Name) 
                ? Result.Fail("İsim gerekli") 
                : Result.Success(),
            () => request.Price <= 0 
                ? Result.Fail("Fiyat sıfırdan büyük olmalı") 
                : Result.Success()
        );
    }
}
```

## <a name="gelişmiş-kullanım-tr"></a>Gelişmiş Kullanım

### Repository Pattern

```csharp
public class UserRepository : IUserRepository
{
    private readonly DbContext _context;

    public Result<User> GetById(int id)
    {
        var user = _context.Users.Find(id);
        
        return user != null
            ? Result<User>.Success(user)
            : Result<User>.Fail("Kullanıcı bulunamadı", statusCode: HttpStatusCode.NotFound);
    }

    public Result<User> Create(User user)
    {
        return Result<User>.Try(
            () =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
                return user;
            },
            "Kullanıcı oluşturulamadı"
        );
    }
}
```

### Domain-Driven Design (DDD)

```csharp
public record DomainError(string Code, string Message, string? Field = null);

public class Order
{
    public Result<Order, DomainError> Ship()
    {
        if (Status != OrderStatus.Paid)
        {
            var error = new DomainError("ORDER_NOT_PAID", "Ödenmemiş sipariş kargoya verilemez");
            return Result<Order, DomainError>.Fail("Kargolama başarısız", new[] { error });
        }

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        
        return Result<Order, DomainError>.Success(this, "Sipariş kargoya verildi");
    }
}
```

### Async Operasyonlar

```csharp
public class UserService
{
    public async Task<Result<User>> GetUserAsync(int id)
    {
        return await Result<User>.Try(
            async () => await _repository.GetByIdAsync(id),
            "Kullanıcı bulunamadı"
        );
    }

    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        var validationResult = await ValidateUserAsync(request);
        if (validationResult.IsFailure)
            return Result<User>.Fail(validationResult.Message, validationResult.Errors);

        var user = new User { Email = request.Email };
        await _repository.AddAsync(user);
        
        return Result<User>.Success(user, "Kullanıcı oluşturuldu", HttpStatusCode.Created);
    }
}
```

## <a name="en-iyi-pratikler-tr"></a>En İyi Pratikler

### ✅ YAPILMASI GEREKENLER
- Controller aksiyonları için `Match` kullanın
- Exception fırlatabilecek operasyonlar için `Try` kullanın
- Çoklu validation hataları için `Validate` kullanın
- Operasyon zincirleme için `Map`/`Bind` kullanın
- Domain-specific hata yönetimi için typed error'lar (`Result<T, TError>`) kullanın
- Farklı hata senaryoları için spesifik HTTP status code'lar dönün

### ❌ YAPILMAMASI GEREKENLER
- `Try` kullanabileceğiniz yerde manuel exception yakalamayın
- `Match` daha uygunken `result.Success` kontrolü yapmayın
- Constructor ile result oluşturmayın (private'dırlar)
- Result'ları mutate etmeyin (immutable record'lardır)

## API Referansı

Detaylı API dokümantasyonu için [API Reference](#api-reference) bölümüne bakın.

## Katkıda Bulunma

Katkılar memnuniyetle karşılanır! Bu açık kaynak bir projedir.

### Nasıl Katkıda Bulunulur
1. Repository'yi fork'layın
2. Feature branch oluşturun (`git checkout -b feature/harika-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Harika özellik eklendi'`)
4. Branch'inizi push edin (`git push origin feature/harika-ozellik`)
5. Pull Request açın

### Geliştirme
```bash
# Repository'yi klonlayın
git clone https://github.com/ErginWebDev/Result.git

# Projeyi build edin
cd Result
dotnet build src/Result.sln

# NuGet paketi oluşturun
cd src/ErginWebDev.Result
dotnet pack -c Release
```

## Repository

- **GitHub**: [https://github.com/ErginWebDev/Result](https://github.com/ErginWebDev/Result)
- **NuGet**: [https://www.nuget.org/packages/ErginWebDev.Result](https://www.nuget.org/packages/ErginWebDev.Result)
- **Issues**: [https://github.com/ErginWebDev/Result/issues](https://github.com/ErginWebDev/Result/issues)

## Lisans

Bu proje MIT Lisansı altında lisanslanmıştır - detaylar için [LICENSE](LICENSE) dosyasına bakın.

---

**Made with ❤️ by [ErginWebDev](https://github.com/ErginWebDev)**

````
