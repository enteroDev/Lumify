/* Middleware for CSRF protection.
 * Validates that the CSRF token from the request header matches the token stored in the cookie
 * For preventing unauthorized requests from external websites on behalf of authenticated users.
 */


using System.Text;

namespace lumify.api.Logic
{
    /// <summary>
    /// ASP.NET Core middleware enforcing CSRF protection via the double-submit-cookie pattern:
    /// for mutating requests (POST/PUT/PATCH/DELETE) the <c>X-CSRF-Token</c> header must match the
    /// <c>XSRF-TOKEN</c> cookie, using a constant-time comparison.
    /// </summary>
    /// <remarks>
    /// Non-mutating methods, CORS preflight, the auth endpoints that run before a CSRF cookie
    /// exists (login, register, password reset, e-mail verification, TOTP login) and the SignalR
    /// hub paths under <c>/hubs</c> are exempt. A failed check returns 403.
    /// </remarks>
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
            "/account/resetPassword",
            "/account/verifyEmail",
            "/account/resendVerification",
            "/account/verifyTotpLogin"
        };

        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates the middleware with the next delegate in the request pipeline.
        /// </summary>
        public CsrfMiddleware(RequestDelegate next) => _next = next;


        /// <summary>
        /// Processes a request: lets exempt and non-mutating requests through, otherwise compares
        /// the CSRF header against the cookie and short-circuits with 403 on mismatch.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
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


        /// <summary>
        /// Compares two strings in constant time to avoid leaking information through timing
        /// side channels.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns><c>true</c> if the values are byte-for-byte equal.</returns>
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