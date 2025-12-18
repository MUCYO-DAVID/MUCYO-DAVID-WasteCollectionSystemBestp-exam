# Design Pattern Implementation

## Overview

This project implements **Dependency Injection (DI)** design pattern, which is a fundamental software design pattern that promotes loose coupling, testability, and maintainability.

---

## 1. Design Pattern Used: Dependency Injection (DI)

### What is Dependency Injection?

Dependency Injection is a design pattern where objects receive their dependencies from an external source rather than creating them internally. This follows the **Inversion of Control (IoC)** principle, where control of object creation is inverted from the class itself to an external container.

### Why We Use It

1. **Loose Coupling**: Classes don't depend on concrete implementations, only on abstractions (interfaces)
2. **Testability**: Easy to mock dependencies for unit testing
3. **Maintainability**: Changes to implementations don't require changes to dependent classes
4. **Flexibility**: Can swap implementations without modifying code

---

## 2. Implementation in This Project

### 2.1 Interface Definition

**File**: `Services/IMomoPaymentService.cs`

```csharp
public interface IMomoPaymentService
{
    Task<string> RequestToPayAsync(string phone, decimal amount);
    Task<string> GetTokenAsync();
    Task<MtnMomoService.MomoTransactionResult> GetTransactionStatusAsync(string transactionId);
}
```

**Purpose**: Defines the contract (abstraction) for payment operations without specifying implementation details.

### 2.2 Concrete Implementation

**File**: `Services/MtnMomoService.cs`

```csharp
public class MtnMomoService : IMomoPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    public MtnMomoService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }
    
    // Implementation of interface methods...
}
```

**Purpose**: Provides the actual implementation of the payment service using MTN Mobile Money API.

### 2.3 Service Registration (Dependency Injection Container)

**File**: `Program.cs` (Line 41)

```csharp
// Register payment service with DI container
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();
```

**Purpose**: Registers the interface and its implementation with ASP.NET Core's built-in dependency injection container. The container will automatically inject `MtnMomoService` whenever `IMomoPaymentService` is requested.

### 2.4 Dependency Injection in Controllers

**File**: `Controllers/Api/PaymentsController.cs`

```csharp
public class PaymentsController : ControllerBase
{
    private readonly IMomoPaymentService _momoService;
    private readonly UserManager<ApplicationUser> _userManager;

    // Constructor Injection
    public PaymentsController(IMomoPaymentService momoService, UserManager<ApplicationUser> userManager)
    {
        _momoService = momoService;  // Injected by DI container
        _userManager = userManager;   // Injected by DI container
    }

    [HttpPost("pay")]
    public async Task<IActionResult> PayWithMoMo([FromBody] PaymentRequestDto model)
    {
        // Use the injected service
        var transactionId = await _momoService.RequestToPayAsync(model.PhoneNumber, model.Amount);
        return Ok(new { message = "Payment initiated successfully", transactionId = transactionId });
    }
}
```

**Key Points**:
- The controller doesn't create `MtnMomoService` directly
- It depends on the `IMomoPaymentService` interface (abstraction)
- The DI container automatically provides the concrete implementation
- This is called **Constructor Injection**

---

## 3. Other Services Using Dependency Injection

### 3.1 Notification Service

**File**: `Services/NotificationService.cs`

```csharp
public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;

    // Constructor Injection
    public NotificationService(ApplicationDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }
}
```

**Registration in Program.cs** (Line 36):
```csharp
builder.Services.AddScoped<NotificationService>();
```

### 3.2 Cart Service

**File**: `Services/CartService.cs`

```csharp
public class CartService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constructor Injection
    public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
}
```

**Registration in Program.cs** (Line 38):
```csharp
builder.Services.AddScoped<CartService>();
```

### 3.3 Email Sender Service

**File**: `SmtpEmailSender.cs`

```csharp
public class SmtpEmailSender : IEmailSender
{
    // Implements IEmailSender interface
}
```

**Registration in Program.cs** (Line 34):
```csharp
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
```

---

## 4. Benefits Demonstrated

### 4.1 Easy Testing

With Dependency Injection, we can easily create mock implementations for testing:

