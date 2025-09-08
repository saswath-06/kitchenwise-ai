using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenWise.Api.Data
{
    /// <summary>
    /// Entity Framework Database Context for KitchenWise
    /// Handles all database operations with Azure SQL Database integration
    /// </summary>
    public class KitchenWiseDbContext : DbContext
    {
        public KitchenWiseDbContext(DbContextOptions<KitchenWiseDbContext> options) : base(options)
        {
        }

        #region DbSets (Database Tables)

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<PantryItem> PantryItems { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<UserFavoriteRecipe> UserFavoriteRecipes { get; set; } = null!;
        public DbSet<CookingSession> CookingSessions { get; set; } = null!;

        #endregion

        #region Model Configuration

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Auth0UserId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.Auth0UserId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PreferredCuisines).HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                );
            });

            // Configure PantryItem entity
            modelBuilder.Entity<PantryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Quantity).HasPrecision(18, 2);
                
                // Foreign key relationship
                entity.HasOne(e => e.User)
                      .WithMany(u => u.PantryItems)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Recipe entity
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Cuisine).HasMaxLength(50);
                entity.Property(e => e.DifficultyLevel).HasMaxLength(20);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                
                // JSON conversion for arrays
                entity.Property(e => e.Ingredients).HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries)
                );
                entity.Property(e => e.Instructions).HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries)
                );
                entity.Property(e => e.GeneratedWith).HasConversion(
                    v => v != null ? string.Join(',', v) : null,
                    v => v != null ? v.Split(',', StringSplitOptions.RemoveEmptyEntries) : null
                );

                // Owned entity for nutrition
                entity.OwnsOne(e => e.Nutrition, nutrition =>
                {
                    nutrition.Property(n => n.Calories).HasPrecision(18, 2);
                    nutrition.Property(n => n.Protein).HasPrecision(18, 2);
                    nutrition.Property(n => n.Carbs).HasPrecision(18, 2);
                    nutrition.Property(n => n.Fat).HasPrecision(18, 2);
                    nutrition.Property(n => n.Fiber).HasPrecision(18, 2);
                });
            });

            // Configure UserFavoriteRecipe many-to-many relationship
            modelBuilder.Entity<UserFavoriteRecipe>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RecipeId });
                
                entity.HasOne(e => e.User)
                      .WithMany(u => u.FavoriteRecipes)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Recipe)
                      .WithMany()
                      .HasForeignKey(e => e.RecipeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CookingSession entity
            modelBuilder.Entity<CookingSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RecipeName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.IngredientsUsed).HasConversion(
                    v => string.Join('|', v),
                    v => v.Split('|', StringSplitOptions.RemoveEmptyEntries)
                );
                
                entity.HasOne(e => e.User)
                      .WithMany(u => u.CookingSessions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Recipe)
                      .WithMany()
                      .HasForeignKey(e => e.RecipeId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        #endregion

        #region Seed Data

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed demo user
            var demoUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = demoUserId,
                Auth0UserId = "auth0|mock123456",
                Email = "demo@kitchenwise.com",
                Name = "Demo User",
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                LastLoginAt = DateTime.UtcNow,
                PreferredCuisines = new[] { "Italian", "Mexican", "Asian" }
            });

            // Seed demo pantry items
            modelBuilder.Entity<PantryItem>().HasData(
                new PantryItem
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    UserId = demoUserId,
                    Name = "Tomatoes",
                    Quantity = 5.0,
                    Unit = "piece",
                    Category = "Vegetables",
                    AddedDate = DateTime.UtcNow.AddDays(-2),
                    ExpiryDate = DateTime.UtcNow.AddDays(5)
                },
                new PantryItem
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    UserId = demoUserId,
                    Name = "Ground Beef",
                    Quantity = 2.0,
                    Unit = "lb",
                    Category = "Meat",
                    AddedDate = DateTime.UtcNow.AddDays(-1),
                    ExpiryDate = DateTime.UtcNow.AddDays(3)
                },
                new PantryItem
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    UserId = demoUserId,
                    Name = "Basmati Rice",
                    Quantity = 3.0,
                    Unit = "cup",
                    Category = "Grains",
                    AddedDate = DateTime.UtcNow.AddDays(-3),
                    ExpiryDate = null
                }
            );
        }

        #endregion
    }

    #region Entity Models

    /// <summary>
    /// User entity representing application users
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string Auth0UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string[] PreferredCuisines { get; set; } = Array.Empty<string>();

        // Navigation properties
        public virtual ICollection<PantryItem> PantryItems { get; set; } = new List<PantryItem>();
        public virtual ICollection<UserFavoriteRecipe> FavoriteRecipes { get; set; } = new List<UserFavoriteRecipe>();
        public virtual ICollection<CookingSession> CookingSessions { get; set; } = new List<CookingSession>();

        // Computed property
        [NotMapped]
        public int PantryItemCount => PantryItems?.Count ?? 0;
    }

    /// <summary>
    /// Pantry item entity representing ingredients in user's pantry
    /// </summary>
    public class PantryItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }

    /// <summary>
    /// Recipe entity representing AI-generated or stored recipes
    /// </summary>
    public class Recipe
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public int PrepTimeMinutes { get; set; }
        public int CookTimeMinutes { get; set; }
        public int Servings { get; set; }
        public string[] Ingredients { get; set; } = Array.Empty<string>();
        public string[] Instructions { get; set; } = Array.Empty<string>();
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string[]? GeneratedWith { get; set; }
        public string? CuisineRequested { get; set; }

        // Nutrition information (owned entity)
        public virtual Nutrition? Nutrition { get; set; }
    }

    /// <summary>
    /// Nutrition information as owned entity
    /// </summary>
    public class Nutrition
    {
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public double Fiber { get; set; }
    }

    /// <summary>
    /// Junction table for user favorite recipes
    /// </summary>
    public class UserFavoriteRecipe
    {
        public Guid UserId { get; set; }
        public Guid RecipeId { get; set; }
        public DateTime AddedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Recipe Recipe { get; set; } = null!;
    }

    /// <summary>
    /// Cooking session entity for tracking recipe usage
    /// </summary>
    public class CookingSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string[] IngredientsUsed { get; set; } = Array.Empty<string>();
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Recipe? Recipe { get; set; }
    }

    #endregion
}

