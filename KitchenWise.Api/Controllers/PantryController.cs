using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using KitchenWise.Api.Repositories;
using KitchenWise.Api.Data;

namespace KitchenWise.Api.Controllers
{
    /// <summary>
    /// Pantry management API controller
    /// Handles CRUD operations for user pantry items
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PantryController : ControllerBase
    {
        private readonly ILogger<PantryController> _logger;
        private readonly IPantryRepository _pantryRepository;

        public PantryController(ILogger<PantryController> logger, IPantryRepository pantryRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pantryRepository = pantryRepository ?? throw new ArgumentNullException(nameof(pantryRepository));
        }

        /// <summary>
        /// Get all pantry items (for testing database connection)
        /// </summary>
        /// <returns>List of all pantry items in database</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> GetAllPantryItems()
        {
            try
            {
                _logger.LogInformation("Getting all pantry items from database");

                // Get demo user ID from database
                var demoUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
                var allItems = await _pantryRepository.GetUserPantryItemsAsync(demoUserId);
                
                var dtoItems = allItems.Select(item => new PantryItemDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    AddedDate = item.AddedDate,
                    ExpiryDate = item.ExpiryDate
                }).OrderBy(item => item.Name).ToList();

                _logger.LogInformation($"Found {dtoItems.Count} pantry items in Azure SQL database");
                return Ok(new { 
                    message = "✅ Successfully connected to Azure SQL Database!",
                    databaseName = "kitchenwiseuser",
                    totalItems = dtoItems.Count,
                    items = dtoItems 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pantry items from database");
                return StatusCode(500, new { error = "Database connection failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Get all pantry items for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user's pantry items</returns>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> GetUserPantryItems(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting pantry items for user: {userId}");

                var userItems = await _pantryRepository.GetUserPantryItemsAsync(userId);
                var dtoItems = userItems.Select(item => new PantryItemDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    AddedDate = item.AddedDate,
                    ExpiryDate = item.ExpiryDate
                }).OrderBy(item => item.Name).ToList();

                _logger.LogInformation($"Found {dtoItems.Count} pantry items for user: {userId}");
                return Ok(dtoItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pantry items for user {userId}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get specific pantry item by ID
        /// </summary>
        /// <param name="id">Pantry item ID</param>
        /// <returns>Pantry item details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<PantryItemDto>> GetPantryItem(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting pantry item: {id}");

                var item = await _pantryRepository.GetPantryItemByIdAsync(id);
                if (item == null)
                {
                    _logger.LogWarning($"Pantry item not found: {id}");
                    return NotFound(new { error = "Pantry item not found", itemId = id });
                }

                var dto = new PantryItemDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    AddedDate = item.AddedDate,
                    ExpiryDate = item.ExpiryDate
                };

                _logger.LogInformation($"Found pantry item: {item.Name}");
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pantry item {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Add new item to user's pantry
        /// </summary>
        /// <param name="request">Pantry item creation request</param>
        /// <returns>Created pantry item</returns>
        [HttpPost]
        public async Task<ActionResult<PantryItemDto>> AddPantryItem([FromBody] CreatePantryItemRequest request)
        {
            try
            {
                _logger.LogInformation($"Adding pantry item: {request.Name} for user: {request.UserId}");

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { error = "Item name is required" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { error = "Quantity must be greater than 0" });
                }

                if (request.UserId == Guid.Empty)
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                // Create new pantry item entity
                var newItem = new PantryItem
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Name = request.Name.Trim(),
                    Quantity = request.Quantity,
                    Unit = request.Unit?.Trim() ?? "piece",
                    Category = request.Category?.Trim() ?? "Other",
                    AddedDate = DateTime.UtcNow,
                    ExpiryDate = request.ExpiryDate
                };

                var savedItem = await _pantryRepository.AddPantryItemAsync(newItem);

                // Convert to DTO
                var dto = new PantryItemDto
                {
                    Id = savedItem.Id,
                    UserId = savedItem.UserId,
                    Name = savedItem.Name,
                    Quantity = savedItem.Quantity,
                    Unit = savedItem.Unit,
                    Category = savedItem.Category,
                    AddedDate = savedItem.AddedDate,
                    ExpiryDate = savedItem.ExpiryDate
                };

                _logger.LogInformation($"Added pantry item to database: {savedItem.Name} with ID: {savedItem.Id}");
                return CreatedAtAction(nameof(GetPantryItem), new { id = savedItem.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding pantry item: {request.Name}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Update existing pantry item
        /// </summary>
        /// <param name="id">Pantry item ID</param>
        /// <param name="request">Update request</param>
        /// <returns>Updated pantry item</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<PantryItemDto>> UpdatePantryItem(Guid id, [FromBody] UpdatePantryItemRequest request)
        {
            try
            {
                _logger.LogInformation($"Updating pantry item: {id}");

                var item = await _pantryRepository.GetPantryItemByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { error = "Pantry item not found", itemId = id });
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    item.Name = request.Name.Trim();
                }

                if (request.Quantity.HasValue && request.Quantity.Value >= 0)
                {
                    item.Quantity = request.Quantity.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Unit))
                {
                    item.Unit = request.Unit.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    item.Category = request.Category.Trim();
                }

                if (request.ExpiryDate.HasValue)
                {
                    item.ExpiryDate = request.ExpiryDate.Value;
                }

                var updatedItem = await _pantryRepository.UpdatePantryItemAsync(item);
                if (updatedItem == null)
                {
                    return StatusCode(500, new { error = "Failed to update item" });
                }

                var dto = new PantryItemDto
                {
                    Id = updatedItem.Id,
                    UserId = updatedItem.UserId,
                    Name = updatedItem.Name,
                    Quantity = updatedItem.Quantity,
                    Unit = updatedItem.Unit,
                    Category = updatedItem.Category,
                    AddedDate = updatedItem.AddedDate,
                    ExpiryDate = updatedItem.ExpiryDate
                };

                _logger.LogInformation($"Updated pantry item: {updatedItem.Name}");
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating pantry item {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete pantry item
        /// </summary>
        /// <param name="id">Pantry item ID</param>
        /// <returns>Success result</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePantryItem(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting pantry item: {id}");

                var item = await _pantryRepository.GetPantryItemByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { error = "Pantry item not found", itemId = id });
                }

                var deleted = await _pantryRepository.DeletePantryItemAsync(id);
                if (!deleted)
                {
                    return StatusCode(500, new { error = "Failed to delete item" });
                }

                _logger.LogInformation($"Deleted pantry item: {item.Name}");
                return Ok(new { message = "Pantry item deleted successfully", deletedItem = item.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting pantry item {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Consume quantity of pantry item (reduce quantity)
        /// </summary>
        /// <param name="id">Pantry item ID</param>
        /// <param name="request">Consumption request</param>
        /// <returns>Updated pantry item or deletion result</returns>
        [HttpPost("{id}/consume")]
        public async Task<ActionResult<PantryItemDto>> ConsumePantryItem(Guid id, [FromBody] ConsumePantryItemRequest request)
        {
            try
            {
                _logger.LogInformation($"Consuming pantry item: {id}, amount: {request.Amount}");

                if (request.Amount <= 0)
                {
                    return BadRequest(new { error = "Consumption amount must be greater than 0" });
                }

                var result = await _pantryRepository.ConsumePantryItemAsync(id, request.Amount);
                if (result == null)
                {
                    return NotFound(new { error = "Pantry item not found or consumption failed", itemId = id });
                }

                // If quantity is 0, item was removed
                if (result.Quantity <= 0)
                {
                    _logger.LogInformation($"Pantry item consumed completely and removed: {result.Name}");
                    return Ok(new { message = "Item consumed completely and removed from pantry", consumedItem = result.Name });
                }

                var dto = new PantryItemDto
                {
                    Id = result.Id,
                    UserId = result.UserId,
                    Name = result.Name,
                    Quantity = result.Quantity,
                    Unit = result.Unit,
                    Category = result.Category,
                    AddedDate = result.AddedDate,
                    ExpiryDate = result.ExpiryDate
                };

                _logger.LogInformation($"Consumed {request.Amount} of {result.Name}, {result.Quantity} remaining");
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error consuming pantry item {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get pantry statistics for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Pantry statistics</returns>
        [HttpGet("user/{userId}/stats")]
        public async Task<ActionResult> GetUserPantryStats(Guid userId)
        {
            try
            {
                _logger.LogInformation($"Getting pantry stats for user: {userId}");

                var stats = await _pantryRepository.GetUserPantryStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting pantry stats for user {userId}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Search pantry items for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="category">Category filter</param>
        /// <returns>Filtered pantry items</returns>
        [HttpGet("user/{userId}/search")]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> SearchUserPantryItems(
            Guid userId,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? category = null)
        {
            try
            {
                _logger.LogInformation($"Searching pantry for user: {userId}, term: '{searchTerm}', category: '{category}'");

                var userItems = await _pantryRepository.SearchUserPantryItemsAsync(userId, searchTerm, category);
                var dtoItems = userItems.Select(item => new PantryItemDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Category = item.Category,
                    AddedDate = item.AddedDate,
                    ExpiryDate = item.ExpiryDate
                }).OrderBy(item => item.Name).ToList();

                _logger.LogInformation($"Search returned {dtoItems.Count} items");
                return Ok(dtoItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching pantry for user {userId}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }

    #region Data Transfer Objects

    /// <summary>
    /// Pantry item data transfer object
    /// </summary>
    public class PantryItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for creating new pantry item
    /// </summary>
    public class CreatePantryItemRequest
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for updating pantry item
    /// </summary>
    public class UpdatePantryItemRequest
    {
        public string? Name { get; set; }
        public double? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Category { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Request for consuming pantry item
    /// </summary>
    public class ConsumePantryItemRequest
    {
        public double Amount { get; set; }
    }

    #endregion
}