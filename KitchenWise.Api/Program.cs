using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using KitchenWise.Api.Data;
using KitchenWise.Api.Repositories;
using KitchenWise.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System;

namespace KitchenWise.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("🍽️ KitchenWise API Starting...");
            Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine("===========================================");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Configure services
                ConfigureServices(builder.Services, builder.Configuration);

                var app = builder.Build();

                // 🔧 DATABASE INITIALIZATION - RUNS AUTOMATICALLY
                // ===============================================
                await InitializeDatabaseAsync(app);

                // Configure pipeline
                ConfigurePipeline(app);

                Console.WriteLine("KitchenWise API configured successfully");
                Console.WriteLine($"Running in {app.Environment.EnvironmentName} mode");

                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start KitchenWise API: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Configure dependency injection services
        /// </summary>
        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Configuring services...");

            // Add controllers
            services.AddControllers();

            // Add API documentation
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "KitchenWise API",
                    Version = "v1",
                    Description = "Smart kitchen organization API for managing pantries and recipes"
                });
            });

            // Add CORS for desktop app communication
            services.AddCors(options =>
            {
                options.AddPolicy("AllowDesktopApp", policy =>
                {
                    policy.WithOrigins("http://localhost", "https://localhost")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // 🔧 AZURE CONFIGURATION SECTION - ADD YOUR DETAILS HERE
            // =====================================================

            // Database Configuration - Azure SQL Database
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // ✅ AZURE SQL CONNECTION STRING CONFIGURED
                // Using your kitchenwiseuser database
                connectionString = "Server=tcp:kitchenwise.database.windows.net,1433;" +
                                 "Initial Catalog=kitchenwiseuser;" +
                                 "Persist Security Info=False;" +
                                 "User ID=s2yeshwa;" +
                                 "Password=Suspense!01;" +
                                 "MultipleActiveResultSets=False;" +
                                 "Encrypt=True;" +
                                 "TrustServerCertificate=False;" +
                                 "Connection Timeout=30;";

                Console.WriteLine("✅ Using configured Azure SQL connection string for kitchenwiseuser database");
            }

            // Add Entity Framework with SQL Server
            services.AddDbContext<KitchenWiseDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });

                // Enable detailed errors in development
                if (configuration.GetValue<bool>("DetailedErrors"))
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });

            // Add Repository pattern
            services.AddScoped<IPantryRepository, PantryRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Add OpenAI service
            services.AddScoped<IOpenAIService, OpenAIServiceFixed>();

            // ✅ AUTH0 JWT AUTHENTICATION CONFIGURATION
            // ========================================
            var auth0Domain = configuration["Auth0:Domain"];
            var auth0Audience = configuration["Auth0:Audience"];

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://{auth0Domain}/";
                    options.Audience = auth0Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://{auth0Domain}/",
                        ValidateAudience = true,
                        ValidAudience = auth0Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });

            services.AddAuthorization();

            Console.WriteLine($"✅ Auth0 JWT authentication configured: {auth0Domain}");

            // Health checks
            services.AddHealthChecks();

            Console.WriteLine("Services configured successfully");
        }

        /// <summary>
        /// Configure the HTTP request pipeline
        /// </summary>
        private static void ConfigurePipeline(WebApplication app)
        {
            Console.WriteLine("Configuring request pipeline...");

            // Development middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KitchenWise API v1");
                    c.RoutePrefix = "swagger"; // Makes Swagger UI available at /swagger
                });
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Security and routing middleware
            app.UseHttpsRedirection();
            app.UseCors("AllowDesktopApp");

            // ✅ AUTH0 AUTHENTICATION & AUTHORIZATION MIDDLEWARE
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers
            app.MapControllers();

            // Health check endpoint
            app.MapHealthChecks("/health");

            // Welcome endpoint
            app.MapGet("/", () => new
            {
                Service = "KitchenWise API",
                Version = "1.0.0",
                Status = "Running",
                Timestamp = DateTime.UtcNow,
                Documentation = "/swagger"
            });

            // API info endpoint
            app.MapGet("/api", () => new
            {
                Message = "Welcome to KitchenWise API",
                Version = "1.0.0",
                Endpoints = new[]
                {
                    "GET /api/users - Get all users",
                    "GET /api/users/{id} - Get user by ID",
                    "POST /api/users - Create new user",
                    "GET /health - Health check",
                    "GET /swagger - API documentation"
                }
            });

            Console.WriteLine("Request pipeline configured successfully");
        }

        /// <summary>
        /// Initialize database with automatic migrations
        /// </summary>
        private static async Task InitializeDatabaseAsync(WebApplication app)
        {
            Console.WriteLine("Initializing database...");

            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<KitchenWiseDbContext>();

                    // 🔧 AUTOMATIC DATABASE MIGRATION
                    // ===============================
                    Console.WriteLine("Checking for pending database migrations...");

                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"Applying {pendingMigrations.Count()} pending migrations...");
                        await context.Database.MigrateAsync();
                        Console.WriteLine("✅ Database migrations applied successfully!");
                    }
                    else
                    {
                        Console.WriteLine("✅ Database is up to date - no migrations needed");
                    }

                    // Verify database connection
                    var canConnect = await context.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        Console.WriteLine("✅ Database connection verified successfully!");

                        // Log some stats
                        var userCount = await context.Users.CountAsync();
                        var pantryItemCount = await context.PantryItems.CountAsync();
                        Console.WriteLine($"📊 Database contains: {userCount} users, {pantryItemCount} pantry items");
                    }
                    else
                    {
                        Console.WriteLine("❌ Database connection failed!");
                        throw new InvalidOperationException("Cannot connect to database");
                    }
                }

                Console.WriteLine("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
                Console.WriteLine("🔧 Please check your Azure SQL connection string and database configuration");
                throw;
            }
        }
    }
}