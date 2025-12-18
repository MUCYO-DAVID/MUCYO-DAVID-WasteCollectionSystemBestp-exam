# Design Pattern Application - Exact Locations

This document shows **exactly where** the Dependency Injection pattern is applied in the codebase with specific file paths and code snippets.

---

## 📍 Location 1: Interface Definition

**File**: `Services/IMomoPaymentService.cs`

**Lines**: 8-13

```csharp
namespace WasteCollectionSystem.Services
{
    /// <summary>
    /// Abstraction for MTN MoMo payment operations (DI-friendly, testable).
    /// </summary>
    public interface IMomoPaymentService
    {
        Task<string> RequestToPayAsync(string phone, decimal amount);
        Task<string> GetTokenAsync();
        Task<MtnMomoService.MomoTransactionResult> GetTransactionStatusAsync(string transactionId);
    }
}
```

**What this shows**: The **abstraction** (interface) that defines the contract.

---

## 📍 Location 2: Concrete Implementation

**File**: `Services/MtnMomoService.cs`

**Lines**: 14-163

```csharp
namespace WasteCollectionSystem.Services
{
    /// <summary>
    /// Production-ready MTN Mobile Money service for Sandbox environment.
    /// Handles authentication, payment requests, and status checking.
    /// </summary>
    public class MtnMomoService : IMomoPaymentService  // ← Implements the interface
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        
        // Constructor receives dependencies (DI in action)
        public MtnMomoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;        // ← Injected dependency
            _configuration = configuration;  // ← Injected dependency
        }
        
        // Implementation of interface methods...
        public async Task<string> RequestToPayAsync(string phone, decimal amount)
        {
            // Implementation code...
        }
    }
}
```

**What this shows**: The **concrete implementation** that implements the interface.

---

## 📍 Location 3: Service Registration (DI Container Setup)

**File**: `Program.cs`

**Line**: 41

```csharp
// MTN MoMo payment service (consolidated) via DI abstraction
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();
```

**What this shows**: 
- Registers `IMomoPaymentService` interface
- Maps it to `MtnMomoService` implementation
- The DI container will automatically provide `MtnMomoService` when `IMomoPaymentService` is requested

**Full context** (Lines 33-42):
```csharp
// ✅ Register email sender service
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
// ✅ Register notification service
builder.Services.AddScoped<NotificationService>();
// ✅ Register cart service
builder.Services.AddScoped<CartService>();
builder.Services.AddHttpContextAccessor();
// MTN MoMo payment service (consolidated) via DI abstraction
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();
```

---

## 📍 Location 4: Dependency Injection in Controller (Primary Usage)

**File**: `Controllers/Api/PaymentsController.cs`

**Lines**: 14-21 (Constructor Injection)

```csharp
public class PaymentsController : ControllerBase
{
    private readonly IMomoPaymentService _momoService;  // ← Interface, not concrete class
    private readonly UserManager<ApplicationUser> _userManager;

    // Constructor Injection - DI container automatically provides implementations
    public PaymentsController(IMomoPaymentService momoService, UserManager<ApplicationUser> userManager)
    {
        _momoService = momoService;      // ← Injected by DI container
        _userManager = userManager;      // ← Injected by DI container
    }
```

**Lines**: 23-40 (Usage of injected service)

```csharp
    [HttpPost("pay")]
    public async Task<IActionResult> PayWithMoMo([FromBody] PaymentRequestDto model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        try 
        {
            // Using the injected service - doesn't know it's MtnMomoService
            var transactionId = await _momoService.RequestToPayAsync(model.PhoneNumber, model.Amount);
            
            return Ok(new { message = "Payment initiated successfully", transactionId = transactionId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Payment failed", error = ex.Message });
        }
    }
```

**What this shows**: 
- Controller **depends on interface** (`IMomoPaymentService`), not concrete class
- Dependencies are **injected via constructor**
- Controller doesn't create `MtnMomoService` - DI container does it automatically

---

## 📍 Location 5: Dependency Injection in Razor Pages

