using System;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;

namespace KitchenWise.Desktop.Services
{
    /// <summary>
    /// Authentication service with API integration
    /// Combines mock Auth0 with real API user management
    /// </summary>
    public class AuthService
    {
        private readonly string _domain;
        private readonly string _clientId;
        private readonly string _redirectUri;
        private readonly ApiService _apiService;
        private readonly OidcClient _oidcClient;

        private UserProfile? _currentUser;
        private UserDto? _currentUserData;
        private string? _accessToken;
        private bool _isAuthenticated;

        public AuthService(string domain, string clientId, string redirectUri, ApiService apiService)
        {
            _domain = domain ?? throw new ArgumentNullException(nameof(domain));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _redirectUri = redirectUri ?? throw new ArgumentNullException(nameof(redirectUri));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize OIDC client for Auth0
            var options = new OidcClientOptions
            {
                Authority = $"https://{_domain}",
                ClientId = _clientId,
                RedirectUri = _redirectUri,
                PostLogoutRedirectUri = _redirectUri,
                Scope = "openid profile email",
                Browser = new SystemBrowser()
            };

            _oidcClient = new OidcClient(options);

            Console.WriteLine($"AuthService initialized with real Auth0 OIDC client: {_domain}");
        }

        /// <summary>
        /// Current authenticated user profile
        /// </summary>
        public UserProfile? CurrentUser => _currentUser;

        /// <summary>
        /// Current user data from API
        /// </summary>
        public UserDto? CurrentUserData => _currentUserData;

        /// <summary>
        /// Current access token for API calls
        /// </summary>
        public string? AccessToken => _accessToken;

        /// <summary>
        /// Whether user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => _isAuthenticated && _currentUser != null && !string.IsNullOrEmpty(_accessToken);

        /// <summary>
        /// Event raised when authentication state changes
        /// </summary>
        public event EventHandler<AuthStateChangedEventArgs>? AuthStateChanged;

