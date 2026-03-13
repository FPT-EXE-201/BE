using FPT.EXE201.Infrastructure;
using FPT.EXE201.Application;
using FPT.EXE201.Api.Filters;
using FPT.EXE201.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.OpenApi.Models;
using DotNetEnv;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "FPT.EXE201.Api")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Month,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        formatter: new CompactJsonFormatter(),
        path: "logs/log-json-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 1_073_741_824) // 1GB
    .CreateLogger();

try
{
    Log.Information("Starting FPT.EXE201 API application...");

    // Load .env file if exists (local dev) — environment variables override appsettings.json
    // Search order: CWD → solution root (for IDE runs where CWD = src/Api/)
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath))
    {
        // When IDE sets CWD to project folder, walk up to find solution root .env
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir?.Parent != null)
        {
            var candidate = Path.Combine(dir.Parent.FullName, ".env");
            if (File.Exists(candidate)) { envPath = candidate; break; }
            dir = dir.Parent;
        }
    }
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        Log.Information("Loaded environment variables from {EnvPath}", envPath);
    }

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Add Infrastructure services (DbContext, Repositories, JWT Authentication, etc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddApplication();

    // Add Authorization
    builder.Services.AddAuthorization();

    // ========================
    // CORS Configuration
    // ========================
    builder.Services.AddCors(options =>
    {
        // Development Mode - Allow All Origins (for testing)
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });

        // Production Mode - Specific Domains (Commented - uncomment when you have domain)
        /*
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(
                    "https://yourdomain.com",           // Your production frontend
                    "https://www.yourdomain.com",       // WWW version
                    "https://admin.yourdomain.com",     // Admin panel
                    "https://api.yourdomain.com"        // API domain
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();  // Enable if using cookies/authentication
        });
        */

        // Mobile App - If you have mobile app
        /*
        options.AddPolicy("MobileApp", policy =>
        {
            policy.WithOrigins(
                    "capacitor://localhost",     // Capacitor iOS/Android
                    "ionic://localhost",         // Ionic
                    "http://localhost"           // React Native
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
        */
    });

    // Add services to the container with Global Exception Filter
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
        options.Filters.Add<ValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings instead of numbers (e.g., "Active" instead of 1)
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        
        // Optional: Use camelCase for enum values (e.g., "active" instead of "Active")
        // options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation()
        .AddFluentValidationClientsideAdapters();

    // Suppress [ApiController] auto 400 ProblemDetails — let GlobalExceptionFilter handle validation errors
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Cấu hình thông tin API chuyên nghiệp
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1.0",
        Title = "FPT EXE201 API",
        Description = "RESTful API for FPT EXE201 Enterprise Application Development Project. " +
                      "This API provides comprehensive endpoints for system management and integration. " +
                      "Features include: Authentication & Authorization, CRUD Operations, Third-party Integration, Pagination & Filtering.",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "FPT EXE201 Development Team",
            Email = "dev.exe201@fpt.edu.vn",
            Url = new Uri("https://fpt.edu.vn")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Cấu hình XML comments để hiển thị documentation chi tiết
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // Cấu hình JWT Bearer Authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

    // Tùy chỉnh Schema IDs để tránh xung đột
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // Sắp xếp actions theo alphabetical order
    options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath}");
});

var app = builder.Build();

var isSwaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");

// Configure the HTTP request pipeline.
if (isSwaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FPT EXE201 API v1.0");
        options.DocumentTitle = "FPT EXE201 API Documentation";
        
        // Cải thiện UX - giữ nguyên layout và màu sắc mặc định
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
    });
}

// Add Serilog Request Logging (logs all HTTP requests with detailed info)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        
        // Add user info if authenticated
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value 
                ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            diagnosticContext.Set("UserEmail", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value);
        }
    };
});

app.UseHttpsRedirection();

// Enable static files serving
app.UseStaticFiles();

// Enable CORS (must be before Authentication/Authorization)
app.UseCors("AllowAll");  // Development mode - allow all origins
// app.UseCors("Production");  // Uncomment for production with specific domains

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapControllers();

// Apply pending migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Auto-apply pending migrations on startup
        // EF Core tracks applied migrations in __EFMigrationsHistory table,
        // so only NEW migrations will be executed — safe for shared DB
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            Log.Information("Applying {Count} pending migration(s): {Migrations}",
                pendingMigrations.Count(), string.Join(", ", pendingMigrations));
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            Log.Information("Database is up to date — no pending migrations");
        }

        await DatabaseSeeder.SeedAsync(context);
        Log.Information("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating/seeding the database");
    }
}

Log.Information("FPT.EXE201 API started successfully");
app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
