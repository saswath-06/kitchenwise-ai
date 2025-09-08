using KitchenWise.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KitchenWise.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByAuth0IdAsync(string auth0UserId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(User user);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UserExistsAsync(Guid id);
        Task<bool> UserExistsByAuth0IdAsync(string auth0UserId);
    }

    public class UserRepository : IUserRepository
    {
        private readonly KitchenWiseDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(KitchenWiseDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.PantryItems)
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by ID: {id}");
                throw;
            }
        }

        public async Task<User?> GetUserByAuth0IdAsync(string auth0UserId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.PantryItems)
                    .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by Auth0 ID: {auth0UserId}");
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.PantryItems)
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user by email: {email}");
                throw;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.LastLoginAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created new user: {user.Email} with ID: {user.Id}");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user: {user.Email}");
                throw;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                user.LastLoginAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated user: {user.Email}");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user: {user.Email}");
                throw;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.PantryItems)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(Guid id)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user exists: {id}");
                throw;
            }
        }

        public async Task<bool> UserExistsByAuth0IdAsync(string auth0UserId)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Auth0UserId == auth0UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user exists by Auth0 ID: {auth0UserId}");
                throw;
            }
        }
    }
}

