/* Middleware for CSRF protection.
 * Validates that the CSRF token from the request header matches the token stored in the cookie
 * For preventing unauthorized requests from external websites on behalf of authenticated users.
 */


using System.Text;

namespace lumify.api.Logic
{
    public class CsrfMiddleware
    {
        private static readonly HashSet<string> _methods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete
        };

        // Allowlist for endpoints that must work before a CSRF cookie exists (Equivilant to "AllowAnonymous" in ASP-Controller)
        private static readonly string[] _csrfExemptPaths =
        {
            "/account/loginUser",
            "/account/registerUser",
            "/account/logoutUser",
            "/account/requestPasswordReset",
            "/account/resetPassword"
        };

        private readonly RequestDelegate _next;
        public CsrfMiddleware(RequestDelegate next) => _next = next;


        public async Task Invoke(HttpContext context)
        {
            // Skip for non-mutating requests
            if (!_methods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Allow CORS preflight to pass
            if (HttpMethods.Options.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Exempt specific paths (login/register) from CSRF check
            var path = context.Request.Path.Value ?? string.Empty;
            if (_csrfExemptPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Exempt SignalR hub endpoints from CSRF check
            if (path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Read cookie + header
            context.Request.Cookies.TryGetValue("XSRF-TOKEN", out var cookieToken);
            var headerToken = context.Request.Headers["X-CSRF-Token"].ToString();

            // DEBUG LOGS
            Console.WriteLine($"--- CSRF CHECK ---");
            Console.WriteLine($"Path: '{path}'");
            Console.WriteLine($"Cookie: '{cookieToken}'");
            Console.WriteLine($"Header: '{headerToken}'");

            // Validate
            if (string.IsNullOrEmpty(cookieToken) || string.IsNullOrEmpty(headerToken))
            {
                Console.WriteLine("REASON: One of the tokens is null or empty");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("CSRF validation failed: Missing token");
                return;
            }

            if (!TimingSafeEquals(cookieToken, headerToken))
            {
                var decodedCookie = System.Net.WebUtility.UrlDecode(cookieToken);
                Console.WriteLine($"Retry with URL-Decode: '{decodedCookie}'");

                if (TimingSafeEquals(decodedCookie, headerToken))
                {
                    Console.WriteLine("SUCCESS: Matches after URL Decode");
                    await _next(context);
                    return;
                }

                Console.WriteLine("REASON: Tokens do not match");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("CSRF validation failed: Mismatch");
                return;
            }

            await _next(context);
        }


        // Constant-time comparison to avoid timing side channels
        private static bool TimingSafeEquals(string a, string b)
        {
            var ba = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length)
            {
                return false;
            }

            var diff = 0;
            for (int i = 0; i < ba.Length; i++)
            {
                diff |= ba[i] ^ bb[i];
            }

            return diff == 0;
        }
    }
}