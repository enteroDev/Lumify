/* NotesControllerTests
 * Unit tests for every action of: NotesController.
 *
 * xUnit creates a NEW instance of this class for each test, so the constructor below creates
 * a setup per call (fresh in-memory DB, fake hub, signed-in controller) and Dispose() cleans up.
 */

using lumify.api.Controllers;
using lumify.api.Hubs;
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.EF;
using lumify.tests.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace lumify.tests.ControllerTests
{
    public class NotesControllerTests : IDisposable
    {
        private const string DefaultUserID = "user-1";
        private const string OldTimestamp = "2000-01-01T00:00:00.0000000Z";

        private readonly LumifyDbContext _db;
        private readonly IHubContext<NoteHub> _hub;
        private readonly Mock<IClientProxy> _hubSpy;
        private readonly NotesController _controller;


        public NotesControllerTests()
        {
            _db = TestDbFactory.Create();

            (IHubContext<NoteHub> hub, Mock<IClientProxy> spy) = SignalRMock.Create<NoteHub>();
            _hub = hub;
            _hubSpy = spy;

            _controller = new NotesController(NullLogger<NotesController>.Instance, _db, _hub);
            ControllerContextFactory.SignIn(_controller, DefaultUserID);
        }

        // Runs after every single test.
        public void Dispose()
        {
            _db.Dispose();
        }




        // --------------- //
        // --- AddNote --- //
        // --------------- //

        [Fact]
        public async Task AddNote_MissingName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request that has only whitespaces for the name-property.
            AddNoteRequest request = new AddNoteRequest { Name = "   " };

            // --- Act --- //
            // * We send the request with an empty name.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                  // We expect a BadRequest, since the name is required.
            Assert.Empty(_db.Notes);                                               // We expect the db to be empty, since a request without name shouldn't be saved.
            SignalRMock.AssertSilent(_hubSpy);                                     // We expect SignalR to stay silent, since nothing got created.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_UnknownWorkspace_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request to add a note into a non existing workspace.
            AddNoteRequest request = new AddNoteRequest { Name = "Notiz", WorkspaceID = "ghost-ws" };

            // --- Act --- //
            // * We try to add the note into the non existing workspace.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspace does not exist.
            Assert.Empty(_db.Notes);                                                // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_NotWorkspaceMember_ReturnsForbid()
        {
            // In this test we check that a user cannot add a note to a workspace he is not a member of.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");
            AddNoteRequest request = new AddNoteRequest { Name = "Notiz", WorkspaceID = "ws-1" };

            // --- Act --- //
            // * We (non-member) try to add the note into the workspace.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
            Assert.Empty(_db.Notes);                                                // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_UnknownFolder_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request to add a personal note into a non existing folder.
            AddNoteRequest request = new AddNoteRequest { Name = "Notiz", FolderID = "ghost-folder" };

            // --- Act --- //
            // * We try to add the note into the non existing folder.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the folder could not be found.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_FolderInDifferentSpace_ReturnsBadRequest()
        {
            // In this test we check that a personal note cannot be put into a workspace-folder.

            // --- Arrange --- //
            // * We seed a workspace and a folder that lives inside that workspace.
            // * We create a request for a PERSONAL note (no workspace) pointing to that workspace-folder.
            SeedWorkspace("ws-1");
            SeedFolder("f-1", DefaultUserID, "ws-1");
            AddNoteRequest request = new AddNoteRequest { Name = "Notiz", FolderID = "f-1" };

            // --- Act --- //
            // * We try to add the personal note into the workspace-folder.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the folder is in a different space.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_PersonalFolderNotOwned_ReturnsForbid()
        {
            // In this test we check that a user cannot add a note into someone else's private folder.

            // --- Arrange --- //
            // * We seed a personal folder (no workspace) that belongs to another user.
            // * We create a request for a personal note pointing to that foreign folder.
            SeedFolder("f-1", "someone-else");
            AddNoteRequest request = new AddNoteRequest { Name = "Notiz", FolderID = "f-1" };

            // --- Act --- //
            // * We (the DefaultUser) try to add the note into the foreign private folder.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since the private folder belongs to another user.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_Personal_PersistsAndReturnsResponse_NoBroadcast()
        {
            // In this test we check if a personal note is persistent
            // and is not getting shared via SignalR, since it gets added into the private space.

            // --- Arrange --- //
            // * We provide a valid request for adding a personal note (name padded with whitespaces).
            AddNoteRequest request = new AddNoteRequest { Name = "  Meine Notiz  " };

            // --- Act --- //
            // * We add the note to the private space.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            NoteResponse body = Assert.IsType<NoteResponse>(ok.Value);              // We expect the response to be type NoteResponse.

            Assert.Equal("Meine Notiz", body.Name);                                 // We expect the name to be trimmed.
            Assert.Equal(DefaultUserID, body.OwnerID);                              // We expect to get the DefaultUser (TestUser) as owner.
            Assert.Null(body.WorkspaceID);                                          // We expect the workspace to be null, since we add into the private space.
            Assert.Null(body.FolderID);                                             // We expect the folder to be null, since we didn't provide one.
            Assert.False(string.IsNullOrWhiteSpace(body.ID));                       // We expect the ID to contain a valid value.

            Note? stored = ReloadNote(body.ID);                                     // We check if the note is existent/persistent in the database.
            Assert.NotNull(stored);                                                 // We expect the note to be present and NotNull.
            Assert.Equal("Meine Notiz", stored!.Name);                              // We expect the stored note to keep the trimmed name.

            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since the note is private.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddNote_InWorkspace_BroadcastsCreated()
        {
            // In this test we check that a note added to a workspace gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and add the current user as a member.
            // * We create a request to add a note into this workspace.
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            AddNoteRequest request = new AddNoteRequest { Name = "Sprint Notiz", WorkspaceID = "ws-1" };

            // --- Act --- //
            // * We add the note to the workspace.
            ActionResult<NoteResponse> result = await _controller.AddNote(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            NoteResponse body = Assert.IsType<NoteResponse>(ok.Value);              // We expect the response to be type NoteResponse.
            Assert.Equal("ws-1", body.WorkspaceID);                                 // We expect the note to belong to our workspace.

            SignalRMock.AssertBroadcast(_hubSpy, "NoteCreated", Times.Once());      // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------- //
        // --- AddTextBlock --- //
        // -------------------- //

        [Fact]
        public async Task AddTextBlock_MissingNoteId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request without a NoteID.
            AddTextBlockRequest request = new AddTextBlockRequest { NoteID = "  ", Type = 0 };

            // --- Act --- //
            // * We try to add the block without telling to which note it belongs.
            ActionResult<TextblockResponse> result = await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the NoteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTextBlock_UnknownNote_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a request that points to a note which doesn't exist.
            AddTextBlockRequest request = new AddTextBlockRequest { NoteID = "ghost", Type = 0 };

            // --- Act --- //
            // * We try to add the block to the non existing note.
            ActionResult<TextblockResponse> result = await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the parent note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTextBlock_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot add a block to a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note that belongs to another user.
            SeedNote("n-1", "someone-else");
            AddTextBlockRequest request = new AddTextBlockRequest { NoteID = "n-1", Type = 0 };

            // --- Act --- //
            // * We (the DefaultUser) try to add a block to the foreign note.
            ActionResult<TextblockResponse> result = await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTextBlock_Valid_PersistsAndAssignsNextNotePos()
        {
            // In this test we check that blocks get persisted with trimmed fields and an auto-incremented position.

            // --- Arrange --- //
            // * We seed our own note that already holds one block at position 0.
            // * We create a request for a second block (name and codeLanguage padded with whitespaces).
            SeedNote("n-1");
            SeedTextBlock("tb-0", "n-1", 0, 0);
            AddTextBlockRequest request = new AddTextBlockRequest
            {
                NoteID = "n-1",
                Type = 1,
                Name = "  Title  ",
                Content = "Hello World",
                CodeLanguage = "  csharp  "
            };

            // --- Act --- //
            // * We add the second block to the note.
            ActionResult<TextblockResponse> result = await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TextblockResponse body = Assert.IsType<TextblockResponse>(ok.Value);    // We expect the response to be type TextblockResponse.

            Assert.Equal("n-1", body.NoteID);                                       // We expect the block to belong to our note.
            Assert.Equal(1, body.Type);                                             // We expect the type to be taken from the request.
            Assert.Equal("Title", body.Name);                                       // We expect the name to be trimmed.
            Assert.Equal("Hello World", body.Content);                              // We expect the content to be stored as provided.
            Assert.Equal("csharp", body.CodeLanguage);                              // We expect the code language to be trimmed.
            Assert.False(body.IsCollapsed);                                         // We expect a new block to be expanded (not collapsed) by default.
            Assert.Equal(1, body.NotePos);                                          // We expect the position to be the next free slot (0 was taken).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTextBlock_InWorkspaceNote_BroadcastsCreated()
        {
            // In this test we check that a block added to a workspace-note gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and our own note inside that workspace.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            AddTextBlockRequest request = new AddTextBlockRequest { NoteID = "n-1", Type = 0 };

            // --- Act --- //
            // * We add a block to the workspace-note.
            ActionResult<TextblockResponse> result = await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                            // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "TextBlockCreated", Times.Once());  // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTextBlock_PersonalNote_NoBroadcast()
        {
            // In this test we check that a block added to a personal note does NOT get broadcasted.

            // --- Arrange --- //
            // * We seed our own personal note (no workspace).
            SeedNote("n-1");
            AddTextBlockRequest request = new AddTextBlockRequest { NoteID = "n-1", Type = 0 };

            // --- Act --- //
            // * We add a block to the personal note.
            await _controller.AddTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since the note is private.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------- //
        // --- AddLinkItem --- //
        // ------------------- //

        [Fact]
        public async Task AddLinkItem_MissingNoteId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request without a NoteID.
            AddLinkItemRequest request = new AddLinkItemRequest { NoteID = "  ", Url = "https://example.com" };

            // --- Act --- //
            // * We try to add the link without telling to which note it belongs.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the NoteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddLinkItem_UnknownNote_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a request that points to a note which doesn't exist.
            AddLinkItemRequest request = new AddLinkItemRequest { NoteID = "ghost", Url = "https://example.com" };

            // --- Act --- //
            // * We try to add the link to the non existing note.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the parent note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddLinkItem_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot add a link to a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note that belongs to another user.
            SeedNote("n-1", "someone-else");
            AddLinkItemRequest request = new AddLinkItemRequest { NoteID = "n-1", Url = "https://example.com" };

            // --- Act --- //
            // * We (the DefaultUser) try to add a link to the foreign note.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddLinkItem_MissingUrl_ReturnsBadRequest()
        {
            // In this test we check that a link without a url is rejected (on an otherwise valid, owned note).

            // --- Arrange --- //
            // * We seed our own note and create a request that has only whitespaces for the url.
            SeedNote("n-1");
            AddLinkItemRequest request = new AddLinkItemRequest { NoteID = "n-1", Url = "   " };

            // --- Act --- //
            // * We try to add the link without a valid url.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the url is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddLinkItem_Valid_PersistsAndAssignsNextNotePos()
        {
            // In this test we check that links get persisted with trimmed fields and an auto-incremented position.

            // --- Arrange --- //
            // * We seed our own note that already holds one link at position 0.
            // * We create a request for a second link (label and url padded with whitespaces).
            SeedNote("n-1");
            SeedLinkItem("li-0", "n-1", "https://first.com", 0);
            AddLinkItemRequest request = new AddLinkItemRequest
            {
                NoteID = "n-1",
                Label = "  Docs  ",
                Url = "  https://example.com  "
            };

            // --- Act --- //
            // * We add the second link to the note.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            LinkItemResponse body = Assert.IsType<LinkItemResponse>(ok.Value);      // We expect the response to be type LinkItemResponse.

            Assert.Equal("n-1", body.NoteID);                                       // We expect the link to belong to our note.
            Assert.Equal("Docs", body.Label);                                       // We expect the label to be trimmed.
            Assert.Equal("https://example.com", body.Url);                          // We expect the url to be trimmed.
            Assert.Equal(1, body.NotePos);                                          // We expect the position to be the next free slot (0 was taken).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddLinkItem_InWorkspaceNote_BroadcastsCreated()
        {
            // In this test we check that a link added to a workspace-note gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and our own note inside that workspace.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            AddLinkItemRequest request = new AddLinkItemRequest { NoteID = "n-1", Url = "https://example.com" };

            // --- Act --- //
            // * We add a link to the workspace-note.
            ActionResult<LinkItemResponse> result = await _controller.AddLinkItem(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "LinkItemCreated", Times.Once());  // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ---------------- //
        // --- SaveNote --- //
        // ---------------- //

        [Fact]
        public async Task SaveNote_MissingId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a save-request without an ID.
            SaveNoteRequest request = new SaveNoteRequest { ID = "" };

            // --- Act --- //
            // * We try to save without telling which note should be updated.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the ID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a save-request for a note-ID that doesn't exist.
            SaveNoteRequest request = new SaveNoteRequest { ID = "ghost" };

            // --- Act --- //
            // * We try to save the non existing note.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot edit a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note that belongs to another user.
            SeedNote("n-1", "someone-else");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", Name = "Hacked" };

            // --- Act --- //
            // * We (the DefaultUser) try to save the foreign note.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed to edit.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_EmptyName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own note and create a request that tries to set the name to whitespaces only.
            SeedNote("n-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", Name = "   " };

            // --- Act --- //
            // * We try to save the note with an empty name.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_UnknownTargetFolder_ReturnsBadRequest()
        {
            // In this test we check that moving a note into a non existing folder is rejected.

            // --- Arrange --- //
            // * We seed our own personal note and request a move into a folder that doesn't exist.
            SeedNote("n-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", FolderID = "ghost-folder" };

            // --- Act --- //
            // * We try to move the note into the non existing folder.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the target folder could not be found.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_PersonalNoteToForeignFolder_ReturnsForbid()
        {
            // In this test we check that a personal note cannot be moved into someone else's private folder.

            // --- Arrange --- //
            // * We seed our own personal note and a private folder owned by another user.
            SeedNote("n-1");
            SeedFolder("f-1", "someone-else");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", FolderID = "f-1" };

            // --- Act --- //
            // * We try to move our note into the foreign private folder.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since the private folder belongs to another user.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_PersonalNoteToWorkspaceFolder_ReturnsBadRequest()
        {
            // In this test we check that a personal note cannot be moved into a workspace-folder.

            // --- Arrange --- //
            // * We seed our own personal note, a workspace and a folder inside that workspace.
            SeedNote("n-1");
            SeedWorkspace("ws-1");
            SeedFolder("f-1", DefaultUserID, "ws-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", FolderID = "f-1" };

            // --- Act --- //
            // * We try to move our personal note into the workspace-folder.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the folder is in a different space.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_WorkspaceNoteToDifferentWorkspaceFolder_ReturnsBadRequest()
        {
            // In this test we check that a workspace-note cannot be moved into a folder of another workspace.

            // --- Arrange --- //
            // * We seed two workspaces, a note in the first and a folder in the second.
            SeedWorkspace("ws-1");
            SeedWorkspace("ws-2");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedFolder("f-1", DefaultUserID, "ws-2");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", FolderID = "f-1" };

            // --- Act --- //
            // * We try to move the note into the folder of the other workspace.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the folder is in a different workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_MoveToRoot_PersistsNullFolder()
        {
            // In this test we check that passing an empty FolderID moves the note back to the root.

            // --- Arrange --- //
            // * We seed our own folder and a note that currently lives inside that folder.
            // * We request a move to root by passing an empty FolderID.
            SeedFolder("f-1");
            SeedNote("n-1", DefaultUserID, null, "f-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", FolderID = "" };

            // --- Act --- //
            // * We save the note with the empty FolderID.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            NoteResponse body = Assert.IsType<NoteResponse>(ok.Value);              // We expect the response to be type NoteResponse.
            Assert.Null(body.FolderID);                                             // We expect the folder to be cleared (moved to root).

            Note? stored = ReloadNote("n-1");                                       // We reload the note to inspect what was persisted.
            Assert.Null(stored!.FolderID);                                          // We expect the stored folder to be null.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_Rename_PersistsAndTouchesTimestamp()
        {
            // In this test we check that a real rename gets persisted and the UpdatedAt timestamp advances.

            // --- Arrange --- //
            // * We seed our own note and request a rename.
            SeedNote("n-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", Name = "Renamed" };

            // --- Act --- //
            // * We save the renamed note.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            NoteResponse body = Assert.IsType<NoteResponse>(ok.Value);              // We expect the response to be type NoteResponse.
            Assert.Equal("Renamed", body.Name);                                     // We expect the response to carry the new name.

            Note? stored = ReloadNote("n-1");                                       // We reload the note to inspect what was persisted.
            Assert.Equal("Renamed", stored!.Name);                                  // We expect the stored name to be updated.
            Assert.NotEqual(stored.CreatedAt, stored.UpdatedAt);                    // We expect UpdatedAt to have advanced past the seeded value.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_NoEffectiveChange_DoesNotTouchTimestampOrBroadcast()
        {
            // In this test we check that saving without a real change neither touches the timestamp nor broadcasts.

            // --- Arrange --- //
            // * We seed a workspace and a note inside it.
            // * We request a save that reuses the seeded name -> nothing actually changes.
            SeedWorkspace("ws-1");
            Note seeded = SeedNote("n-1", DefaultUserID, "ws-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", Name = seeded.Name };

            // --- Act --- //
            // * We save the note without any effective change.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect a success status, even though nothing changed.

            Note? stored = ReloadNote("n-1");                                       // We reload the note to inspect its timestamp.
            Assert.Equal(OldTimestamp, stored!.UpdatedAt);                          // We expect UpdatedAt to stay untouched (still the seeded value).
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since there was no change to share.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveNote_WorkspaceChange_BroadcastsUpdated()
        {
            // In this test we check that a real change on a workspace-note gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and a note inside it, then request a rename.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SaveNoteRequest request = new SaveNoteRequest { ID = "n-1", Name = "Renamed" };

            // --- Act --- //
            // * We save the renamed workspace-note.
            ActionResult<NoteResponse> result = await _controller.SaveNote(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "NoteUpdated", Times.Once());      // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------- //
        // --- SaveTextBlock --- //
        // --------------------- //

        [Fact]
        public async Task SaveTextBlock_MissingId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a save-request without an ID.
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "" };

            // --- Act --- //
            // * We try to save without telling which block should be updated.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the ID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a save-request for a block-ID that doesn't exist.
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "ghost" };

            // --- Act --- //
            // * We try to save the non existing block.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the block could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_ParentNoteMissing_ReturnsNotFound()
        {
            // In this test we check that editing a block fails if its parent note was soft-deleted.

            // --- Arrange --- //
            // * We seed a soft-deleted note and an (active) block on it.
            SeedNote("n-1", DefaultUserID, null, null, "2001-01-01T00:00:00Z");
            SeedTextBlock("tb-1", "n-1");
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "tb-1", Name = "x" };

            // --- Act --- //
            // * We try to save the block whose parent note is gone.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the parent note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot edit a block of a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note owned by another user and a block on it.
            SeedNote("n-1", "someone-else");
            SeedTextBlock("tb-1", "n-1");
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "tb-1", Name = "x" };

            // --- Act --- //
            // * We (the DefaultUser) try to save the foreign block.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed to edit.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_NegativeNotePos_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own note and a block, then request a negative position.
            SeedNote("n-1");
            SeedTextBlock("tb-1", "n-1");
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "tb-1", NotePos = -1 };

            // --- Act --- //
            // * We try to save the block with the negative position.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the position cannot be negative.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_Valid_PersistsFields()
        {
            // In this test we check that a real change to a block gets persisted with trimmed/normalized fields.

            // --- Arrange --- //
            // * We seed our own note and a block.
            // * We request changes to every editable field (name and codeLanguage padded with whitespaces).
            SeedNote("n-1");
            SeedTextBlock("tb-1", "n-1", 0, 0);
            SaveTextblockRequest request = new SaveTextblockRequest
            {
                ID = "tb-1",
                Type = 2,
                Name = "  Heading  ",
                Content = "Some content",
                CodeLanguage = "  python  ",
                IsCollapsed = true,
                NotePos = 3
            };

            // --- Act --- //
            // * We save the changed block.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TextblockResponse body = Assert.IsType<TextblockResponse>(ok.Value);    // We expect the response to be type TextblockResponse.

            Assert.Equal(2, body.Type);                                             // We expect the type to be updated.
            Assert.Equal("Heading", body.Name);                                     // We expect the name to be trimmed.
            Assert.Equal("Some content", body.Content);                             // We expect the content to be stored as provided.
            Assert.Equal("python", body.CodeLanguage);                              // We expect the code language to be trimmed.
            Assert.True(body.IsCollapsed);                                          // We expect the collapsed flag to be true.
            Assert.Equal(3, body.NotePos);                                          // We expect the position to be updated.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_NoEffectiveChange_NoBroadcast()
        {
            // In this test we check that saving a block without a real change does not broadcast.

            // --- Arrange --- //
            // * We seed a workspace, a note inside it and a block.
            // * We request a save that reuses the seeded values -> nothing changes.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedTextBlock("tb-1", "n-1", 1, 0, "Keep", "keep content");
            SaveTextblockRequest request = new SaveTextblockRequest
            {
                ID = "tb-1",
                Type = 1,
                Name = "Keep",
                Content = "keep content",
                NotePos = 0
            };

            // --- Act --- //
            // * We save the block without any effective change.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect a success status, even though nothing changed.
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since there was no change to share.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTextBlock_WorkspaceChange_BroadcastsUpdated()
        {
            // In this test we check that a real change to a workspace-block gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace, a note inside it and a block, then request a content change.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedTextBlock("tb-1", "n-1");
            SaveTextblockRequest request = new SaveTextblockRequest { ID = "tb-1", Content = "Changed" };

            // --- Act --- //
            // * We save the changed workspace-block.
            ActionResult<TextblockResponse> result = await _controller.SaveTextBlock(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "TextBlockUpdated", Times.Once()); // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------ //
        // --- DeleteNote --- //
        // ------------------ //

        [Fact]
        public async Task DeleteNote_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete a note, but pass only whitespace as the ID.
            ActionResult result = await _controller.DeleteNote("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the noteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteNote_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete a note-ID that doesn't exist.
            ActionResult result = await _controller.DeleteNote("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundResult>(result);                                  // We expect a NotFound, since the note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteNote_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note that belongs to another user.
            SeedNote("n-1", "someone-else");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign note.
            ActionResult result = await _controller.DeleteNote("n-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadNote("n-1")!.DeletedAt);                              // We expect the note to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteNote_Valid_SoftDeletesNoteAndChildren()
        {
            // In this test we check that deleting a note soft-deletes the note AND all its child modules.

            // --- Arrange --- //
            // * We seed our own note with a block, a link and an attachment.
            SeedNote("n-1");
            SeedTextBlock("tb-1", "n-1");
            SeedLinkItem("li-1", "n-1");
            SeedAttachment("att-1", "n-1");

            // --- Act --- //
            // * We delete the note.
            ActionResult result = await _controller.DeleteNote("n-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadNote("n-1")!.DeletedAt);                           // We expect the note to be soft-deleted.
            Assert.NotNull(ReloadTextBlock("tb-1")!.DeletedAt);                     // We expect the child block to be soft-deleted too.
            Assert.NotNull(ReloadLinkItem("li-1")!.DeletedAt);                      // We expect the child link to be soft-deleted too.
            Assert.NotNull(ReloadAttachment("att-1")!.DeletedAt);                   // We expect the child attachment to be soft-deleted too.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteNote_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting a workspace-note broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and a note inside it.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");

            // --- Act --- //
            // * We delete the workspace-note.
            await _controller.DeleteNote("n-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "NoteDeleted", Times.Once());      // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ----------------------- //
        // --- DeleteTextBlock --- //
        // ----------------------- //

        [Fact]
        public async Task DeleteTextBlock_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete a block, but pass only whitespace as the ID.
            ActionResult result = await _controller.DeleteTextBlock(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the textblockID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTextBlock_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete a block-ID that doesn't exist.
            ActionResult result = await _controller.DeleteTextBlock("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the block could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTextBlock_ParentNoteMissing_ReturnsNotFound()
        {
            // In this test we check that deleting a block fails if its parent note was soft-deleted.

            // --- Arrange --- //
            // * We seed a soft-deleted note and an (active) block on it.
            SeedNote("n-1", DefaultUserID, null, null, "2001-01-01T00:00:00Z");
            SeedTextBlock("tb-1", "n-1");

            // --- Act --- //
            // * We try to delete the block whose parent note is gone.
            ActionResult result = await _controller.DeleteTextBlock("tb-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the parent note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTextBlock_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete a block of a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note owned by another user and a block on it.
            SeedNote("n-1", "someone-else");
            SeedTextBlock("tb-1", "n-1");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign block.
            ActionResult result = await _controller.DeleteTextBlock("tb-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadTextBlock("tb-1")!.DeletedAt);                        // We expect the block to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTextBlock_Valid_SoftDeletes()
        {
            // In this test we check that deleting a block only soft-deletes it (the row stays present).

            // --- Arrange --- //
            // * We seed our own note and a block on it.
            SeedNote("n-1");
            SeedTextBlock("tb-1", "n-1");

            // --- Act --- //
            // * We delete the block.
            ActionResult result = await _controller.DeleteTextBlock("tb-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadTextBlock("tb-1")!.DeletedAt);                     // We expect DeletedAt to be set -> soft-deleted, row still present.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTextBlock_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting a block in a workspace broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace, a note inside it and a block.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedTextBlock("tb-1", "n-1");

            // --- Act --- //
            // * We delete the block from the workspace-note.
            await _controller.DeleteTextBlock("tb-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "TextBlockDeleted", Times.Once()); // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ---------------------- //
        // --- DeleteLinkItem --- //
        // ---------------------- //

        [Fact]
        public async Task DeleteLinkItem_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete a link, but pass only whitespace as the ID.
            ActionResult result = await _controller.DeleteLinkItem(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the linkItemID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteLinkItem_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete a link-ID that doesn't exist.
            ActionResult result = await _controller.DeleteLinkItem("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the link could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteLinkItem_ParentNoteMissing_ReturnsNotFound()
        {
            // In this test we check that deleting a link fails if its parent note was soft-deleted.

            // --- Arrange --- //
            // * We seed a soft-deleted note and an (active) link on it.
            SeedNote("n-1", DefaultUserID, null, null, "2001-01-01T00:00:00Z");
            SeedLinkItem("li-1", "n-1");

            // --- Act --- //
            // * We try to delete the link whose parent note is gone.
            ActionResult result = await _controller.DeleteLinkItem("li-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the parent note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteLinkItem_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete a link of a note owned by someone else.

            // --- Arrange --- //
            // * We seed a note owned by another user and a link on it.
            SeedNote("n-1", "someone-else");
            SeedLinkItem("li-1", "n-1");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign link.
            ActionResult result = await _controller.DeleteLinkItem("li-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadLinkItem("li-1")!.DeletedAt);                         // We expect the link to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteLinkItem_Valid_SoftDeletes()
        {
            // In this test we check that deleting a link only soft-deletes it (the row stays present).

            // --- Arrange --- //
            // * We seed our own note and a link on it.
            SeedNote("n-1");
            SeedLinkItem("li-1", "n-1");

            // --- Act --- //
            // * We delete the link.
            ActionResult result = await _controller.DeleteLinkItem("li-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadLinkItem("li-1")!.DeletedAt);                      // We expect DeletedAt to be set -> soft-deleted, row still present.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteLinkItem_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting a link in a workspace broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace, a note inside it and a link.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedLinkItem("li-1", "n-1");

            // --- Act --- //
            // * We delete the link from the workspace-note.
            await _controller.DeleteLinkItem("li-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "LinkItemDeleted", Times.Once());  // We expect a broadcast, since the note lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------- //
        // --- GetAllNotes --- //
        // ------------------- //

        [Fact]
        public async Task GetAllNotesOfUser_ReturnsOnlyOwnPersonalActiveNotes()
        {
            // In this test we check that the query returns ONLY the user's own, personal and active notes.

            // --- Arrange --- //
            // * We seed the owner and a second user (the query joins Users, so the owner must exist).
            // * We seed one note per exclusion case, only the personal/active/own one should remain.
            SeedUser();
            SeedUser("other");
            SeedWorkspace("ws-1");

            SeedNote("personal");                                                   // owned
            SeedNote("workspace", DefaultUserID, "ws-1");                           // public note
            SeedNote("deleted", DefaultUserID, null, null, "2001-01-01T00:00:00Z"); // deleted
            SeedNote("foreign", "other");                                           // not the owner

            // --- Act --- //
            // * We query all personal notes of the current user.
            ActionResult<List<NoteResponse>> result = await _controller.GetAllNotesOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<NoteResponse> notes = Assert.IsAssignableFrom<List<NoteResponse>>(ok.Value);       // We expect the body to be a list of NoteResponse.

            Assert.Single(notes);                                                                   // We expect exactly one note to survive the filters.
            Assert.Equal("personal", notes[0].ID);                                                  // We expect it to be the personal/active/own one.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllNotesOfUser_ExcludesNotesOfDeletedOwner()
        {
            // In this test we check that notes of a soft-deleted owner are excluded by the Users-join.

            // --- Arrange --- //
            // * We seed the owner as soft-deleted and give him a (otherwise valid) personal note.
            SeedUser(DefaultUserID, "2001-01-01T00:00:00Z");
            SeedNote("personal");

            // --- Act --- //
            // * We query all personal notes of the (deleted) current user.
            ActionResult<List<NoteResponse>> result = await _controller.GetAllNotesOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<NoteResponse> notes = Assert.IsAssignableFrom<List<NoteResponse>>(ok.Value);       // We expect the body to be a list of NoteResponse.
            Assert.Empty(notes);                                                                    // We expect no note, since the owner is soft-deleted.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllNotesOfWorkspace_ReturnsActiveWorkspaceNotes()
        {
            // In this test we check that the query returns only the active notes of the given workspace.

            // --- Arrange --- //
            // * We seed the owner and a workspace, then one note per case.
            SeedUser();
            SeedWorkspace("ws-1");
            SeedNote("a", DefaultUserID, "ws-1");
            SeedNote("b", DefaultUserID, "ws-1", null, "2001-01-01T00:00:00Z");   // public note, but deleted
            SeedNote("c");                                                        // personal note

            // --- Act --- //
            // * We query all notes of the workspace.
            ActionResult<List<NoteResponse>> result = await _controller.GetAllNotesOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<NoteResponse> notes = Assert.IsAssignableFrom<List<NoteResponse>>(ok.Value);       // We expect the body to be a list of NoteResponse.
            Assert.Single(notes);                                                                   // We expect exactly one note to survive the filters.
            Assert.Equal("a", notes[0].ID);                                                         // We expect it to be the active workspace-note.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------- //
        // --- GetNoteWithID --- //
        // --------------------- //

        [Fact]
        public async Task GetNoteWithID_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We query a single note, but pass only whitespace as the ID.
            ActionResult<NoteResponse> result = await _controller.GetNoteWithID(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the noteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteWithID_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We seed only the user (the query joins Users), but no note.
            SeedUser();

            // --- Act --- //
            // * We query a note-ID that doesn't exist.
            ActionResult<NoteResponse> result = await _controller.GetNoteWithID("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteWithID_Found_ReturnsNote()
        {
            // In this test we check that an existing note is returned, including the resolved OwnerName.

            // --- Arrange --- //
            // * We seed the user (for the join) and one note.
            // * Our SeedUser sets no first/last name, so the OwnerName falls back to the user ID.
            SeedUser();
            SeedNote("n-1");

            // --- Act --- //
            // * We query the seeded note by its ID.
            ActionResult<NoteResponse> result = await _controller.GetNoteWithID("n-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            NoteResponse body = Assert.IsType<NoteResponse>(ok.Value);              // We expect the body to be type NoteResponse.
            Assert.Equal("n-1", body.ID);                                           // We expect to get back the note we asked for.
            Assert.Equal(DefaultUserID, body.OwnerName);                            // We expect the OwnerName to fall back to the user ID.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------- //
        // --- GetNoteCount -- //
        // ------------------- //

        [Fact]
        public async Task GetNoteCountOfUser_CountsOnlyPersonalActiveNotes()
        {
            // In this test we check that the count only includes the user's personal and active notes.

            // --- Arrange --- //
            // * We seed two personal/active notes and one per exclusion case.
            SeedWorkspace("ws-1");
            SeedNote("p1");
            SeedNote("p2");
            SeedNote("w1", DefaultUserID, "ws-1");                           // excluded
            SeedNote("d1", DefaultUserID, null, null, "2001-01-01T00:00:00Z"); // excluded

            // --- Act --- //
            // * We ask for the count of the current user's personal notes.
            ActionResult<int> result = await _controller.GetNoteCountOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two personal/active notes.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteCountOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace count, but pass only whitespace as the ID.
            ActionResult<int> result = await _controller.GetNoteCountOfWorkspace("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteCountOfWorkspace_UnknownWorkspace_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the count of a workspace that doesn't exist.
            ActionResult<int> result = await _controller.GetNoteCountOfWorkspace("ws-x", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the workspace could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteCountOfWorkspace_NotMember_ReturnsForbid()
        {
            // In this test we check that a non-member cannot read the workspace's note count.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");

            // --- Act --- //
            // * We (non-member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetNoteCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetNoteCountOfWorkspace_Member_ReturnsCount()
        {
            // In this test we check that a member gets the correct count of active workspace-notes.

            // --- Arrange --- //
            // * We seed a workspace and add the current user as a member.
            // * We seed two active notes and one deleted one (which should be excluded).
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedNote("n-1", DefaultUserID, "ws-1");
            SeedNote("n-2", DefaultUserID, "ws-1");
            SeedNote("n-3", DefaultUserID, "ws-1", null, "2001-01-01T00:00:00Z");   // excluded

            // --- Act --- //
            // * We (member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetNoteCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two active workspace-notes.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------------- //
        // --- GetTextBlocksOfNote --- //
        // --------------------------- //

        [Fact]
        public async Task GetTextBlocksOfNote_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the blocks, but pass only whitespace as the noteID.
            ActionResult<List<TextblockResponse>> result = await _controller.GetTextBlocksOfNote(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the noteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTextBlocksOfNote_UnknownNote_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the blocks of a note that doesn't exist.
            ActionResult<List<TextblockResponse>> result = await _controller.GetTextBlocksOfNote("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTextBlocksOfNote_ReturnsActiveBlocksOrderedByPosition()
        {
            // In this test we check that only active blocks are returned, ordered by their position.

            // --- Arrange --- //
            // * We seed a note with two active blocks (seeded out of order) and one deleted block.
            SeedNote("n-1");
            SeedTextBlock("tb-1", "n-1", 0, 1);
            SeedTextBlock("tb-0", "n-1", 0, 0);
            SeedTextBlock("tb-x", "n-1", 0, 2, null, null, null, 0, "2001-01-01T00:00:00Z");   // deleted

            // --- Act --- //
            // * We ask for the blocks of the note.
            ActionResult<List<TextblockResponse>> result = await _controller.GetTextBlocksOfNote("n-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<TextblockResponse> blocks = Assert.IsAssignableFrom<List<TextblockResponse>>(ok.Value);// We expect the body to be a list of TextblockResponse.
            Assert.Equal(2, blocks.Count);                                                          // We expect the deleted block to be excluded.
            Assert.Equal("tb-0", blocks[0].ID);                                                     // We expect the block at position 0 to come first.
            Assert.Equal("tb-1", blocks[1].ID);                                                     // We expect the block at position 1 to come second.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------------- //
        // --- GetLinkItemsOfNote --- //
        // -------------------------- //

        [Fact]
        public async Task GetLinkItemsOfNote_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the links, but pass only whitespace as the noteID.
            ActionResult<List<LinkItemResponse>> result = await _controller.GetLinkItemsOfNote(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the noteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetLinkItemsOfNote_UnknownNote_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the links of a note that doesn't exist.
            ActionResult<List<LinkItemResponse>> result = await _controller.GetLinkItemsOfNote("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the note could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetLinkItemsOfNote_ReturnsActiveLinksOrderedByPosition()
        {
            // In this test we check that only active links are returned, ordered by their position.

            // --- Arrange --- //
            // * We seed a note with two active links (seeded out of order) and one deleted link.
            SeedNote("n-1");
            SeedLinkItem("li-1", "n-1", "https://second.com", 1);
            SeedLinkItem("li-0", "n-1", "https://first.com", 0);
            SeedLinkItem("li-x", "n-1", "https://gone.com", 2, null, "2001-01-01T00:00:00Z");   // deleted

            // --- Act --- //
            // * We ask for the links of the note.
            ActionResult<List<LinkItemResponse>> result = await _controller.GetLinkItemsOfNote("n-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            List<LinkItemResponse> links = Assert.IsAssignableFrom<List<LinkItemResponse>>(ok.Value);   // We expect the body to be a list of LinkItemResponse.
            Assert.Equal(2, links.Count);                                                               // We expect the deleted link to be excluded.
            Assert.Equal("li-0", links[0].ID);                                                          // We expect the link at position 0 to come first.
            Assert.Equal("li-1", links[1].ID);                                                          // We expect the link at position 1 to come second.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------------- //
        // --- getSpaceInfosOfNote -- //
        // -------------------------- //

        [Fact]
        public async Task GetSpaceInfosOfNote_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace info, but pass only whitespace as the noteID.
            ActionResult<WorkspaceResponse> result = await _controller.getSpaceInfosOfNote(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the noteID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfNote_PersonalNote_ReturnsNotFound()
        {
            // In this test we check that a personal note has no workspace to resolve.

            // --- Arrange --- //
            // * We seed a personal note (no workspace).
            SeedNote("n-1");

            // --- Act --- //
            // * We ask for the workspace info of the personal note.
            ActionResult<WorkspaceResponse> result = await _controller.getSpaceInfosOfNote("n-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since a personal note has no workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfNote_WorkspaceNote_ReturnsWorkspace()
        {
            // In this test we check that a workspace-note resolves to its workspace.

            // --- Arrange --- //
            // * We seed a workspace and a note inside that workspace.
            SeedWorkspace("ws-1");
            SeedNote("n-1", DefaultUserID, "ws-1");

            // --- Act --- //
            // * We ask for the workspace info of the workspace-note.
            ActionResult<WorkspaceResponse> result = await _controller.getSpaceInfosOfNote("n-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            WorkspaceResponse body = Assert.IsType<WorkspaceResponse>(ok.Value);    // We expect the body to be type WorkspaceResponse.
            Assert.Equal("ws-1", body.ID);                                          // We expect to get back the workspace the note belongs to.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------- //
        // --- HELPERS --- //
        // --------------- //

        // Inserts a User. The GET queries join the Users table, so the owner has to exist there.
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

        // Inserts a WorkspaceMember (used by the workspace permission checks).
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

        // Inserts a Folder.
        private Folder SeedFolder(string id, string ownerID = DefaultUserID, string? workspaceID = null, string? deletedAt = null)
        {
            Folder folder = new Folder
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = workspaceID,
                Name = "folder_" + id,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Folders.Add(folder);
            _db.SaveChanges();

            return folder;
        }

        // Inserts a Note.
        private Note SeedNote(
            string id,
            string ownerID = DefaultUserID,
            string? workspaceID = null,
            string? folderID = null,
            string? deletedAt = null)
        {
            Note note = new Note
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = workspaceID,
                FolderID = folderID,
                Name = "note_" + id,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Notes.Add(note);
            _db.SaveChanges();

            return note;
        }

        // Inserts a Note_TextBlock.
        private Note_TextBlock SeedTextBlock(
            string id,
            string noteID,
            int type = 0,
            int notePos = 0,
            string? name = null,
            string? content = null,
            string? codeLanguage = null,
            int isCollapsed = 0,
            string? deletedAt = null)
        {
            Note_TextBlock textBlock = new Note_TextBlock
            {
                ID = id,
                NoteID = noteID,
                Type = type,
                Name = name,
                Content = content,
                CodeLanguage = codeLanguage,
                IsCollapsed = isCollapsed,
                NotePos = notePos,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Note_TextBlocks.Add(textBlock);
            _db.SaveChanges();

            return textBlock;
        }

        // Inserts a Note_LinkItem.
        private Note_LinkItem SeedLinkItem(
            string id,
            string noteID,
            string url = "https://example.com",
            int notePos = 0,
            string? label = null,
            string? deletedAt = null)
        {
            Note_LinkItem linkItem = new Note_LinkItem
            {
                ID = id,
                NoteID = noteID,
                Label = label,
                Url = url,
                NotePos = notePos,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Note_LinkItems.Add(linkItem);
            _db.SaveChanges();

            return linkItem;
        }

        // Inserts a NoteAttachment (used by the cascade-delete test).
        private NoteAttachment SeedAttachment(string id, string noteID, string ownerID = DefaultUserID, string? deletedAt = null)
        {
            NoteAttachment attachment = new NoteAttachment
            {
                ID = id,
                NoteID = noteID,
                OwnerID = ownerID,
                OriginalFileName = "file_" + id + ".png",
                StoredFileName = id + ".png",
                ContentType = "image/png",
                FileSize = 123,
                CreatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.NoteAttachments.Add(attachment);
            _db.SaveChanges();

            return attachment;
        }

        // Reloads a Note from the context so a test can inspect what was persisted.
        private Note? ReloadNote(string id)
        {
            return _db.Notes.Find(id);
        }

        // Reloads a Note_TextBlock from the context so a test can inspect what was persisted.
        private Note_TextBlock? ReloadTextBlock(string id)
        {
            return _db.Note_TextBlocks.Find(id);
        }

        // Reloads a Note_LinkItem from the context so a test can inspect what was persisted.
        private Note_LinkItem? ReloadLinkItem(string id)
        {
            return _db.Note_LinkItems.Find(id);
        }

        // Reloads a NoteAttachment from the context so a test can inspect what was persisted.
        private NoteAttachment? ReloadAttachment(string id)
        {
            return _db.NoteAttachments.Find(id);
        }
    }
}
