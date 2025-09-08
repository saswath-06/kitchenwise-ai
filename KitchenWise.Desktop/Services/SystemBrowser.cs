using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.OidcClient.Browser;

namespace KitchenWise.Desktop.Services
{
    /// <summary>
    /// System browser implementation for OIDC authentication
    /// Opens the default system browser for Auth0 login
    /// </summary>
    public class SystemBrowser : IBrowser
    {
        private readonly int _port;

        public SystemBrowser(int port = 8080)
        {
            _port = port;
        }

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create a simple HTTP listener to capture the callback
                var listener = new System.Net.HttpListener();
                var callbackUrl = $"http://localhost:{_port}/callback/";
                
                // Try to start listener with port conflict handling
                try
                {
                    listener.Prefixes.Add(callbackUrl);
                    listener.Start();
                }
                catch (System.Net.HttpListenerException ex) when (ex.Message.Contains("conflicts with an existing registration"))
                {
                    // Port conflict - try to stop any existing listener and retry
                    listener.Close();
                    listener = new System.Net.HttpListener();
                    listener.Prefixes.Add(callbackUrl);
                    
                    // Wait a bit and try again
                    await Task.Delay(1000, cancellationToken);
                    listener.Start();
                }
                Console.WriteLine($"✅ Auth0 callback listener started on {callbackUrl}");

                // Open the browser with the Auth0 login URL
                OpenBrowser(options.StartUrl);
                Console.WriteLine($"🌐 Opened browser for Auth0 authentication");

                // Wait for the callback
                var context = await listener.GetContextAsync();
                var response = context.Response;
                
                // Send a success page to the browser
                var responseString = @"
                    <html>
                    <head><title>KitchenWise - Login Successful</title></head>
                    <body style='font-family: Arial; text-align: center; padding: 50px;'>
                        <h2>🎉 Login Successful!</h2>
                        <p>You can now close this window and return to KitchenWise.</p>
                    </body>
                    </html>";
                
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                response.StatusCode = 200;
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                listener.Stop();

                // Return the callback URL with parameters
                var callbackUri = context.Request.Url.ToString();
                Console.WriteLine($"✅ Auth0 callback received: {callbackUri}");

                return new BrowserResult
                {
                    ResultType = BrowserResultType.Success,
                    Response = callbackUri
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Auth0 browser error: {ex.Message}");
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = ex.Message
                };
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open browser: {ex.Message}");
                throw;
            }
        }
    }
}
