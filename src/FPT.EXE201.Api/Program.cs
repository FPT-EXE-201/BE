using FPT.EXE201.Infrastructure;
using FPT.EXE201.Application;
using FPT.EXE201.Api.Filters;
using FluentValidation.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.OpenApi.Models;

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

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Add Infrastructure services (DbContext, Repositories, JWT Authentication, etc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddApplication();

    // Add Authorization policies
    builder.Services.AddAuthorization();

    // Add services to the container with Global Exception Filter
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    });

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation()
        .AddFluentValidationClientsideAdapters();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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

app.UseAuthentication(); // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

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