        /// <summary>
        /// Attempt to login user with Auth0 and sync with API
        /// </summary>
        /// <returns>Login result with user info or error</returns>
        public async Task<LoginResult> LoginAsync()
        {
            try
            {
                Console.WriteLine("Starting real Auth0 login process with API integration...");

                // Step 1: Real Auth0 authentication
                var loginResult = await _oidcClient.LoginAsync();
                
                if (loginResult.IsError)
                {
                    Console.WriteLine($"Auth0 login failed: {loginResult.Error}");
                    return new LoginResult
                    {
                        IsSuccess = false,
                        User = null,
                        UserData = null,
                        AccessToken = null,
                        ErrorMessage = loginResult.Error
                    };
                }

                // Extract user information from Auth0 result
                var auth0User = new UserProfile
                {
                    UserId = loginResult.User.FindFirst("sub")?.Value ?? "",
                    Email = loginResult.User.FindFirst("email")?.Value ?? "",
                    Name = loginResult.User.FindFirst("name")?.Value ?? "",
                    Picture = loginResult.User.FindFirst("picture")?.Value,
                    EmailVerified = bool.Parse(loginResult.User.FindFirst("email_verified")?.Value ?? "false"),
                    Provider = "auth0"
                };

                var accessToken = loginResult.AccessToken;

                Console.WriteLine($"Real Auth0 login successful for: {auth0User.Email}");

                // Step 2: Set API authorization header
                _apiService.SetAuthorizationHeader(accessToken);

                // Step 3: Sync user with API (create or update)
                var userDto = await SyncUserWithApiAsync(auth0User);
                if (userDto == null)
                {
                    throw new Exception("Failed to sync user with API");
                }

                // Step 4: Update last login in API
                await _apiService.UpdateLastLoginAsync(userDto.Id);

                // Step 5: Set authentication state
                _currentUser = auth0User;
                _currentUserData = userDto;
                _accessToken = accessToken;
                _isAuthenticated = true;

                Console.WriteLine($"Login successful with API sync for user: {auth0User.Email}");

                // Raise auth state changed event
                OnAuthStateChanged(new AuthStateChangedEventArgs(true, auth0User, userDto));

                return new LoginResult
                {
                    IsSuccess = true,
                    User = auth0User,
                    UserData = userDto,
                    AccessToken = accessToken,
                    ErrorMessage = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");

                // Clear any partial authentication state
                await LogoutAsync();

                return new LoginResult
                {
                    IsSuccess = false,
                    User = null,
                    UserData = null,
                    AccessToken = null,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Logout current user and clear authentication state
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                Console.WriteLine("Starting real Auth0 logout process...");

                var wasAuthenticated = _isAuthenticated;
                var previousUser = _currentUser;

                // Skip Auth0 browser logout due to configuration issues
                // Just perform local cleanup
                Console.WriteLine("Performing local logout (skipping Auth0 browser logout)...");

                // Clear authentication state
                _currentUser = null;
                _currentUserData = null;
                _accessToken = null;
                _isAuthenticated = false;

                // Clear API authorization
                _apiService.SetAuthorizationHeader(null);

                Console.WriteLine("Logout completed successfully");

                // Raise auth state changed event if user was previously authenticated
                if (wasAuthenticated)
                {
                    OnAuthStateChanged(new AuthStateChangedEventArgs(false, null, null));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
                // Even if logout fails, clear local state
                _currentUser = null;
                _currentUserData = null;
                _accessToken = null;
                _isAuthenticated = false;
                _apiService.SetAuthorizationHeader(null);
                
                Console.WriteLine("Authorization header cleared");
                
                // Still raise the event to update UI
                OnAuthStateChanged(new AuthStateChangedEventArgs(false, null, null));
            }
        }

        /// <summary>
        /// Sync Auth0 user with API database
        /// </summary>
        /// <param name="auth0User">Auth0 user profile</param>
        /// <returns>User data from API</returns>
        private async Task<UserDto?> SyncUserWithApiAsync(UserProfile auth0User)
        {
            try
            {
                Console.WriteLine($"Syncing user with API: {auth0User.Email}");

                // Check if user already exists in API
                var existingUser = await _apiService.GetUserByAuth0IdAsync(auth0User.UserId);

                if (existingUser != null)
                {
                    Console.WriteLine($"User found in API: {existingUser.Email}");
                    return existingUser;
                }

                // Create new user in API
                var createRequest = new CreateUserRequest
                {
                    Auth0UserId = auth0User.UserId,
                    Email = auth0User.Email,
                    Name = auth0User.Name,
                    IsEmailVerified = auth0User.EmailVerified,
                    PreferredCuisines = new[] { "Italian", "Mexican" } // Default preferences
                };

                var newUser = await _apiService.CreateOrUpdateUserAsync(createRequest);
                if (newUser != null)
                {
                    Console.WriteLine($"New user created in API: {newUser.Email}");
                }

                return newUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing user with API: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if the current token is still valid
        /// </summary>
        /// <returns>True if token is valid, false otherwise</returns>
        public async Task<bool> IsTokenValidAsync()
        {
            if (string.IsNullOrEmpty(_accessToken) || _currentUser == null)
            {
                return false;
            }

            try
            {
                // Test API connectivity with current token
                var isHealthy = await _apiService.IsApiHealthyAsync();
                if (!isHealthy)
                {
                    Console.WriteLine("API is not healthy - token may be invalid");
                    return false;
                }

                // Mock token validation - assume valid for demo
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refresh the access token if needed
        /// </summary>
        /// <returns>True if refresh successful, false otherwise</returns>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                if (!_isAuthenticated || _currentUser == null)
                {
                    return false;
                }

                Console.WriteLine("Refreshing access token...");

                // For Auth0, we would typically use refresh tokens
                // For now, we'll re-authenticate silently if possible
                Console.WriteLine("Token refresh not implemented - user may need to re-login");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token refresh error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get updated user info from API
        /// </summary>
        /// <returns>Updated user profile</returns>
        public async Task<UserDto?> GetUpdatedUserInfoAsync()
        {
            try
            {
                if (!IsAuthenticated || _currentUser == null)
                {
                    return null;
                }

                var updatedUser = await _apiService.GetUserByAuth0IdAsync(_currentUser.UserId);
                if (updatedUser != null)
                {
                    _currentUserData = updatedUser;
                    Console.WriteLine($"Updated user info retrieved: {updatedUser.Email}");
                }

                return updatedUser;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get updated user info error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Test API connectivity
        /// </summary>
        /// <returns>API status information</returns>
        public async Task<ApiStatusResponse?> TestApiConnectivityAsync()
        {
            try
            {
                return await _apiService.GetApiStatusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API connectivity test failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Initialize the service and check for existing authentication
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine("Initializing AuthService with API integration...");

                // Test API connectivity
                var apiStatus = await _apiService.GetApiStatusAsync();
                if (apiStatus != null)
                {
                    Console.WriteLine($"API connection successful: {apiStatus.Service} v{apiStatus.Version}");
                }
                else
                {
                    Console.WriteLine("Warning: Could not connect to API");
                }

                // Check for stored refresh tokens or session (placeholder for now)
                await Task.Delay(100);

                Console.WriteLine("AuthService initialization completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService initialization error: {ex.Message}");
            }
        }


        /// <summary>
        /// Raise the AuthStateChanged event
        /// </summary>
        private void OnAuthStateChanged(AuthStateChangedEventArgs args)
        {
            AuthStateChanged?.Invoke(this, args);
        }
    }

    /// <summary>
    /// User profile information from Auth0
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
        public bool EmailVerified { get; set; }
        public string Provider { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"{Name} ({Email})";
        }
    }

    /// <summary>
    /// Result of login operation with API data
    /// </summary>
    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public UserProfile? User { get; set; }
        public UserDto? UserData { get; set; }
        public string? AccessToken { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event arguments for authentication state changes with API data
    /// </summary>
    public class AuthStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; }
        public UserProfile? User { get; }
        public UserDto? UserData { get; }

        public AuthStateChangedEventArgs(bool isAuthenticated, UserProfile? user, UserDto? userData)
        {
            IsAuthenticated = isAuthenticated;
            User = user;
            UserData = userData;
        }
    }
}