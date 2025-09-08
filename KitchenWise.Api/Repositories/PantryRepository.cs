using Microsoft.EntityFrameworkCore;
using KitchenWise.Api.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KitchenWise.Api.Repositories
{
    /// <summary>
    /// Repository for pantry item data access operations
    /// Replaces mock data with real database operations
    /// </summary>
    public interface IPantryRepository
    {
        Task<IEnumerable<PantryItem>> GetUserPantryItemsAsync(Guid userId);
        Task<PantryItem?> GetPantryItemByIdAsync(Guid itemId);
        Task<PantryItem> AddPantryItemAsync(PantryItem item);
        Task<PantryItem?> UpdatePantryItemAsync(PantryItem item);
        Task<bool> DeletePantryItemAsync(Guid itemId);
        Task<PantryItem?> ConsumePantryItemAsync(Guid itemId, double amount);
        Task<IEnumerable<PantryItem>> SearchUserPantryItemsAsync(Guid userId, string? searchTerm = null, string? category = null);
        Task<PantryStatsDto> GetUserPantryStatsAsync(Guid userId);
    }

    /// <summary>
    /// Implementation of pantry repository with Entity Framework
    /// </summary>
    public class PantryRepository : IPantryRepository
    {
        private readonly KitchenWiseDbContext _context;
        private readonly ILogger<PantryRepository> _logger;

        public PantryRepository(KitchenWiseDbContext context, ILogger<PantryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Read Operations

        /// <summary>
        /// Get all pantry items for a specific user
        /// </summary>
        public async Task<IEnumerable<PantryItem>> GetUserPantryItemsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Fetching pantry items for user: {userId}");

                var items = await _context.PantryItems
                    .Where(item => item.UserId == userId)
                    .OrderBy(item => item.Name)
                    .ToListAsync();

                _logger.LogInformation($"Found {items.Count} pantry items for user: {userId}");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching pantry items for user: {userId}");
                throw;
            }
        }

        /// <summary>
        /// Get specific pantry item by ID
        /// </summary>
        public async Task<PantryItem?> GetPantryItemByIdAsync(Guid itemId)
        {
            try
            {
                _logger.LogInformation($"Fetching pantry item: {itemId}");

                var item = await _context.PantryItems
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Id == itemId);

                if (item != null)
                {
                    _logger.LogInformation($"Found pantry item: {item.Name}");
                }
                else
                {
                    _logger.LogWarning($"Pantry item not found: {itemId}");
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching pantry item: {itemId}");
                throw;
            }
        }

        /// <summary>
        /// Search pantry items with filters
        /// </summary>
        public async Task<IEnumerable<PantryItem>> SearchUserPantryItemsAsync(Guid userId, string? searchTerm = null, string? category = null)
        {
            try
            {
                _logger.LogInformation($"Searching pantry for user: {userId}, term: '{searchTerm}', category: '{category}'");

                var query = _context.PantryItems.Where(item => item.UserId == userId);

                // Apply search term filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLowerInvariant();
                    query = query.Where(item =>
                        item.Name.ToLower().Contains(term) ||
                        item.Category.ToLower().Contains(term) ||
                        item.Unit.ToLower().Contains(term));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category) && category != "All")
                {
                    query = query.Where(item => item.Category == category);
                }

                var results = await query.OrderBy(item => item.Name).ToListAsync();

                _logger.LogInformation($"Search returned {results.Count} items");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching pantry items for user: {userId}");
                throw;
            }
        }

        /// <summary>
        /// Get pantry statistics for a user
        /// </summary>
        public async Task<PantryStatsDto> GetUserPantryStatsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting pantry stats for user: {userId}");

                var userItems = await _context.PantryItems
                    .Where(item => item.UserId == userId)
                    .ToListAsync();

                var now = DateTime.UtcNow;

                var stats = new PantryStatsDto
                {
                    TotalItems = userItems.Count,
                    TotalQuantity = userItems.Sum(i => i.Quantity),
                    CategoriesCount = userItems.GroupBy(i => i.Category).Count(),
                    ExpiringItems = userItems.Count(i => i.ExpiryDate.HasValue &&
                                                        i.ExpiryDate.Value.Date >= now.Date &&
                                                        i.ExpiryDate.Value.Date <= now.Date.AddDays(7)),
                    ExpiredItems = userItems.Count(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value.Date < now.Date),
                    RecentlyAdded = userItems.Count(i => i.AddedDate >= now.AddDays(-7)),
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation($"Generated pantry stats for user: {userId}");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pantry stats for user: {userId}");
                throw;
            }
        }

        #endregion

        #region Write Operations

        /// <summary>
        /// Add new pantry item
        /// </summary>
        public async Task<PantryItem> AddPantryItemAsync(PantryItem item)
        {
            try
            {
                _logger.LogInformation($"Adding pantry item: {item.Name} for user: {item.UserId}");

                // Validate item
                if (string.IsNullOrWhiteSpace(item.Name))
                    throw new ArgumentException("Item name is required");
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be greater than 0");
                if (item.UserId == Guid.Empty)
                    throw new ArgumentException("User ID is required");

                // Set default values
                item.Id = Guid.NewGuid();
                item.AddedDate = DateTime.UtcNow;
                item.Unit = string.IsNullOrWhiteSpace(item.Unit) ? "piece" : item.Unit;
                item.Category = string.IsNullOrWhiteSpace(item.Category) ? "Other" : item.Category;

                _context.PantryItems.Add(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Added pantry item: {item.Name} with ID: {item.Id}");
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding pantry item: {item.Name}");
                throw;
            }
        }

        /// <summary>
        /// Update existing pantry item
        /// </summary>
        public async Task<PantryItem?> UpdatePantryItemAsync(PantryItem updatedItem)
        {
            try
            {
                _logger.LogInformation($"Updating pantry item: {updatedItem.Id}");

                var existingItem = await _context.PantryItems.FindAsync(updatedItem.Id);
                if (existingItem == null)
                {
                    _logger.LogWarning($"Pantry item not found for update: {updatedItem.Id}");
                    return null;
                }

                // Update properties
                existingItem.Name = updatedItem.Name;
                existingItem.Quantity = updatedItem.Quantity;
                existingItem.Unit = updatedItem.Unit;
                existingItem.Category = updatedItem.Category;
                existingItem.ExpiryDate = updatedItem.ExpiryDate;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated pantry item: {existingItem.Name}");
                return existingItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating pantry item: {updatedItem.Id}");
                throw;
            }
        }

        /// <summary>
        /// Delete pantry item
        /// </summary>
        public async Task<bool> DeletePantryItemAsync(Guid itemId)
        {
            try
            {
                _logger.LogInformation($"Deleting pantry item: {itemId}");

                var item = await _context.PantryItems.FindAsync(itemId);
                if (item == null)
                {
                    _logger.LogWarning($"Pantry item not found for deletion: {itemId}");
                    return false;
                }

                _context.PantryItems.Remove(item);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Deleted pantry item: {item.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting pantry item: {itemId}");
                throw;
            }
        }

        /// <summary>
        /// Consume quantity from pantry item
        /// </summary>
        public async Task<PantryItem?> ConsumePantryItemAsync(Guid itemId, double amount)
        {
            try
            {
                _logger.LogInformation($"Consuming pantry item: {itemId}, amount: {amount}");

                var item = await _context.PantryItems.FindAsync(itemId);
                if (item == null)
                {
                    _logger.LogWarning($"Pantry item not found for consumption: {itemId}");
                    return null;
                }

                if (amount <= 0)
                    throw new ArgumentException("Consumption amount must be greater than 0");
                if (amount > item.Quantity)
                    throw new ArgumentException("Cannot consume more than available quantity");

                // Reduce quantity
                item.Quantity -= amount;

                // If quantity reaches 0 or below, remove item
                if (item.Quantity <= 0)
                {
                    _context.PantryItems.Remove(item);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Pantry item consumed completely and removed: {item.Name}");
                    return null; // Item was removed
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Consumed {amount} of {item.Name}, {item.Quantity} remaining");
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error consuming pantry item: {itemId}");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// Pantry statistics data transfer object
    /// </summary>
    public class PantryStatsDto
    {
        public int TotalItems { get; set; }
        public double TotalQuantity { get; set; }
        public int CategoriesCount { get; set; }
        public int ExpiringItems { get; set; }
        public int ExpiredItems { get; set; }
        public int RecentlyAdded { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

