using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WasteCollectionSystem.Data;
using WasteCollectionSystem.Models;
using WasteCollectionSystem.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

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

// Access denied path for role-protected pages
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/AccessDenied";
});

// ✅ Register email sender service
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
// ✅ Register notification service
builder.Services.AddScoped<NotificationService>();
// ✅ Register cart service
builder.Services.AddScoped<CartService>();
builder.Services.AddHttpContextAccessor();
// MTN MoMo payment service (consolidated) via DI abstraction
builder.Services.AddHttpClient<IMomoPaymentService, MtnMomoService>();

// ✅ Enable MVC controllers and Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ✅ API Explorer & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WasteCollectionSystem API", Version = "v1" });
    
    // Add Security Definition for JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ✅ JWT Authentication Setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication()
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});



// ✅ Build the app AFTER registering services
var app = builder.Build();


// Seed roles at startup
using (var scope = app.Services.CreateScope())
{
    // Apply pending EF Core migrations at startup
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Admin", "User"};
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed database from SQL
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await SeedData.SeedFromSqlAsync(db, logger);
}

// ✅ Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ✅ Enable Swagger in Development AND Production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WasteCollectionSystem API v1");
    // To serve SwaggerUI at application root without a subpath, use:
    // c.RoutePrefix = string.Empty;
});



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Map endpoints AFTER app is declared
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.MapRazorPages();

// Notifications API for user area - returns actual notifications
app.MapGet("/api/user/notifications", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var notifications = await ctx.Notifications
        .Where(n => n.UserId == user.Id)
        .OrderByDescending(n => n.CreatedAt)
        .Take(7)
        .Select(n => new
        {
            n.Id,
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt,
            n.Url
        })
        .ToListAsync();

    return Results.Ok(notifications);
}).RequireAuthorization();

// Mark notification as read
app.MapPost("/api/user/notifications/{id}/read", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um, int id) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var notification = await ctx.Notifications
        .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);
    
    if (notification == null)
    {
        return Results.NotFound();
    }

    notification.IsRead = true;
    await ctx.SaveChangesAsync();

    return Results.Ok(new { success = true });
}).RequireAuthorization();

// Mark all notifications as read
app.MapPost("/api/user/notifications/read-all", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var notifications = await ctx.Notifications
        .Where(n => n.UserId == user.Id && !n.IsRead)
        .ToListAsync();
    
    foreach (var notification in notifications)
    {
        notification.IsRead = true;
    }
    
    await ctx.SaveChangesAsync();

    return Results.Ok(new { success = true, count = notifications.Count });
}).RequireAuthorization();

// Get unread count
app.MapGet("/api/user/notifications/unread-count", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var count = await ctx.Notifications
        .CountAsync(n => n.UserId == user.Id && !n.IsRead);

    return Results.Ok(new { count });
}).RequireAuthorization();

// Admin chart data: requests per day for current month
app.MapGet("/api/admin/requests-by-day", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null || user.Role != "Admin") return Results.Unauthorized();

    var today = DateTime.Today;
    var start = new DateTime(today.Year, today.Month, 1);
    var end = start.AddMonths(1);

    var groups = await ctx.WasteRequests
        .Where(r => r.RequestDate >= start && r.RequestDate < end)
        .GroupBy(r => r.RequestDate.Date)
        .Select(g => new { Day = g.Key, Count = g.Count() })
        .OrderBy(x => x.Day)
        .ToListAsync();

    var result = groups.Select(g => new { day = g.Day.ToString("MM-dd"), count = g.Count });
    return Results.Ok(result);
}).RequireAuthorization();

// Admin payments monthly revenue (last 12 months)
app.MapGet("/api/admin/payments-monthly", async (HttpContext http, ApplicationDbContext ctx, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    if (user == null || user.Role != "Admin") return Results.Unauthorized();

    var today = DateTime.Today;
    var start = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
    var end = new DateTime(today.Year, today.Month, 1).AddMonths(1);

    var groups = await ctx.Payments
        .Where(p => p.PaymentStatus == "Paid" && p.PaymentDate >= start && p.PaymentDate < end)
        .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
        .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(x => x.Amount) })
        .OrderBy(x => x.Year).ThenBy(x => x.Month)
        .ToListAsync();

    var result = groups.Select(g => new { month = new DateTime(g.Year, g.Month, 1).ToString("yyyy-MM"), total = g.Total });
    return Results.Ok(result);
}).RequireAuthorization();

// Cart items API endpoint
app.MapGet("/api/cart/items", async (HttpContext http, CartService cartService, UserManager<ApplicationUser> um) =>
{
    var user = await um.GetUserAsync(http.User);
    var items = await cartService.GetCartItemsAsync(user?.Id);
    
    var result = items.Select(r => new
    {
        requestId = r.RequestID,
        wasteType = r.WasteType,
        location = r.Location,
        status = r.Status,
        requestDate = r.RequestDate,
        amount = 10.00 // Default amount, can be calculated based on waste type
    });
    
    return Results.Ok(result);
});

app.Run();

// Expose Program for integration testing
public partial class Program { }