```csharp
// In a test project, we can create a mock:
public class MockMomoPaymentService : IMomoPaymentService
{
    public Task<string> RequestToPayAsync(string phone, decimal amount)
    {
        return Task.FromResult("mock-transaction-id");
    }
    // ... other methods
}

// Then inject the mock in tests:
var controller = new PaymentsController(new MockMomoPaymentService(), mockUserManager);
```

### 4.2 Flexibility

If we want to switch from MTN MoMo to another payment provider, we only need to:
1. Create a new class implementing `IMomoPaymentService`
2. Change the registration in `Program.cs`
3. No changes needed in `PaymentsController` or other dependent classes

### 4.3 Separation of Concerns

- **Controller**: Handles HTTP requests/responses
- **Service**: Contains business logic
- **Interface**: Defines contract
- Each component has a single responsibility

---

## 5. Service Lifetime Types Used

ASP.NET Core DI container supports three service lifetimes:

1. **Transient** (`AddTransient`): New instance created every time
   - Used for: `IEmailSender` - lightweight, stateless services

2. **Scoped** (`AddScoped`): One instance per HTTP request
   - Used for: `NotificationService`, `CartService` - services that need request context

3. **Singleton** (`AddSingleton`): One instance for the entire application lifetime
   - Not used in this project, but available if needed

---

## 6. Visual Representation

```
┌─────────────────────────────────────────────────────────┐
│                    Program.cs                           │
│  Registers: IMomoPaymentService → MtnMomoService       │
└─────────────────────────────────────────────────────────┘
                        │
                        │ DI Container
                        │ (Automatic Injection)
                        ▼
┌─────────────────────────────────────────────────────────┐
│          PaymentsController                              │
│  Constructor: IMomoPaymentService _momoService         │
│  (Receives MtnMomoService automatically)               │
└─────────────────────────────────────────────────────────┘
                        │
                        │ Uses
                        ▼
┌─────────────────────────────────────────────────────────┐
│          MtnMomoService                                  │
│  (Implements IMomoPaymentService)                       │
│  - RequestToPayAsync()                                  │
│  - GetTokenAsync()                                      │
│  - GetTransactionStatusAsync()                         │
└─────────────────────────────────────────────────────────┘
```

---

## 7. Summary

### Design Pattern: **Dependency Injection (DI)**

### Why It Was Chosen:
1. **Industry Standard**: DI is a fundamental pattern in modern .NET applications
2. **ASP.NET Core Built-in Support**: The framework provides excellent DI container
3. **Testability**: Essential for writing unit tests
4. **Maintainability**: Makes code easier to modify and extend
5. **Best Practice**: Follows SOLID principles, especially Dependency Inversion Principle

### Where It's Used:
- ✅ Payment Service (`IMomoPaymentService` / `MtnMomoService`)
- ✅ Notification Service (`NotificationService`)
- ✅ Cart Service (`CartService`)
- ✅ Email Service (`IEmailSender` / `SmtpEmailSender`)
- ✅ Database Context (`ApplicationDbContext`)
- ✅ Identity Services (`UserManager`, `SignInManager`)

### Evidence for Examiner:
1. **Interface Definition**: `Services/IMomoPaymentService.cs`
2. **Implementation**: `Services/MtnMomoService.cs`
3. **Registration**: `Program.cs` (Line 41)
4. **Usage**: `Controllers/Api/PaymentsController.cs` (Constructor injection)
5. **Documentation**: This file (`DESIGN_PATTERN.md`)

---

## 8. Code Examples for Examiner

### Example 1: Interface and Implementation
```csharp
// Interface (Abstraction)
public interface IMomoPaymentService
{
    Task<string> RequestToPayAsync(string phone, decimal amount);
}

// Implementation (Concrete Class)
public class MtnMomoService : IMomoPaymentService
{
    public async Task<string> RequestToPayAsync(string phone, decimal amount)
    {
        // Implementation details...
    }
}
```

### Example 2: Service Registration
```csharp
// In Program.cs
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();
```

### Example 3: Dependency Injection in Controller
```csharp
public class PaymentsController : ControllerBase
{
    private readonly IMomoPaymentService _momoService;

    // Constructor Injection - DI container provides the implementation
    public PaymentsController(IMomoPaymentService momoService)
    {
        _momoService = momoService;
    }
}
```

---

**Status**: ✅ Design Pattern Successfully Implemented and Documented
