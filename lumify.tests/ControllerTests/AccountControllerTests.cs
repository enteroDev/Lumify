/* AccountControllerTests
 * Unit tests for the password-reset flow of: AccountController
 * (requestPasswordReset + resetPassword).
 *
 * xUnit creates a NEW instance of this class for each test, so the constructor below creates
 * a setup per call (fresh in-memory DB, fake email service, controller) and Dispose() cleans up.
 *
 * The IEmailService is mocked and never sends a real mail. Instead it CAPTURES the reset link,
 * so a test can pull the raw token out of it and drive the second step (resetPassword) - exactly
 * like a real user clicking the link in their inbox.
 */

using lumify.api.Controllers;
using lumify.api.Interfaces;
using lumify.api.Logic;
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.EF;
using lumify.api.Models.Settings;
using lumify.tests.Helper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace lumify.tests.ControllerTests
{
    public class AccountControllerTests : IDisposable
    {
        private const string DefaultUserID = "user-1";
        private const string OldTimestamp = "2000-01-01T00:00:00.0000000Z";

        private readonly LumifyDbContext _db;
        private readonly Mock<IEmailService> _emailMock;
        private readonly AccountController _controller;

        // The most recent reset link handed to the email service (captured, never really sent).
        private string? _lastResetLink;


        public AccountControllerTests()
        {
            _db = TestDbFactory.Create();

            // The mailer is faked - we just remember the link it was asked to send.
            _emailMock = new Mock<IEmailService>();
            _emailMock
                .Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string?, string, CancellationToken>((to, name, link, ct) => _lastResetLink = link)
                .Returns(Task.CompletedTask);

            // Real InternalLogic (it does the password + token hashing). JWT settings are only needed
            // for login/register, not for the reset flow, but the constructor requires them.
            var jwt = Options.Create(new JwtSettings
            {
                Secret = "test-secret-which-is-long-enough-1234567890",
                Issuer = "lumify-api",
                Audience = "lumify-app",
                ExpirationMinutes = 480
            });
            var logic = new InternalLogic(NullLogger<InternalLogic>.Instance, _db, jwt);

            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(x => x.EnvironmentName).Returns("Development");

            var config = new ConfigurationBuilder().Build();

            var app = Options.Create(new AppSettings
            {
                FrontendBaseUrl = "http://localhost:5333",
                PasswordResetPath = "/Auth/reset"
            });

            _controller = new AccountController(
                NullLogger<AccountController>.Instance,
                config,
                env.Object,
                _db,
                logic,
                _emailMock.Object,
                app);

            ControllerContextFactory.SignInAnonymous(_controller);
        }

        // Runs after every single test.
        public void Dispose()
        {
            _db.Dispose();
        }



        // ------------------------------ //
        // --- RequestPasswordReset ----- //
        // ------------------------------ //

        [Fact]
        public async Task RequestPasswordReset_EmptyIdentifier_ReturnsOkAndSendsNothing()
        {
            // In this test we check that a blank identifier is answered generically and triggers no mail.

            // --- Arrange --- //
            RequestPasswordResetRequest request = new RequestPasswordResetRequest { Identifier = "   " };

            // --- Act --- //
            IActionResult result = await _controller.RequestPasswordReset(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect the generic success status.
            Assert.Empty(_db.PasswordResetTokens);                                  // We expect no token, since no identifier was given.
            AssertNoMailSent();                                                     // We expect no mail to be sent.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task RequestPasswordReset_UnknownUser_ReturnsOkAndSendsNothing()
        {
            // In this test we check that an unknown identifier looks identical to a known one (no enumeration).

            // --- Arrange --- //
            // * We seed nobody and ask for a reset of a user that does not exist.
            RequestPasswordResetRequest request = new RequestPasswordResetRequest { Identifier = "ghost" };

            // --- Act --- //
            IActionResult result = await _controller.RequestPasswordReset(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect the same generic success status as for a real user.
            Assert.Empty(_db.PasswordResetTokens);                                  // We expect no token, since the user does not exist.
            AssertNoMailSent();                                                     // We expect no mail, since the user does not exist.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task RequestPasswordReset_DeletedUser_ReturnsOkAndSendsNothing()
        {
            // In this test we check that a soft-deleted account cannot trigger a reset mail.

            // --- Arrange --- //
            // * We seed a soft-deleted user and request a reset by his email.
            SeedUser(deletedAt: OldTimestamp);
            RequestPasswordResetRequest request = new RequestPasswordResetRequest { Identifier = "chilly@test.local" };

            // --- Act --- //
            IActionResult result = await _controller.RequestPasswordReset(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect the generic success status.
            Assert.Empty(_db.PasswordResetTokens);                                  // We expect no token, since the user is soft-deleted.
            AssertNoMailSent();                                                     // We expect no mail, since the user is soft-deleted.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task RequestPasswordReset_KnownUser_CreatesTokenAndSendsLink()
        {
            // In this test we check the happy path: a known user gets exactly one open token and a mail with a link.

            // --- Arrange --- //
            // * We seed a user and request a reset by his username.
            SeedUser(username: "chilly");
            RequestPasswordResetRequest request = new RequestPasswordResetRequest { Identifier = "chilly" };

            // --- Act --- //
            IActionResult result = await _controller.RequestPasswordReset(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect the generic success status.

            PasswordResetToken token = Assert.Single(_db.PasswordResetTokens);      // We expect exactly one token to be created.
            Assert.Equal(DefaultUserID, token.UserID);                              // We expect the token to belong to our user.
            Assert.Null(token.UsedAt);                                              // We expect the fresh token to be unused.
            Assert.NotEqual(ExtractToken(_lastResetLink!), token.TokenHash);        // We expect the DB to store the HASH, never the raw token from the link.

            _emailMock.Verify(                                                      // We expect exactly one mail to be sent...
                x => x.SendPasswordResetEmailAsync("chilly@test.local", It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once());
            Assert.StartsWith("http://localhost:5333/Auth/reset?token=", _lastResetLink); // ...with a link pointing at the frontend reset page.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task RequestPasswordReset_SecondRequest_InvalidatesPreviousToken()
        {
            // In this test we check that requesting a new link invalidates the previous (still open) one.

            // --- Arrange --- //
            // * We seed a user and request a reset twice.
            SeedUser();

            // --- Act --- //
            await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Identifier = "chilly" }, CancellationToken.None);
            await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Identifier = "chilly" }, CancellationToken.None);

            // --- Assert --- //
            List<PasswordResetToken> tokens = _db.PasswordResetTokens.ToList();
            Assert.Equal(2, tokens.Count);                                          // We expect both tokens to still exist as rows.
            Assert.Single(tokens, t => t.UsedAt == null);                           // We expect exactly one of them to still be open (the newest).
        }



        // ------------------------ //
        // --- ResetPassword ------ //
        // ------------------------ //

        [Fact]
        public async Task ResetPassword_MissingFields_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to reset without a token / password.
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = "", NewPassword = "" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since token and password are required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_TooShortPassword_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We submit a password that is shorter than the minimum length.
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = "whatever", NewPassword = "short" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the password is too short.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_UnknownToken_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed a user but use a token that was never issued.
            SeedUser();

            // --- Act --- //
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = "not-a-real-token", NewPassword = "newPassword123" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the token cannot be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_ValidToken_ChangesPasswordAndConsumesToken()
        {
            // In this test we check the happy path: a valid token sets a new password and is then consumed.

            // --- Arrange --- //
            // * We seed a user, request a reset and grab the raw token from the captured link.
            SeedUser();
            await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Identifier = "chilly" }, CancellationToken.None);
            string rawToken = ExtractToken(_lastResetLink!);

            // --- Act --- //
            // * We submit the token together with a new password.
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = rawToken, NewPassword = "brandNewPassword1" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect a success status.

            User user = _db.Users.Single();
            Assert.NotEqual("OLD_HASH", user.PasswordHash);                         // We expect the stored password hash to have changed.
            Assert.NotEqual(OldTimestamp, user.UpdatedAt);                          // We expect the user's UpdatedAt to have advanced.

            PasswordResetToken token = _db.PasswordResetTokens.Single();
            Assert.NotNull(token.UsedAt);                                           // We expect the token to be consumed (single-use).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_AlreadyUsedToken_ReturnsBadRequest()
        {
            // In this test we check that a token cannot be used a second time.

            // --- Arrange --- //
            // * We seed a user, request a reset, and use the token once successfully.
            SeedUser();
            await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Identifier = "chilly" }, CancellationToken.None);
            string rawToken = ExtractToken(_lastResetLink!);
            await _controller.ResetPassword(new ResetPasswordRequest { Token = rawToken, NewPassword = "brandNewPassword1" }, CancellationToken.None);

            // --- Act --- //
            // * We try to reuse the same token.
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = rawToken, NewPassword = "anotherPassword2" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the token was already consumed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task ResetPassword_ExpiredToken_ReturnsBadRequest()
        {
            // In this test we check that an expired token is rejected.

            // --- Arrange --- //
            // * We seed a user, request a reset, then backdate the token's expiry into the past.
            SeedUser();
            await _controller.RequestPasswordReset(new RequestPasswordResetRequest { Identifier = "chilly" }, CancellationToken.None);
            string rawToken = ExtractToken(_lastResetLink!);

            PasswordResetToken token = _db.PasswordResetTokens.Single();
            token.ExpiresAt = "2000-01-01T00:00:00.0000000Z";                       // already expired
            _db.SaveChanges();

            // --- Act --- //
            IActionResult result = await _controller.ResetPassword(new ResetPasswordRequest { Token = rawToken, NewPassword = "brandNewPassword1" }, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the token has expired.
            Assert.Equal("OLD_HASH", _db.Users.Single().PasswordHash);             // We expect the password to stay unchanged.
        }



        // --------------- //
        // --- HELPERS --- //
        // --------------- //

        // Inserts a User with a known (dummy) password hash so tests can detect a change.
        private User SeedUser(
            string id = DefaultUserID,
            string username = "chilly",
            string email = "chilly@test.local",
            string? deletedAt = null)
        {
            User user = new User
            {
                ID = id,
                Username = username,
                Email = email,
                PasswordHash = "OLD_HASH",
                PasswordSalt = "OLD_SALT",
                Role = "User",
                DisplayName = "Chilly",
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return user;
        }

        // Pulls the raw token out of a captured reset link (…/Auth/reset?token=XXXX).
        private static string ExtractToken(string link)
        {
            return Uri.UnescapeDataString(link.Split("token=")[1]);
        }

        // Asserts that the email service was never asked to send anything.
        private void AssertNoMailSent()
        {
            _emailMock.Verify(
                x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never());
            Assert.Null(_lastResetLink);
        }
    }
}
