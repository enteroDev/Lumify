/* UserControllerTests
 * Unit tests for every action of: UsersController.
 *
 * xUnit creates a NEW instance of this class for each test, so the constructor below creates
 * a setup per call (fresh in-memory DB, fake presence service, signed-in controller) and
 * Dispose() cleans up.
 *
 * Note: UsersController has no SignalR hub. Instead it depends on IPresenceService to resolve
 * the online-status of users, so we hand it a Moq-stub that reports everyone as "Online".
 */

using lumify.api.Controllers;
using lumify.api.Interfaces;
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.EF;
using lumify.api.Models.Enum;
using lumify.tests.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace lumify.tests.ControllerTests
{
    public class UserControllerTests : IDisposable
    {
        private const string DefaultUserID = "user-1";
        private const string OldTimestamp = "2000-01-01T00:00:00.0000000Z";

        private readonly LumifyDbContext _db;
        private readonly Mock<IPresenceService> _presence;
        private readonly UsersController _controller;


        public UserControllerTests()
        {
            _db = TestDbFactory.Create();

            // We stub the presence service so every user looks "Online" to the controller.
            _presence = new Mock<IPresenceService>();
            _presence.Setup(x => x.GetPresenceStatus(It.IsAny<string>())).Returns(PresenceStatus.Online);

            _controller = new UsersController(NullLogger<UsersController>.Instance, _db, _presence.Object);
            ControllerContextFactory.SignIn(_controller, DefaultUserID);
        }

        // Runs after every single test.
        public void Dispose()
        {
            _db.Dispose();
        }



        // ----------------------- //
        // --- SaveUserProfile --- //
        // ----------------------- //

        [Fact]
        public async Task SaveUserProfile_NullRequest_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We call the endpoint without a request body.
            ActionResult<UserProfileResponse> result = await _controller.SaveUserProfile(null!, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the request is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserProfile_UnknownUser_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We do NOT seed the current user, so the lookup cannot resolve.
            SaveUserProfileRequest request = new SaveUserProfileRequest { DisplayName = "Neu" };

            // --- Act --- //
            // * We try to save a profile for a user that doesn't exist.
            ActionResult<UserProfileResponse> result = await _controller.SaveUserProfile(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserProfile_Valid_TrimsAndPersistsFields()
        {
            // In this test we check that provided fields are trimmed and persisted.

            // --- Arrange --- //
            // * We seed the current user and request a change to every editable field (padded with whitespaces).
            SeedUser();
            SaveUserProfileRequest request = new SaveUserProfileRequest
            {
                DisplayName = "  Chilly  ",
                AvatarUrl = "  /avatars/x.png  ",
                Bio = "  Hallo Welt  "
            };

            // --- Act --- //
            // * We save the profile.
            ActionResult<UserProfileResponse> result = await _controller.SaveUserProfile(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserProfileResponse body = Assert.IsType<UserProfileResponse>(ok.Value); // We expect the body to be a UserProfileResponse.

            Assert.Equal("Chilly", body.DisplayName);                               // We expect the displayName to be trimmed.
            Assert.Equal("/avatars/x.png", body.AvatarUrl);                         // We expect the avatarUrl to be trimmed.
            Assert.Equal("Hallo Welt", body.Bio);                                   // We expect the bio to be trimmed.

            User? stored = ReloadUser(DefaultUserID);                               // We reload the user to inspect what was persisted.
            Assert.Equal("Chilly", stored!.DisplayName);                            // We expect the stored displayName to be updated.
            Assert.NotEqual(stored.CreatedAt, stored.UpdatedAt);                    // We expect UpdatedAt to have advanced past the seeded value.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserProfile_EmptyStrings_NormalizeToNull()
        {
            // In this test we check that provided-but-empty fields are normalized to null.

            // --- Arrange --- //
            // * We seed the user with existing values, then request a save that empties every field.
            User seeded = SeedUser();
            seeded.DisplayName = "Alt";
            seeded.AvatarUrl = "/old.png";
            seeded.Bio = "Alte Bio";
            _db.SaveChanges();

            SaveUserProfileRequest request = new SaveUserProfileRequest { DisplayName = "   ", AvatarUrl = "   ", Bio = "   " };

            // --- Act --- //
            // * We save the profile with whitespace-only fields.
            ActionResult<UserProfileResponse> result = await _controller.SaveUserProfile(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserProfileResponse body = Assert.IsType<UserProfileResponse>(ok.Value); // We expect the body to be a UserProfileResponse.

            Assert.Null(body.DisplayName);                                          // We expect the emptied displayName to be normalized to null.
            Assert.Null(body.AvatarUrl);                                            // We expect the emptied avatarUrl to be normalized to null.
            Assert.Null(body.Bio);                                                  // We expect the emptied bio to be normalized to null.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserProfile_NullFields_LeavesExistingValuesUntouched()
        {
            // In this test we check that fields left null in the request are not changed (partial update).

            // --- Arrange --- //
            // * We seed a user that already has a displayName and request a save that only sets the bio.
            User seeded = SeedUser();
            seeded.DisplayName = "Behalten";
            _db.SaveChanges();

            SaveUserProfileRequest request = new SaveUserProfileRequest { Bio = "Neue Bio" };

            // --- Act --- //
            // * We save the profile, providing only the bio.
            ActionResult<UserProfileResponse> result = await _controller.SaveUserProfile(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserProfileResponse body = Assert.IsType<UserProfileResponse>(ok.Value); // We expect the body to be a UserProfileResponse.

            Assert.Equal("Behalten", body.DisplayName);                             // We expect the untouched displayName to be preserved.
            Assert.Equal("Neue Bio", body.Bio);                                     // We expect the provided bio to be applied.
        }



        // ---------------------- //
        // --- SaveUserAvatar --- //
        // ---------------------- //

        [Fact]
        public async Task SaveUserAvatar_NoFile_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We call the endpoint without a file.
            ActionResult<string> result = await _controller.SaveUserAvatar(null!, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since a file is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserAvatar_DisallowedContentType_ReturnsBadRequest()
        {
            // In this test we check that only image content types are accepted.

            // --- Arrange --- //
            // * We build an upload that claims to be a PDF.
            IFormFile file = CreateUpload("dokument.pdf", "application/pdf");

            // --- Act --- //
            // * We try to upload the non-image file.
            ActionResult<string> result = await _controller.SaveUserAvatar(file, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since only image files are allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveUserAvatar_UnknownUser_ReturnsNotFound()
        {
            // In this test we check that a valid image for a non-existing user is rejected before any disk write.

            // --- Arrange --- //
            // * We provide a valid image, but do NOT seed the current user.
            IFormFile file = CreateUpload("avatar.png", "image/png");

            // --- Act --- //
            // * We try to upload the avatar for a user that doesn't exist.
            ActionResult<string> result = await _controller.SaveUserAvatar(file, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user could not be resolved.
        }



        // --------------------- //
        // --- DeleteAccount --- //
        // --------------------- //

        [Fact]
        public async Task DeleteAccount_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete the account of a user that doesn't exist.
            IActionResult result = await _controller.DeleteAccount(CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteAccount_Valid_SoftDeletes()
        {
            // In this test we check that deleting the own account only soft-deletes the user (the row stays present).

            // --- Arrange --- //
            // * We seed the current user.
            SeedUser();

            // --- Act --- //
            // * We delete the account.
            IActionResult result = await _controller.DeleteAccount(CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadUser(DefaultUserID)!.DeletedAt);                   // We expect DeletedAt to be set -> soft-deleted, row still present.
        }



        // ----------------------------- //
        // --- GetUserProfileWithID ---- //
        // ----------------------------- //

        [Fact]
        public async Task GetUserProfileWithID_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for a profile, but pass only whitespace as the userID.
            ActionResult<UserProfileResponse> result = await _controller.GetUserProfileWithID("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the userID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserProfileWithID_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for a profile of a user that doesn't exist.
            ActionResult<UserProfileResponse> result = await _controller.GetUserProfileWithID("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserProfileWithID_DeletedUser_ReturnsNotFound()
        {
            // In this test we check that soft-deleted users are hidden from the profile lookup.

            // --- Arrange --- //
            // * We seed a user that is already soft-deleted.
            SeedUser("other", deletedAt: OldTimestamp);

            // --- Act --- //
            // * We ask for the profile of the deleted user.
            ActionResult<UserProfileResponse> result = await _controller.GetUserProfileWithID("other", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user is soft-deleted.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserProfileWithID_Valid_ReturnsProfile()
        {
            // --- Arrange --- //
            // * We seed another user.
            SeedUser("other");

            // --- Act --- //
            // * We ask for that user's profile.
            ActionResult<UserProfileResponse> result = await _controller.GetUserProfileWithID("other", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserProfileResponse body = Assert.IsType<UserProfileResponse>(ok.Value); // We expect the body to be a UserProfileResponse.
            Assert.Equal("other", body.ID);                                         // We expect to get back the requested user.
        }



        // -------------------------------- //
        // --- GetUserAccountInfoWithID --- //
        // -------------------------------- //

        [Fact]
        public async Task GetUserAccountInfoWithID_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for account info, but pass only whitespace as the userID.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserAccountInfoWithID(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the userID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserAccountInfoWithID_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for account info of a user that doesn't exist.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserAccountInfoWithID("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserAccountInfoWithID_Valid_ReturnsAccountInfo()
        {
            // --- Arrange --- //
            // * We seed another user.
            SeedUser("other");

            // --- Act --- //
            // * We ask for that user's account info.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserAccountInfoWithID("other", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserAccountInfoResponse body = Assert.IsType<UserAccountInfoResponse>(ok.Value); // We expect a UserAccountInfoResponse.
            Assert.Equal("other", body.ID);                                         // We expect to get back the requested user.
            Assert.Equal("other@test.local", body.Email);                           // We expect the seeded email to be carried over.
        }



        // ----------------------------- //
        // --- GetUserPreviewWithID ---- //
        // ----------------------------- //

        [Fact]
        public async Task GetUserPreviewWithID_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for a preview, but pass only whitespace as the userID.
            ActionResult<UserPreviewResponse> result = await _controller.GetUserPreviewWithID("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the userID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserPreviewWithID_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for a preview of a user that doesn't exist.
            ActionResult<UserPreviewResponse> result = await _controller.GetUserPreviewWithID("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserPreviewWithID_Valid_ReturnsPreviewWithPresence()
        {
            // In this test we check that a preview is returned and enriched with the presence status.

            // --- Arrange --- //
            // * We seed another user. (The presence stub reports "Online" for everyone.)
            SeedUser("other");

            // --- Act --- //
            // * We ask for that user's preview.
            ActionResult<UserPreviewResponse> result = await _controller.GetUserPreviewWithID("other", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserPreviewResponse body = Assert.IsType<UserPreviewResponse>(ok.Value); // We expect a UserPreviewResponse.
            Assert.Equal("other", body.ID);                                         // We expect to get back the requested user.
            Assert.Equal(PresenceStatus.Online, body.PresenceStatus);               // We expect the presence status to be filled in from the service.
        }



        // ----------------------- //
        // --- GetUserProfile ---- //
        // ----------------------- //

        [Fact]
        public async Task GetUserProfile_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the current user's profile without seeding that user.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserProfile(CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the current user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserProfile_Valid_ReturnsOwnProfile()
        {
            // --- Arrange --- //
            // * We seed the current user.
            SeedUser();

            // --- Act --- //
            // * We ask for the current user's own profile.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserProfile(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserProfileResponse body = Assert.IsType<UserProfileResponse>(ok.Value); // We expect a UserProfileResponse.
            Assert.Equal(DefaultUserID, body.ID);                                   // We expect to get back the current user.
        }



        // --------------------------- //
        // --- GetUserAccountInfo ---- //
        // --------------------------- //

        [Fact]
        public async Task GetUserAccountInfo_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the current user's account info without seeding that user.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserAccountInfo(CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the current user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetUserAccountInfo_Valid_ReturnsOwnAccountInfo()
        {
            // --- Arrange --- //
            // * We seed the current user.
            SeedUser();

            // --- Act --- //
            // * We ask for the current user's own account info.
            ActionResult<UserAccountInfoResponse> result = await _controller.GetUserAccountInfo(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            UserAccountInfoResponse body = Assert.IsType<UserAccountInfoResponse>(ok.Value); // We expect a UserAccountInfoResponse.
            Assert.Equal(DefaultUserID, body.ID);                                   // We expect to get back the current user.
        }



        // -------------------------- //
        // --- GetAvatarOfUser ------ //
        // -------------------------- //

        [Fact]
        public async Task GetAvatarOfUser_UnknownUser_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the current user's avatar without seeding that user.
            ActionResult<string> result = await _controller.GetAvatarOfUser(CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the current user could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAvatarOfUser_Valid_ReturnsAvatarUrl()
        {
            // --- Arrange --- //
            // * We seed the current user with an avatar url.
            User seeded = SeedUser();
            seeded.AvatarUrl = "/Data/avatars/avatar_user-1.png";
            _db.SaveChanges();

            // --- Act --- //
            // * We ask for the current user's avatar.
            ActionResult<string> result = await _controller.GetAvatarOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            Assert.Equal("/Data/avatars/avatar_user-1.png", ok.Value);              // We expect to get back the stored avatar url.
        }



        // ------------------------ //
        // --- GetRelatedUsers ---- //
        // ------------------------ //

        [Fact]
        public async Task GetRelatedUsers_NoSharedWorkspaces_ReturnsEmpty()
        {
            // In this test we check that a user without shared workspaces gets an empty list.

            // --- Arrange --- //
            // * We seed only the current user, with no workspace memberships.
            SeedUser();

            // --- Act --- //
            // * We ask for related users.
            IActionResult result = await _controller.GetRelatedUsers(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<UserPreviewResponse> users = Assert.IsAssignableFrom<List<UserPreviewResponse>>(ok.Value); // We expect a list of previews.
            Assert.Empty(users);                                                    // We expect no related users, since there is no shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetRelatedUsers_ReturnsMembersOfSharedWorkspaces_ExcludingSelf()
        {
            // In this test we check that members of shared workspaces are returned, but not the current user himself.

            // --- Arrange --- //
            // * We seed a workspace and add the current user, a colleague and a deleted user as members.
            SeedUser();
            SeedUser("colleague");
            SeedUser("ghost", deletedAt: OldTimestamp);
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedMember("ws-1", "colleague");
            SeedMember("ws-1", "ghost");

            // --- Act --- //
            // * We ask for related users.
            IActionResult result = await _controller.GetRelatedUsers(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<UserPreviewResponse> users = Assert.IsAssignableFrom<List<UserPreviewResponse>>(ok.Value); // We expect a list of previews.

            Assert.Single(users);                                                   // We expect exactly the one active colleague.
            Assert.Equal("colleague", users[0].ID);                                 // We expect that colleague to be returned.
            Assert.Equal(PresenceStatus.Online, users[0].PresenceStatus);           // We expect the presence status to be filled in.
        }



        // ------------------------------------ //
        // --- Get5LastModifiedTodosOfUser ---- //
        // ------------------------------------ //

        [Fact]
        public async Task Get5LastModifiedTodosOfUser_ReturnsOnlyOwnActiveTodos_OrderedByRecency_Max5()
        {
            // In this test we check that only the user's own, active todos are returned, newest first and capped at 5.

            // --- Arrange --- //
            // * We seed six own todos with ascending UpdatedAt, plus a foreign and a deleted one (both excluded).
            SeedUser();
            for (int i = 1; i <= 6; i++)
            {
                SeedTodoList("t" + i, DefaultUserID, updatedAt: "2026-06-1" + i + "T00:00:00Z");
            }
            SeedTodoList("foreign", "other");
            SeedTodoList("deleted", DefaultUserID, deletedAt: OldTimestamp);

            // --- Act --- //
            // * We ask for the 5 most recently modified todos.
            IActionResult result = await _controller.Get5LastModifiedTodosOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<TodoListResponse> todos = Assert.IsAssignableFrom<List<TodoListResponse>>(ok.Value); // We expect a list of todos.

            Assert.Equal(5, todos.Count);                                           // We expect the result to be capped at 5.
            Assert.Equal("t6", todos[0].ID);                                        // We expect the most recently modified todo first.
            Assert.Equal("t2", todos[4].ID);                                        // We expect the oldest of the top 5 last.
        }



        // -------------------------------------- //
        // --- GetLast5ModifiedEventsOfUser ----- //
        // -------------------------------------- //

        [Fact]
        public async Task GetLast5ModifiedEventsOfUser_ReturnsOnlyOwnActiveEvents_OrderedByRecency_Max5()
        {
            // In this test we check that only the user's own, active events are returned, newest first and capped at 5.

            // --- Arrange --- //
            // * We seed six own events with ascending UpdatedAt, plus a foreign and a deleted one (both excluded).
            SeedUser();
            for (int i = 1; i <= 6; i++)
            {
                SeedEvent("e" + i, DefaultUserID, updatedAt: "2026-06-1" + i + "T00:00:00Z");
            }
            SeedEvent("foreign", "other");
            SeedEvent("deleted", DefaultUserID, deletedAt: OldTimestamp);

            // --- Act --- //
            // * We ask for the 5 most recently modified events.
            IActionResult result = await _controller.GetLast5ModifiedEventsOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<EventResponse> events = Assert.IsAssignableFrom<List<EventResponse>>(ok.Value); // We expect a list of events.

            Assert.Equal(5, events.Count);                                          // We expect the result to be capped at 5.
            Assert.Equal("e6", events[0].ID);                                       // We expect the most recently modified event first.
        }



        // ------------------------------------- //
        // --- GetLast5ModifiedNotesOfUser ----- //
        // ------------------------------------- //

        [Fact]
        public async Task GetLast5ModifiedNotesOfUser_ReturnsOnlyOwnActiveNotes_OrderedByRecency_Max5()
        {
            // In this test we check that only the user's own, active notes are returned, newest first and capped at 5.

            // --- Arrange --- //
            // * We seed six own notes with ascending UpdatedAt, plus a foreign and a deleted one (both excluded).
            SeedUser();
            for (int i = 1; i <= 6; i++)
            {
                SeedNote("n" + i, DefaultUserID, updatedAt: "2026-06-1" + i + "T00:00:00Z");
            }
            SeedNote("foreign", "other");
            SeedNote("deleted", DefaultUserID, deletedAt: OldTimestamp);

            // --- Act --- //
            // * We ask for the 5 most recently modified notes.
            IActionResult result = await _controller.GetLast5ModifiedNotesOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<NoteResponse> notes = Assert.IsAssignableFrom<List<NoteResponse>>(ok.Value); // We expect a list of notes.

            Assert.Equal(5, notes.Count);                                           // We expect the result to be capped at 5.
            Assert.Equal("n6", notes[0].ID);                                        // We expect the most recently modified note first.
        }



        // -------------------- //
        // --- SearchUsers ---- //
        // -------------------- //

        [Fact]
        public async Task SearchUsers_EmptyQuery_ReturnsEmpty()
        {
            // In this test we check that an empty query short-circuits to an empty list.

            // --- Arrange --- //
            // * We seed a user that would match, but search with a blank query.
            SeedUser();
            SeedUser("other");

            // --- Act --- //
            // * We search with a whitespace-only query.
            IActionResult result = await _controller.SearchUsers("   ", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<UserPreviewResponse> users = Assert.IsAssignableFrom<List<UserPreviewResponse>>(ok.Value); // We expect a list of previews.
            Assert.Empty(users);                                                    // We expect no results for a blank query.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SearchUsers_MatchesByUsername_ExcludingSelfAndDeleted()
        {
            // In this test we check that the search matches by username and excludes the current user and deleted users.

            // --- Arrange --- //
            // * We seed the current user plus a matching, a non-matching and a deleted-but-matching user.
            SeedUser();                                                             // username: user_user-1
            SeedUser("match");                                                      // username: user_match
            SeedUser("nope");                                                       // username: user_nope
            SeedUser("ghost", deletedAt: OldTimestamp);                             // username: user_ghost (deleted)

            // --- Act --- //
            // * We search for "match".
            IActionResult result = await _controller.SearchUsers("match", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<UserPreviewResponse> users = Assert.IsAssignableFrom<List<UserPreviewResponse>>(ok.Value); // We expect a list of previews.

            Assert.Single(users);                                                   // We expect exactly the one matching, active, non-self user.
            Assert.Equal("match", users[0].ID);                                     // We expect that user to be returned.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SearchUsers_DoesNotReturnCurrentUser()
        {
            // In this test we check that a query matching the current user never returns himself.

            // --- Arrange --- //
            // * We seed only the current user and search for his own username.
            SeedUser();

            // --- Act --- //
            // * We search for the current user's own username.
            IActionResult result = await _controller.SearchUsers("user_user-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<UserPreviewResponse> users = Assert.IsAssignableFrom<List<UserPreviewResponse>>(ok.Value); // We expect a list of previews.
            Assert.Empty(users);                                                    // We expect the current user to be excluded from his own search.
        }



        // ------------------------------------------- //
        // --- SearchAvailableUsersForWorkspace ------ //
        // ------------------------------------------- //

        [Fact]
        public async Task SearchAvailableUsersForWorkspace_MissingWorkspaceId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We search, but pass only whitespace as the workspaceID.
            IActionResult result = await _controller.SearchAvailableUsersForWorkspace("  ", "x", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SearchAvailableUsersForWorkspace_EmptyQuery_ReturnsEmpty()
        {
            // In this test we check that a blank query short-circuits to an empty list.

            // --- Act --- //
            // * We search with a valid workspaceID but a whitespace-only query.
            IActionResult result = await _controller.SearchAvailableUsersForWorkspace("ws-1", "   ", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<RelatedUserResponse> users = Assert.IsAssignableFrom<List<RelatedUserResponse>>(ok.Value); // We expect a list of related users.
            Assert.Empty(users);                                                    // We expect no results for a blank query.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SearchAvailableUsersForWorkspace_UnknownWorkspace_ReturnsNotFound()
        {
            // --- Act --- //
            // * We search inside a workspace that doesn't exist.
            IActionResult result = await _controller.SearchAvailableUsersForWorkspace("ghost-ws", "match", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the workspace could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SearchAvailableUsersForWorkspace_ExcludesSelfAndExistingMembers()
        {
            // In this test we check that the search returns only users that can still be added:
            // not the current user, and not users that are already members of the workspace.

            // --- Arrange --- //
            // * We seed a workspace with the current user and an existing member, plus a still-addable matching user.
            SeedUser();
            SeedUser("matchA");                                                     // already a member -> excluded
            SeedUser("matchB");                                                     // addable -> expected
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedMember("ws-1", "matchA");

            // --- Act --- //
            // * We search for users containing "match" that can join the workspace.
            IActionResult result = await _controller.SearchAvailableUsersForWorkspace("ws-1", "match", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);              // We expect to get a success status.
            List<RelatedUserResponse> users = Assert.IsAssignableFrom<List<RelatedUserResponse>>(ok.Value); // We expect a list of related users.

            Assert.Single(users);                                                   // We expect exactly the one addable user.
            Assert.Equal("matchB", users[0].UserID);                                // We expect the non-member to be returned.
            Assert.Equal(PresenceStatus.Online, users[0].PresenceStatus);           // We expect the presence status to be filled in.
        }



        // --------------- //
        // --- HELPERS --- //
        // --------------- //

        // Inserts a User. Username is derived from the id so the search-tests can match on it.
        private User SeedUser(string id = DefaultUserID, string? deletedAt = null)
        {
            User user = new User
            {
                ID = id,
                Username = "user_" + id,
                Email = id + "@test.local",
                PasswordHash = "hash",
                Role = "User",
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return user;
        }

        // Inserts a Workspace.
        private Workspace SeedWorkspace(string id, string ownerID = DefaultUserID, string? deletedAt = null)
        {
            Workspace workspace = new Workspace
            {
                ID = id,
                OwnerID = ownerID,
                Name = "workspace_" + id,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Workspaces.Add(workspace);
            _db.SaveChanges();

            return workspace;
        }

        // Inserts a WorkspaceMember (used by the related/available-user queries).
        private WorkspaceMember SeedMember(string workspaceID, string userID, string? deletedAt = null)
        {
            WorkspaceMember member = new WorkspaceMember
            {
                ID = Guid.NewGuid().ToString(),
                WorkspaceID = workspaceID,
                UserID = userID,
                Role = 1,
                CreatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.WorkspaceMembers.Add(member);
            _db.SaveChanges();

            return member;
        }

        // Inserts a TodoList.
        private TodoList SeedTodoList(string id, string ownerID = DefaultUserID, string? updatedAt = null, string? deletedAt = null)
        {
            TodoList todo = new TodoList
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = null,
                Name = "todo_" + id,
                Status = 1,
                IsArchived = 0,
                CreatedAt = OldTimestamp,
                UpdatedAt = updatedAt ?? OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.TodoLists.Add(todo);
            _db.SaveChanges();

            return todo;
        }

        // Inserts an Event.
        private Event SeedEvent(string id, string ownerID = DefaultUserID, string? updatedAt = null, string? deletedAt = null)
        {
            Event calendarEvent = new Event
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = null,
                Name = "event_" + id,
                Description = null,
                Status = 1,
                StartDate = "2026-06-10T09:00:00.0000000Z",
                EndDate = "2026-06-10T10:00:00.0000000Z",
                IsAllDay = 0,
                DueDate = null,
                CreatedAt = OldTimestamp,
                UpdatedAt = updatedAt ?? OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Events.Add(calendarEvent);
            _db.SaveChanges();

            return calendarEvent;
        }

        // Inserts a Note.
        private Note SeedNote(string id, string ownerID = DefaultUserID, string? updatedAt = null, string? deletedAt = null)
        {
            Note note = new Note
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = null,
                FolderID = null,
                Name = "note_" + id,
                CreatedAt = OldTimestamp,
                UpdatedAt = updatedAt ?? OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Notes.Add(note);
            _db.SaveChanges();

            return note;
        }

        // Reloads a User from the context so a test can inspect what was persisted.
        private User? ReloadUser(string id)
        {
            return _db.Users.Find(id);
        }

        // Builds an in-memory IFormFile upload with the given name and content type.
        private static IFormFile CreateUpload(string fileName, string contentType)
        {
            byte[] bytes = new byte[] { 1, 2, 3 };
            MemoryStream stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
