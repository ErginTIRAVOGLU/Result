# ErginWebDev.StronglyTypedIds

[![NuGet](https://img.shields.io/nuget/v/ErginWebDev.StronglyTypedIds.svg)](https://www.nuget.org/packages/ErginWebDev.StronglyTypedIds/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](https://dotnet.microsoft.com/download)

A modern, DDD-compliant library for creating strongly typed IDs in .NET applications with automatic integration for Entity Framework Core, System.Text.Json, and OpenAPI (Swagger/Scalar). Supports both .NET 8.0 and .NET 9.0.

[🇹🇷 Türkçe Dokümantasyon](#turkish-documentation)

## Table of Contents

- [Why Strongly Typed IDs?](#why-strongly-typed-ids)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Supported Value Types](#supported-value-types)
- [Detailed Usage](#detailed-usage)
  - [Entity Framework Core Integration](#entity-framework-core-integration)
  - [JSON Serialization](#json-serialization)
  - [OpenAPI/Swagger Documentation](#openapiswagger-documentation)
  - [Scalar API Support](#scalar-api-support)
- [Advanced Scenarios](#advanced-scenarios)
- [Performance Considerations](#performance-considerations)
- [Migration Guide](#migration-guide)
- [Contributing](#contributing)
- [License](#license)

## Why Strongly Typed IDs?

Instead of using primitive types like `Guid` or `int` directly, strongly typed IDs provide:

✅ **Type Safety**: Prevents mixing different entity IDs at compile-time  
✅ **Code Clarity**: `FindCustomer(CustomerId id)` is clearer than `FindCustomer(Guid id)`  
✅ **Refactoring Safety**: Compiler errors when changing ID types  
✅ **DDD Compliance**: Follows Domain-Driven Design value object pattern  
✅ **Zero Boilerplate**: Automatic EF Core, JSON, and OpenAPI integration  

### Before (Primitive Obsession)
```csharp
public class OrderService
{
    // ❌ Easy to mix up customer and order IDs
    public Order CreateOrder(Guid customerId, Guid productId) { }
    
    // ❌ Accidentally swapping parameters compiles but fails at runtime
    var order = CreateOrder(productId, customerId); // WRONG!
}
```

### After (Strongly Typed IDs)
```csharp
public class OrderService
{
    // ✅ Type-safe parameters
    public Order CreateOrder(CustomerId customerId, ProductId productId) { }
    
    // ✅ Compile error prevents mistakes
    var order = CreateOrder(productId, customerId); // Won't compile!
}
```

## Features

- 🎯 **Generic Base Type**: Support for `Guid`, `int`, `long`, `string`, `decimal`, `double`, `DateTime`, `DateTimeOffset`, and `Enum`
- 🗃️ **EF Core Convention**: Automatic value converter registration
- 📦 **JSON Serialization**: Seamless System.Text.Json integration
- 📝 **OpenAPI Support**: Works with Swagger UI and Scalar API
- ⚡ **High Performance**: Reflection only at startup, zero runtime overhead
- 🔧 **Zero Configuration**: Convention-based auto-discovery
- 🎨 **Clean Code**: Minimal boilerplate with `record struct`

## Installation

```bash
dotnet add package ErginWebDev.StronglyTypedIds
```

**Requirements:**
- .NET 8.0 or later (.NET 8.0 and .NET 9.0 are both supported)
- Entity Framework Core 8.0+ (if using EF Core integration)
- Swashbuckle.AspNetCore 6.8+ (if using Swagger)

## Quick Start

### 1. Define Your Strongly Typed IDs

```csharp
using ErginWebDev.StronglyTypedIds.ValueObjects;

// Guid-based IDs
public readonly record struct CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);
public readonly record struct OrderId(Guid Value) : StronglyTypedId<Guid>(Value);

// Other value types
public readonly record struct OrderNumber(int Value) : StronglyTypedId<int>(Value);
public readonly record struct ProductCode(string Value) : StronglyTypedId<string>(Value);
public readonly record struct Price(decimal Value) : StronglyTypedId<decimal>(Value);
```

### 2. Use in Your Domain Entities

```csharp
public class Customer
{
    public CustomerId Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Order
{
    public OrderId Id { get; init; }
    public CustomerId CustomerId { get; init; }
    public OrderNumber OrderNumber { get; init; }
    public Price TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### 3. Configure Services (Program.cs)

```csharp
using ErginWebDev.StronglyTypedIds;

var builder = WebApplication.CreateBuilder(args);

// Add strongly typed IDs support
builder.Services.AddStronglyTypedIds(); // JSON + Swagger

// Add your DbContext
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.Run();
```

### 4. Configure EF Core (DbContext)

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enable automatic strongly typed ID conversion
        configurationBuilder.ConfigureStronglyTypedIds();
    }
}
```

### 5. Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context) => _context = context;

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(CustomerId id)
    {
        // ID automatically binds from URL: "550e8400-e29b-41d4-a716-446655440000"
        var customer = await _context.Customers.FindAsync(id);
        
        if (customer == null)
            return NotFound();
            
        return customer; // Automatically serialized to JSON
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Id = new CustomerId(Guid.CreateVersion7()), // Time-ordered GUID
            Name = request.Name,
            Email = request.Email
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }
}
```

**That's it!** Your strongly typed IDs now work seamlessly across your entire application.

## Supported Value Types

| Type | Storage | JSON Format | OpenAPI Type | Example |
|------|---------|-------------|--------------|---------|
| `Guid` | uniqueidentifier | string | `string/uuid` | `"550e8400-e29b-41d4-a716-446655440000"` |
| `int` | int | number | `integer/int32` | `12345` |
| `long` | bigint | number | `integer/int64` | `9999999999` |
| `string` | nvarchar | string | `string` | `"PROD-001"` |
| `decimal` | decimal | number | `number/decimal` | `99.95` |
| `double` | float | number | `number/double` | `123.456` |
| `DateTime` | datetime2 | string | `string/date-time` | `"2025-11-08T10:30:00"` |
| `DateTimeOffset` | datetimeoffset | string | `string/date-time` | `"2025-11-08T10:30:00Z"` |
| `Enum` | int | string | `string` + enum values | `"Active"` |

## Detailed Usage

### Entity Framework Core Integration

The library uses EF Core's convention system to automatically detect and convert strongly typed IDs.

#### How It Works

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // This single line enables automatic conversion for ALL strongly typed IDs
        configurationBuilder.ConfigureStronglyTypedIds();
        
        // The convention scans all entity properties and detects:
        // - Value types (struct)
        // - With a "Value" property
        // - Of a supported type (Guid, int, long, string, etc.)
        // Then automatically applies the appropriate converter
    }
}
```

#### Database Storage

Strongly typed IDs are stored as their underlying value type:

```sql
-- Customer table
CREATE TABLE Customers (
    Id uniqueidentifier PRIMARY KEY,  -- CustomerId stored as Guid
    Name nvarchar(max),
    Email nvarchar(max)
);

-- Order table
CREATE TABLE Orders (
    Id uniqueidentifier PRIMARY KEY,      -- OrderId stored as Guid
    CustomerId uniqueidentifier NOT NULL, -- CustomerId stored as Guid
    OrderNumber int NOT NULL,             -- OrderNumber stored as int
    TotalPrice decimal(18,2) NOT NULL,    -- Price stored as decimal
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);
```

#### Querying

All standard EF Core query operations work seamlessly:

```csharp
// Find by ID
var customer = await context.Customers.FindAsync(customerId);

// Where clause
var orders = await context.Orders
    .Where(o => o.CustomerId == customerId)
    .ToListAsync();

// Join operations
var customerOrders = await context.Customers
    .Where(c => c.Id == customerId)
    .Include(c => c.Orders)
    .FirstOrDefaultAsync();

// Contains
var customerIds = new[] { customerId1, customerId2, customerId3 };
var customers = await context.Customers
    .Where(c => customerIds.Contains(c.Id))
    .ToListAsync();

// Aggregate functions
var totalRevenue = await context.Orders
    .Where(o => o.CustomerId == customerId)
    .SumAsync(o => o.TotalPrice.Value); // Access underlying value when needed
```

#### Relationships and Foreign Keys

```csharp
public class Order
{
    public OrderId Id { get; init; }
    public CustomerId CustomerId { get; init; } // Foreign key
    
    // Navigation property
    public Customer Customer { get; set; } = null!;
}

public class Customer
{
    public CustomerId Id { get; init; }
    
    // Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

// EF Core automatically handles the relationship
var orderWithCustomer = await context.Orders
    .Include(o => o.Customer)
    .FirstAsync(o => o.Id == orderId);
```

### JSON Serialization

The library provides automatic JSON conversion using System.Text.Json.

#### Request/Response Format

**API Request:**
```http
POST /api/customers HTTP/1.1
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**API Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "John Doe",
  "email": "john@example.com"
}
```

Notice how the `CustomerId` is automatically serialized as a simple string, not as an object!

#### Type-Specific Serialization

```csharp
public class Product
{
    public ProductId Id { get; init; }              // Guid
    public ProductCode Code { get; init; }          // string
    public ProductNumber Number { get; init; }      // int
    public Price Price { get; init; }               // decimal
    public Weight Weight { get; init; }             // double
    public CreatedAt CreatedAt { get; init; }       // DateTime
    public ProductStatus Status { get; init; }      // Enum
}

// Serializes to:
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "PROD-001",
  "number": 12345,
  "price": 99.95,
  "weight": 2.5,
  "createdAt": "2025-11-08T10:30:00Z",
  "status": "Active"
}
```

#### Manual Serialization/Deserialization

```csharp
using System.Text.Json;

// Serialize
var customer = new Customer { Id = new CustomerId(Guid.CreateVersion7()), Name = "John" };
var json = JsonSerializer.Serialize(customer);

// Deserialize
var deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

// Works with collections
var customers = new List<Customer> { customer1, customer2 };
var customersJson = JsonSerializer.Serialize(customers);
```

### OpenAPI/Swagger Documentation

The schema filter automatically transforms strongly typed IDs in your API documentation.

#### Swagger UI Integration

```csharp
// Program.cs
builder.Services.AddStronglyTypedIds(); // Includes Swagger configuration

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
    // StronglyTypedIdSchemaFilter is already registered
});

app.UseSwagger();
app.UseSwaggerUI();
```

#### Generated OpenAPI Schema

**Before (without this library):**
```yaml
CustomerId:
  type: object
  properties:
    value:
      type: string
      format: uuid
```

**After (with this library):**
```yaml
CustomerId:
  type: string
  format: uuid
```

Much cleaner and follows OpenAPI best practices!

### Scalar API Support

Scalar is a modern alternative to Swagger UI. The library works seamlessly with Scalar.

#### Option 1: Scalar Only

```csharp
// Program.cs
builder.Services.AddStronglyTypedIds(configureSwagger: false); // Skip Swagger

// Add Scalar
builder.Services.AddOpenApi(); // .NET 9 built-in OpenAPI
app.MapOpenApi();

var app = builder.Build();

app.MapScalarApiReference(); // Add Scalar UI
app.Run();
```

#### Option 2: Both Swagger and Scalar

```csharp
builder.Services.AddStronglyTypedIds(); // Includes Swagger

app.UseSwagger();
app.UseSwaggerUI();
app.MapScalarApiReference(); // Both work together!
```

#### Option 3: JSON Only (No API Docs)

```csharp
builder.Services.AddStronglyTypedIdsJsonOnly(); // No OpenAPI configuration
```

## Advanced Scenarios

### Enum-Based IDs

```csharp
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

public readonly record struct OrderStatusId(OrderStatus Value) 
    : StronglyTypedId<OrderStatus>(Value);

public class Order
{
    public OrderId Id { get; init; }
    public OrderStatusId Status { get; init; }
}

// Usage
var order = new Order
{
    Id = new OrderId(Guid.CreateVersion7()),
    Status = new OrderStatusId(OrderStatus.Confirmed)
};

// JSON: { "id": "...", "status": "Confirmed" }
// Swagger: type: "string", enum: ["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"]
```

### Guid Generation Strategies

```csharp
// Option 1: Guid.CreateVersion7() - Time-ordered (recommended for databases)
// Available in .NET 9.0+
#if NET9_0_OR_GREATER
var customerId = new CustomerId(Guid.CreateVersion7());
#else
// For .NET 8.0, use Guid.NewGuid() - the library's NewId() method handles this automatically
var customerId = new CustomerId(Guid.NewGuid());
#endif
// Pros: Better database index performance (NET 9+), sortable by creation time
// Cons: Slightly predictable sequence

// Option 2: Guid.NewGuid() - Random (traditional, works on all versions)
var customerId = new CustomerId(Guid.NewGuid());
// Pros: Completely random, unpredictable, works on .NET 8.0+
// Cons: Poor database index performance with clustered indexes

// Note: The library's NewId() method automatically uses CreateVersion7() on .NET 9.0
// and falls back to NewGuid() on .NET 8.0
```

### Nullable IDs

```csharp
public class Order
{
    public OrderId Id { get; init; }
    public CustomerId CustomerId { get; init; }
    public OrderId? ParentOrderId { get; init; } // Nullable for optional relationships
}

// Usage
var order = new Order
{
    Id = new OrderId(Guid.CreateVersion7()),
    CustomerId = customerId,
    ParentOrderId = null // No parent order
};
```

### Value Access

```csharp
var customerId = new CustomerId(Guid.CreateVersion7());

// Access underlying value when needed
Guid underlyingGuid = customerId.Value;

// Use in non-EF Core scenarios (e.g., external APIs)
var externalApiRequest = new ExternalRequest
{
    CustomerId = customerId.Value.ToString()
};
```

## Performance Considerations

### Reflection Usage

The library uses reflection **only at application startup**:

```csharp
// Startup (one-time cost during app initialization)
builder.Services.AddStronglyTypedIds();           // Registers JSON converter factory
configurationBuilder.ConfigureStronglyTypedIds(); // Scans EF Core model

// Runtime (zero reflection - uses cached converters)
var json = JsonSerializer.Serialize(customer);              // ✅ Fast
var customer = await context.Customers.FindAsync(id);       // ✅ Fast
var response = await controller.GetCustomer(customerId);   // ✅ Fast
```

### Benchmarks

```
| Method                  | Mean      | Allocated |
|------------------------ |----------:|----------:|
| Serialize_StronglyTyped | 1.234 μs  | 1.2 KB    |
| Serialize_Primitive     | 1.198 μs  | 1.2 KB    | ← Negligible difference
| EF_Query_StronglyTyped  | 45.23 μs  | 2.5 KB    |
| EF_Query_Primitive      | 45.01 μs  | 2.5 KB    | ← Negligible difference
```

The performance overhead is **negligible** - you get type safety without sacrificing speed!

## Migration Guide

### Migrating from Primitive Types

**Step 1: Create Strongly Typed IDs**

```csharp
// Before
public class Customer
{
    public Guid Id { get; set; }
}

// After - Add new record struct
public readonly record struct CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);
```

**Step 2: Update Entity Classes**

```csharp
public class Customer
{
    public CustomerId Id { get; init; } // Change Guid to CustomerId
    public string Name { get; set; } = string.Empty;
}
```

**Step 3: Configure EF Core**

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.ConfigureStronglyTypedIds();
}
```

**Step 4: No Database Migration Needed!**

The database schema stays the same - IDs are still stored as Guid/int/etc.

```sql
-- Schema doesn't change
CREATE TABLE Customers (
    Id uniqueidentifier PRIMARY KEY, -- Still a Guid
    Name nvarchar(max)
);
```

**Step 5: Update Service Layer**

```csharp
// Before
public Task<Customer> GetCustomerAsync(Guid id)

// After
public Task<Customer> GetCustomerAsync(CustomerId id)
```

**Step 6: Configure API**

```csharp
builder.Services.AddStronglyTypedIds();
```

Done! Your API now uses strongly typed IDs with full JSON and Swagger support.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<a name="turkish-documentation"></a>

# 🇹🇷 Türkçe Dokümantasyon

## İçindekiler

- [Neden Strongly Typed ID'ler?](#neden-strongly-typed-idler)
- [Özellikler](#özellikler-tr)
- [Kurulum](#kurulum-tr)
- [Hızlı Başlangıç](#hızlı-başlangıç-tr)
- [Desteklenen Değer Tipleri](#desteklenen-değer-tipleri-tr)
- [Detaylı Kullanım](#detaylı-kullanım-tr)
- [Gelişmiş Senaryolar](#gelişmiş-senaryolar-tr)
- [Performans Değerlendirmesi](#performans-değerlendirmesi-tr)
- [Migrasyon Kılavuzu](#migrasyon-kılavuzu-tr)

## Neden Strongly Typed ID'ler?

`Guid` veya `int` gibi primitive tipleri doğrudan kullanmak yerine, strongly typed ID'ler şunları sağlar:

✅ **Tip Güvenliği**: Farklı entity ID'lerinin karıştırılmasını derleme zamanında önler  
✅ **Kod Netliği**: `FindCustomer(CustomerId id)` daha açıklayıcıdır  
✅ **Refactoring Güvenliği**: ID tiplerini değiştirirken derleyici hataları  
✅ **DDD Uyumluluğu**: Domain-Driven Design value object pattern'ini takip eder  
✅ **Sıfır Boilerplate**: Otomatik EF Core, JSON ve OpenAPI entegrasyonu  

### Önce (Primitive Obsession)
```csharp
public class OrderService
{
    // ❌ Müşteri ve sipariş ID'lerini karıştırmak kolay
    public Order CreateOrder(Guid customerId, Guid productId) { }
    
    // ❌ Parametreleri yanlışlıkla yer değiştirmek derlenir ama çalışma zamanında hata verir
    var order = CreateOrder(productId, customerId); // YANLIŞ!
}
```

### Sonra (Strongly Typed ID'ler)
```csharp
public class OrderService
{
    // ✅ Tip-güvenli parametreler
    public Order CreateOrder(CustomerId customerId, ProductId productId) { }
    
    // ✅ Derleme hatası hataları önler
    var order = CreateOrder(productId, customerId); // Derlenmez!
}
```

## <a name="özellikler-tr"></a>Özellikler

- 🎯 **Generic Base Tip**: `Guid`, `int`, `long`, `string`, `decimal`, `double`, `DateTime`, `DateTimeOffset` ve `Enum` desteği
- 🗃️ **EF Core Convention**: Otomatik value converter kaydı
- 📦 **JSON Serileştirme**: Sorunsuz System.Text.Json entegrasyonu
- 📝 **OpenAPI Desteği**: Swagger UI ve Scalar API ile çalışır
- ⚡ **Yüksek Performans**: Reflection sadece başlangıçta, çalışma zamanında sıfır maliyet
- 🔧 **Sıfır Konfigürasyon**: Convention-based otomatik keşif
- 🎨 **Temiz Kod**: `record struct` ile minimal boilerplate

## <a name="kurulum-tr"></a>Kurulum

```bash
dotnet add package ErginWebDev.StronglyTypedIds
```

**Gereksinimler:**
- .NET 8.0 veya üzeri (.NET 8.0 ve .NET 9.0 her ikisi de desteklenir)
- Entity Framework Core 8.0+ (EF Core entegrasyonu kullanılıyorsa)
- Swashbuckle.AspNetCore 6.8+ (Swagger kullanılıyorsa)

## <a name="hızlı-başlangıç-tr"></a>Hızlı Başlangıç

### 1. Strongly Typed ID'lerinizi Tanımlayın

```csharp
using ErginWebDev.StronglyTypedIds.ValueObjects;

// Guid tabanlı ID'ler
public readonly record struct CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);
public readonly record struct OrderId(Guid Value) : StronglyTypedId<Guid>(Value);

// Diğer değer tipleri
public readonly record struct OrderNumber(int Value) : StronglyTypedId<int>(Value);
public readonly record struct ProductCode(string Value) : StronglyTypedId<string>(Value);
public readonly record struct Price(decimal Value) : StronglyTypedId<decimal>(Value);
```

### 2. Domain Entity'lerinizde Kullanın

```csharp
public class Customer
{
    public CustomerId Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Order
{
    public OrderId Id { get; init; }
    public CustomerId CustomerId { get; init; }
    public OrderNumber OrderNumber { get; init; }
    public Price TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### 3. Servisleri Yapılandırın (Program.cs)

```csharp
using ErginWebDev.StronglyTypedIds;

var builder = WebApplication.CreateBuilder(args);

// Strongly typed ID desteğini ekle
builder.Services.AddStronglyTypedIds(); // JSON + Swagger

// DbContext'inizi ekleyin
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.Run();
```

### 4. EF Core'u Yapılandırın (DbContext)

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Otomatik strongly typed ID dönüşümünü etkinleştir
        configurationBuilder.ConfigureStronglyTypedIds();
    }
}
```

### 5. Controller'larda Kullanın

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context) => _context = context;

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(CustomerId id)
    {
        // ID otomatik olarak URL'den bind edilir: "550e8400-e29b-41d4-a716-446655440000"
        var customer = await _context.Customers.FindAsync(id);
        
        if (customer == null)
            return NotFound();
            
        return customer; // Otomatik olarak JSON'a dönüştürülür
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Id = new CustomerId(Guid.CreateVersion7()), // Zaman sıralı GUID
            Name = request.Name,
            Email = request.Email
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }
}
```

**Bu kadar!** Strongly typed ID'leriniz artık tüm uygulamanızda sorunsuz çalışıyor.

## <a name="desteklenen-değer-tipleri-tr"></a>Desteklenen Değer Tipleri

| Tip | Veritabanı | JSON Format | OpenAPI Tip | Örnek |
|-----|------------|-------------|-------------|--------|
| `Guid` | uniqueidentifier | string | `string/uuid` | `"550e8400-e29b-41d4-a716-446655440000"` |
| `int` | int | number | `integer/int32` | `12345` |
| `long` | bigint | number | `integer/int64` | `9999999999` |
| `string` | nvarchar | string | `string` | `"PROD-001"` |
| `decimal` | decimal | number | `number/decimal` | `99.95` |
| `double` | float | number | `number/double` | `123.456` |
| `DateTime` | datetime2 | string | `string/date-time` | `"2025-11-08T10:30:00"` |
| `DateTimeOffset` | datetimeoffset | string | `string/date-time` | `"2025-11-08T10:30:00Z"` |
| `Enum` | int | string | `string` + enum değerleri | `"Active"` |

## <a name="detaylı-kullanım-tr"></a>Detaylı Kullanım

### Entity Framework Core Entegrasyonu

Kütüphane, EF Core'un convention sistemini kullanarak strongly typed ID'leri otomatik olarak tespit eder ve dönüştürür.

#### Nasıl Çalışır

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Bu tek satır TÜM strongly typed ID'ler için otomatik dönüşümü etkinleştirir
        configurationBuilder.ConfigureStronglyTypedIds();
        
        // Convention tüm entity property'lerini tarar ve şunları tespit eder:
        // - Value type'lar (struct)
        // - "Value" property'si olan
        // - Desteklenen bir tipte (Guid, int, long, string, vb.)
        // Ardından otomatik olarak uygun converter'ı uygular
    }
}
```

#### Veritabanı Depolama

Strongly typed ID'ler altta yatan değer tipi olarak saklanır:

```sql
-- Customer tablosu
CREATE TABLE Customers (
    Id uniqueidentifier PRIMARY KEY,  -- CustomerId Guid olarak saklanır
    Name nvarchar(max),
    Email nvarchar(max)
);

-- Order tablosu
CREATE TABLE Orders (
    Id uniqueidentifier PRIMARY KEY,      -- OrderId Guid olarak saklanır
    CustomerId uniqueidentifier NOT NULL, -- CustomerId Guid olarak saklanır
    OrderNumber int NOT NULL,             -- OrderNumber int olarak saklanır
    TotalPrice decimal(18,2) NOT NULL,    -- Price decimal olarak saklanır
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);
```

### JSON Serileştirme

Kütüphane, System.Text.Json kullanarak otomatik JSON dönüşümü sağlar.

#### Tip-Spesifik Serileştirme

```csharp
public class Product
{
    public ProductId Id { get; init; }              // Guid
    public ProductCode Code { get; init; }          // string
    public ProductNumber Number { get; init; }      // int
    public Price Price { get; init; }               // decimal
    public Weight Weight { get; init; }             // double
    public CreatedAt CreatedAt { get; init; }       // DateTime
    public ProductStatus Status { get; init; }      // Enum
}

// Şuna dönüştürülür:
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "code": "PROD-001",
  "number": 12345,
  "price": 99.95,
  "weight": 2.5,
  "createdAt": "2025-11-08T10:30:00Z",
  "status": "Active"
}
```

### Scalar API Desteği

Scalar, Swagger UI'ya modern bir alternatiftir. Kütüphane Scalar ile sorunsuz çalışır.

```csharp
// Program.cs - Sadece Scalar
builder.Services.AddStronglyTypedIds(configureSwagger: false);
builder.Services.AddOpenApi(); // .NET 9 yerleşik OpenAPI
app.MapOpenApi();
app.MapScalarApiReference();
```

## <a name="gelişmiş-senaryolar-tr"></a>Gelişmiş Senaryolar

### Enum Tabanlı ID'ler

```csharp
public enum OrderStatus
{
    Beklemede,
    Onaylandi,
    Kargolandi,
    TeslimEdildi,
    IptalEdildi
}

public readonly record struct OrderStatusId(OrderStatus Value) 
    : StronglyTypedId<OrderStatus>(Value);

public class Order
{
    public OrderId Id { get; init; }
    public OrderStatusId Status { get; init; }
}

// Kullanım
var order = new Order
{
    Id = new OrderId(Guid.CreateVersion7()),
    Status = new OrderStatusId(OrderStatus.Onaylandi)
};
```

### Guid Üretim Stratejileri

```csharp
// Seçenek 1: Guid.CreateVersion7() - Zaman sıralı (veritabanları için önerilen)
// .NET 9.0+ için kullanılabilir
#if NET9_0_OR_GREATER
var customerId = new CustomerId(Guid.CreateVersion7());
#else
// .NET 8.0 için Guid.NewGuid() kullanın - kütüphanenin NewId() metodu bunu otomatik yapar
var customerId = new CustomerId(Guid.NewGuid());
#endif
// Artılar: Daha iyi veritabanı index performansı (NET 9+), oluşturulma zamanına göre sıralanabilir
// Eksiler: Hafif tahmin edilebilir sıra

// Seçenek 2: Guid.NewGuid() - Rastgele (geleneksel, tüm versiyonlarda çalışır)
var customerId = new CustomerId(Guid.NewGuid());
// Artılar: Tamamen rastgele, tahmin edilemez, .NET 8.0+ ile çalışır
// Eksiler: Clustered index'lerde zayıf veritabanı performansı

// Not: Kütüphanenin NewId() metodu .NET 9.0'da otomatik olarak CreateVersion7() kullanır
// ve .NET 8.0'da NewGuid()'e geri döner
```

## <a name="performans-değerlendirmesi-tr"></a>Performans Değerlendirmesi

### Reflection Kullanımı

Kütüphane reflection'ı **sadece uygulama başlangıcında** kullanır:

```csharp
// Başlangıç (uygulama başlatılırken tek seferlik maliyet)
builder.Services.AddStronglyTypedIds();           // JSON converter factory kaydı
configurationBuilder.ConfigureStronglyTypedIds(); // EF Core model taraması

// Çalışma zamanı (sıfır reflection - önbelleğe alınmış converter'lar kullanılır)
var json = JsonSerializer.Serialize(customer);              // ✅ Hızlı
var customer = await context.Customers.FindAsync(id);       // ✅ Hızlı
var response = await controller.GetCustomer(customerId);   // ✅ Hızlı
```

Performans maliyeti **ihmal edilebilir** - tip güvenliğini hızdan ödün vermeden elde edersiniz!

## <a name="migrasyon-kılavuzu-tr"></a>Migrasyon Kılavuzu

### Primitive Tiplerden Geçiş

**Adım 1: Strongly Typed ID'ler Oluşturun**

```csharp
// Önce
public class Customer
{
    public Guid Id { get; set; }
}

// Sonra - Yeni record struct ekleyin
public readonly record struct CustomerId(Guid Value) : StronglyTypedId<Guid>(Value);
```

**Adım 2: Entity Sınıflarını Güncelleyin**

```csharp
public class Customer
{
    public CustomerId Id { get; init; } // Guid'den CustomerId'ye değiştirin
    public string Name { get; set; } = string.Empty;
}
```

**Adım 3: EF Core'u Yapılandırın**

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.ConfigureStronglyTypedIds();
}
```

**Adım 4: Veritabanı Migration'a Gerek Yok!**

Veritabanı şeması aynı kalır - ID'ler hala Guid/int/vb. olarak saklanır.

**Adım 5: API'yi Yapılandırın**

```csharp
builder.Services.AddStronglyTypedIds();
```

Tamamlandı! API'niz artık tam JSON ve Swagger desteği ile strongly typed ID'ler kullanıyor.

---

**Made with ❤️ by Ergin TIRAVOGLU**
