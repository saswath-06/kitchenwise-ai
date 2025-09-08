using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using KitchenWise.Api.Data;
using KitchenWise.Api.Repositories;

namespace KitchenWise.Api.Controllers
{
    /// <summary>
    /// User management API controller
    /// Handles user registration, profiles, and authentication data
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserRepository _userRepository;

        public UserController(ILogger<UserController> logger, IUserRepository userRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        /// <summary>
        /// Get all users (admin only in production)
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
        {
            try
            {
                _logger.LogInformation("Getting all users");

                var users = await _userRepository.GetAllUsersAsync();
                var userDtos = users.Select(MapToDto).OrderBy(u => u.Name).ToList();

                _logger.LogInformation($"Found {userDtos.Count} users");
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            try
            {
                _logger.LogInformation($"Getting user with ID: {id}");

                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {id}");
                    return NotFound(new { error = "User not found", userId = id });
                }

                var userDto = MapToDto(user);
                _logger.LogInformation($"Found user: {userDto.Email}");
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get user by Auth0 user ID
        /// </summary>
        /// <param name="auth0UserId">Auth0 user ID</param>
        /// <returns>User details</returns>
        [HttpGet("auth0/{auth0UserId}")]
        public async Task<ActionResult<UserDto>> GetUserByAuth0Id(string auth0UserId)
        {
            try
            {
                _logger.LogInformation($"Getting user with Auth0 ID: {auth0UserId}");

                if (string.IsNullOrWhiteSpace(auth0UserId))
                {
                    return BadRequest(new { error = "Auth0 user ID is required" });
                }

                var user = await _userRepository.GetUserByAuth0IdAsync(auth0UserId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for Auth0 ID: {auth0UserId}");
                    return NotFound(new { error = "User not found", auth0UserId });
                }

                var userDto = MapToDto(user);
                _logger.LogInformation($"Found user: {userDto.Email}");
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by Auth0 ID {auth0UserId}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Create or update user profile
        /// </summary>
        /// <param name="request">User creation/update request</param>
        /// <returns>Created/updated user</returns>
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateOrUpdateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating/updating user: {request.Email}");

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Auth0UserId))
                {
                    return BadRequest(new { error = "Auth0 user ID is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { error = "Email is required" });
                }

                // Check if user already exists
                var existingUser = await _userRepository.GetUserByAuth0IdAsync(request.Auth0UserId);

                if (existingUser != null)
                {
                    // Update existing user
                    existingUser.Name = request.Name ?? existingUser.Name;
                    existingUser.Email = request.Email;
                    existingUser.IsEmailVerified = request.IsEmailVerified ?? existingUser.IsEmailVerified;
                    existingUser.PreferredCuisines = request.PreferredCuisines ?? Array.Empty<string>();

                    var updatedUser = await _userRepository.UpdateUserAsync(existingUser);
                    var userDto = MapToDto(updatedUser);

                    _logger.LogInformation($"Updated existing user: {userDto.Email}");
                    return Ok(userDto);
                }
                else
                {
                    // Create new user
                    var newUser = new User
                    {
                        Auth0UserId = request.Auth0UserId,
                        Email = request.Email,
                        Name = request.Name ?? request.Email.Split('@')[0], // Default name from email
                        IsEmailVerified = request.IsEmailVerified ?? false,
                        PreferredCuisines = request.PreferredCuisines ?? Array.Empty<string>()
                    };

                    var createdUser = await _userRepository.CreateUserAsync(newUser);
                    var userDto = MapToDto(createdUser);

                    _logger.LogInformation($"Created new user: {userDto.Email} with ID: {userDto.Id}");
                    return CreatedAtAction(nameof(GetUser), new { id = userDto.Id }, userDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating/updating user: {request.Email}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Update user's last login time
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success result</returns>
        [HttpPut("{id}/login")]
        public async Task<ActionResult> UpdateLastLogin(Guid id)
        {
            try
            {
                _logger.LogInformation($"Updating last login for user: {id}");

                var user = await _userRepository.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found", userId = id });
                }

                await _userRepository.UpdateUserAsync(user);

                _logger.LogInformation($"Updated last login for user: {user.Email}");
                return Ok(new { message = "Last login updated successfully", timestamp = user.LastLoginAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating last login for user {id}");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Delete user (admin only in production) - Disabled for now
        /// </summary>
        /// <param name="id">User ID to delete</param>
        /// <returns>Success result</returns>
        /*[HttpDelete("{id}")]
        public ActionResult DeleteUser(Guid id)
        {
            // TODO: Implement with repository pattern
            return StatusCode(501, new { error = "Delete user not implemented yet" });
        }*/

        /// <summary>
        /// Get API health and statistics
        /// </summary>
        /// <returns>API status information</returns>
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            try
            {
                var users = await _userRepository.GetAllUsersAsync();
                var userDtos = users.Select(MapToDto).ToList();

                var stats = new
                {
                    TotalUsers = userDtos.Count,
                    VerifiedUsers = userDtos.Count(u => u.IsEmailVerified),
                    ActiveUsers = userDtos.Count(u => u.LastLoginAt > DateTime.UtcNow.AddDays(-7)),
                    TotalPantryItems = userDtos.Sum(u => u.PantryItemCount),
                    PopularCuisines = userDtos
                        .SelectMany(u => u.PreferredCuisines)
                        .GroupBy(c => c)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => new { Cuisine = g.Key, UserCount = g.Count() })
                        .ToList(),
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Maps User entity to UserDto
        /// </summary>
        private UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Auth0UserId = user.Auth0UserId,
                Email = user.Email,
                Name = user.Name,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                PantryItemCount = user.PantryItems?.Count ?? 0,
                PreferredCuisines = user.PreferredCuisines
            };
        }
    }

    /// <summary>
    /// User data transfer object
    /// </summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Auth0UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int PantryItemCount { get; set; }
        public string[] PreferredCuisines { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Request model for creating/updating users
    /// </summary>
    public class CreateUserRequest
    {
        public string Auth0UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool? IsEmailVerified { get; set; }
        public string[]? PreferredCuisines { get; set; }
    }
}