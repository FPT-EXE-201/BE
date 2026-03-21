using FPT.EXE201.Application;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Infrastructure.AI;
using FPT.EXE201.Infrastructure.Persistence;
using FPT.EXE201.Infrastructure.Repositories;
using FPT.EXE201.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace FPT.EXE201.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // CRITICAL: Disable default claim mapping to keep JWT claim names (like 'sub', 'role') original
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            // Add DbContext with MySQL
            services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mySqlOptions =>
                    {
                        mySqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    });
            });

            // Add repositories here
            #region Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILanguageRepository, LanguageRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Week 5 — Supabase Storage
            services.AddHttpClient<IFileStorageService, SupabaseStorageService>(client =>
            {
                var supabaseUrl = configuration["Supabase:Url"]
                    ?? throw new InvalidOperationException("Supabase:Url is required.");
                var serviceKey = configuration["Supabase:ServiceRoleKey"]
                    ?? throw new InvalidOperationException("Supabase:ServiceRoleKey is required.");

                client.BaseAddress = new Uri($"{supabaseUrl.TrimEnd('/')}/storage/v1/");
                client.DefaultRequestHeaders.Add("apikey", serviceKey);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceKey);
            });

            services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
            services.AddScoped<IOcrService, OcrService>();

            services.AddHttpClient<IAiProvider, GeminiAiProvider>(client =>
            {
                var baseUrl = configuration["AI:Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta/";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["AI:Gemini:TimeoutSeconds"] ?? "60"));
            });

            services.AddHttpClient<IOcrProvider, AzureOcrProvider>(client =>
            {
                var endpoint = configuration["AI:AzureDocumentIntelligence:Endpoint"]
                    ?? throw new InvalidOperationException("AI:AzureDocumentIntelligence:Endpoint is required.");
                client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["AI:AzureDocumentIntelligence:TimeoutSeconds"] ?? "120"));
            });

            services.AddSingleton<IOcrJobQueue, OcrJobQueue>();
            services.AddHostedService<OcrBackgroundService>();
            services.AddSingleton<IMealPlanJobQueue, MealPlanJobQueue>();
            services.AddHostedService<MealPlanBackgroundService>();
            services.AddScoped<IWeightOcrService, WeightOcrService>();
            #endregion

            // Add JWT Authentication
            var jwtSecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            var jwtIssuer = configuration["Jwt:Issuer"] ?? "FPT.EXE201.Api";
            var jwtAudience = configuration["Jwt:Audience"] ?? "FPT.EXE201.Client";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Sub, // Use 'sub' as NameIdentifier
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs/chat")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }
    }
}
