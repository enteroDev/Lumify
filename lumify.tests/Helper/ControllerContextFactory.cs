/* ControllerContextFactory
 * Fakes the authenticated user for a controller.
 * This is NOT a mock - it just builds the ControllerContext/claims that a real HTTP request
 * would normally provide. Controllers read the current user via User.FindFirst("UserID"); in a
 * real request that comes from the JWT/cookie, in a test we attach the claim manually here.
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace lumify.tests.Helper
{
    public static class ControllerContextFactory
    {
        // --------------- //
        // --- Sign In --- //
        // --------------- //

        // Attaches a user carrying the given UserID claim to the controller.
        public static void SignIn(ControllerBase controller, string userID)
        {
            // Build the identity that holds the UserID claim the controllers look for.
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim("UserID", userID));

            ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            // Put that user onto a fresh HttpContext and hand it to the controller.
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.User = principal;

            ControllerContext controllerContext = new ControllerContext();
            controllerContext.HttpContext = httpContext;

            controller.ControllerContext = controllerContext;
        }

        // Attaches a request context that has NO UserID claim (simulates an unauthenticated request).
        // The controllers throw UnauthorizedAccessException when that claim is missing.
        public static void SignInAnonymous(ControllerBase controller)
        {
            // A fresh DefaultHttpContext already carries an empty (unauthenticated) user.
            DefaultHttpContext httpContext = new DefaultHttpContext();

            ControllerContext controllerContext = new ControllerContext();
            controllerContext.HttpContext = httpContext;

            controller.ControllerContext = controllerContext;
        }
    }
}