**File**: `Pages/Payment/Payment.cshtml.cs`

**Example code** (showing pattern):
```csharp
public class PaymentModel : PageModel
{
    private readonly IMomoPaymentService _momoService;  // ← Interface dependency
    private readonly ApplicationDbContext _context;

    // Constructor Injection
    public PaymentModel(IMomoPaymentService momoService, ApplicationDbContext context)
    {
        _momoService = momoService;  // ← Injected by DI
        _context = context;          // ← Injected by DI
    }
    
    // Uses injected service
    public async Task OnPostAsync()
    {
        var transactionId = await _momoService.RequestToPayAsync(phone, amount);
        // ...
    }
}
```

---

## 📍 Location 6: Notification Service (Another Example)

**File**: `Services/NotificationService.cs`

**Lines**: 8-17

```csharp
public class NotificationService
{
    private readonly ApplicationDbContext _context;      // ← Injected dependency
    private readonly IEmailSender _emailSender;          // ← Injected dependency (interface)

    // Constructor Injection
    public NotificationService(ApplicationDbContext context, IEmailSender emailSender)
    {
        _context = context;        // ← DI container provides ApplicationDbContext
        _emailSender = emailSender; // ← DI container provides SmtpEmailSender
    }
```

**Registration in Program.cs** (Line 36):
```csharp
builder.Services.AddScoped<NotificationService>();
```

**Usage example** (in any page/controller):
```csharp
public class SomePageModel : PageModel
{
    private readonly NotificationService _notificationService;  // ← Injected

    public SomePageModel(NotificationService notificationService)
    {
        _notificationService = notificationService;  // ← DI container provides it
    }
}
```

---

## 📍 Location 7: Cart Service (Another Example)

**File**: `Services/CartService.cs`

**Lines**: 7-16

```csharp
public class CartService
{
    private readonly ApplicationDbContext _context;           // ← Injected
    private readonly IHttpContextAccessor _httpContextAccessor; // ← Injected

    // Constructor Injection
    public CartService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;                  // ← DI container provides
        _httpContextAccessor = httpContextAccessor; // ← DI container provides
    }
```

**Registration in Program.cs** (Line 38):
```csharp
builder.Services.AddScoped<CartService>();
```

---

## 📍 Location 8: Email Service (Interface Implementation)

**File**: `SmtpEmailSender.cs`

**Interface**: `IEmailSender` (from ASP.NET Core Identity)

**Implementation**:
```csharp
public class SmtpEmailSender : IEmailSender  // ← Implements interface
{
    // Implementation details...
}
```

**Registration in Program.cs** (Line 34):
```csharp
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
```

**Usage in NotificationService** (Line 11):
```csharp
private readonly IEmailSender _emailSender;  // ← Depends on interface
```

---

## 📍 Location 9: Database Context Injection

**File**: `Data/ApplicationDbContext.cs`

**Lines**: 7-12

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Constructor Injection - receives DbContextOptions
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
```

**Registration in Program.cs** (Lines 16-17):
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Usage throughout application**:
- `NotificationService` receives `ApplicationDbContext` via constructor
- `CartService` receives `ApplicationDbContext` via constructor
- All controllers/pages can receive it via constructor injection

---

## 📍 Location 10: Program.cs - Complete DI Registration

**File**: `Program.cs`

**Lines**: 15-42 (All service registrations)

```csharp
// ✅ Register DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Identity setup (using ApplicationUser instead of IdentityUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ✅ Register email sender service
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// ✅ Register notification service
builder.Services.AddScoped<NotificationService>();

// ✅ Register cart service
builder.Services.AddScoped<CartService>();

builder.Services.AddHttpContextAccessor();

// MTN MoMo payment service (consolidated) via DI abstraction
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();
```

**What this shows**: All services are registered with the DI container here.

---

## 📍 Location 11: Usage in API Endpoints

**File**: `Program.cs`

**Lines**: 291-307 (Minimal API endpoint using DI)

```csharp
// Cart items API endpoint
app.MapGet("/api/cart/items", async (HttpContext http, CartService cartService, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    var items = await cartService.GetCartItemsAsync(user?.Id);  // ← Using injected service
    
    var result = items.Select(r => new
    {
        requestId = r.RequestID,
        wasteType = r.WasteType,
        location = r.Location,
        status = r.Status,
        requestDate = r.RequestDate,
        amount = 10.00
    });
    
    return Results.Ok(result);
});
```

**What this shows**: 
- `CartService` is automatically injected into the endpoint
- `UserManager<ApplicationUser>` is also injected
- No manual instantiation needed

---

## 📍 Location 12: Usage in Admin Pages

**File**: `Pages/Admin/Dashboard.cshtml.cs`

**Example pattern** (typical for all admin pages):
```csharp
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;      // ← Injected
    private readonly NotificationService _notificationService; // ← Injected

    public DashboardModel(ApplicationDbContext context, NotificationService notificationService)
    {
        _context = context;                    // ← DI provides
        _notificationService = notificationService; // ← DI provides
    }
}
```

---

## 📍 Location 13: Usage in User Pages

**File**: `Pages/User/UserDashboard.cshtml.cs`

**Example pattern**:
```csharp
public class UserDashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly CartService _cartService;  // ← Injected service

    public UserDashboardModel(ApplicationDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;  // ← DI container provides
    }
    
    public async Task OnGetAsync()
    {
        var cartCount = await _cartService.GetCartCountAsync(UserId);  // ← Using injected service
        // ...
    }
}
```

---

## 📊 Summary: Where DI Pattern is Applied

| Location | File | Lines | What It Shows |
|----------|------|-------|---------------|
| **Interface** | `Services/IMomoPaymentService.cs` | 8-13 | Abstraction definition |
| **Implementation** | `Services/MtnMomoService.cs` | 14-163 | Concrete implementation |
| **Registration** | `Program.cs` | 41 | DI container setup |
| **Controller Usage** | `Controllers/Api/PaymentsController.cs` | 14-40 | Constructor injection |
| **Notification Service** | `Services/NotificationService.cs` | 8-17 | Service receiving dependencies |
| **Cart Service** | `Services/CartService.cs` | 7-16 | Service receiving dependencies |
| **Email Service** | `SmtpEmailSender.cs` | - | Interface implementation |
| **Database Context** | `Data/ApplicationDbContext.cs` | 7-12 | Context injection |
| **All Services** | `Program.cs` | 15-42 | Complete DI registration |
| **API Endpoints** | `Program.cs` | 291-307 | Automatic injection in endpoints |
| **Razor Pages** | Various `.cshtml.cs` files | - | Constructor injection pattern |

---

## 🔍 How to Verify for Examiner

### Step 1: Check Interface
```powershell
# Open file
code Services/IMomoPaymentService.cs
# Or
notepad Services/IMomoPaymentService.cs
```

### Step 2: Check Implementation
```powershell
code Services/MtnMomoService.cs
```

### Step 3: Check Registration
```powershell
code Program.cs
# Go to line 41
```

### Step 4: Check Usage
```powershell
code Controllers/Api/PaymentsController.cs
# See constructor at lines 17-21
```

### Step 5: Run the Application
```powershell
dotnet run
# The DI container automatically wires everything together
```

---

## ✅ Evidence Checklist

- [x] Interface defined: `Services/IMomoPaymentService.cs`
- [x] Implementation created: `Services/MtnMomoService.cs`
- [x] Service registered: `Program.cs` line 41
- [x] Used in controller: `Controllers/Api/PaymentsController.cs`
- [x] Used in services: `NotificationService.cs`, `CartService.cs`
- [x] Used in pages: Various `.cshtml.cs` files
- [x] Documented: `DESIGN_PATTERN.md`, `DESIGN_PATTERN_SUMMARY.md`, this file

---

**Status**: ✅ Dependency Injection pattern is applied throughout the application at the locations shown above.
