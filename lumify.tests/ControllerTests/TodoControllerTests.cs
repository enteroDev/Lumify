/* TodoControllerTests
 * Unit tests for every action of: TodoListsController.
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
    public class TodoControllerTests : IDisposable
    {
        private const string DefaultUserID = "user-1";
        private const string OldTimestamp = "2000-01-01T00:00:00.0000000Z";

        private readonly LumifyDbContext _db;
        private readonly IHubContext<TodoHub> _hub;
        private readonly Mock<IClientProxy> _hubSpy;
        private readonly TodoListsController _controller;


        public TodoControllerTests()
        {
            _db = TestDbFactory.Create();

            (IHubContext<TodoHub> hub, Mock<IClientProxy> spy) = SignalRMock.Create<TodoHub>();
            _hub = hub;
            _hubSpy = spy;

            _controller = new TodoListsController(NullLogger<TodoListsController>.Instance, _db, _hub);
            ControllerContextFactory.SignIn(_controller, DefaultUserID);
        }

        // Runs after every single test.
        public void Dispose()
        {
            _db.Dispose();
        }





        // ------------------- //
        // --- AddTodoList --- //
        // ------------------- //

        [Fact]
        public async Task AddTodoList_MissingName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request, that has an empty string for the name-property
            AddTodoListRequest request = new AddTodoListRequest { Name = "   " };

            // --- Act --- //
            // * We send the request with an empty name
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect to get a BadRequest answer from the server/api
            Assert.Empty(_db.TodoLists);                                            // We also expect the db to be empty, since a request without name shouldn't be saved.
            SignalRMock.AssertSilent(_hubSpy);                                      // At last we want to check if something is parsed to the hub. -> We assume not.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoList_UnknownWorkspace_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request to add a TodoList into a non existing Workspace
            AddTodoListRequest request = new AddTodoListRequest { Name = "Karpfen", WorkspaceID = "Dudelsack" };

            // --- Act --- //
            // * We try to add a TodoList into a non existing Workspace
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect to get a BadRequest from the server/api
            Assert.Empty(_db.TodoLists);                                            // We expect that the database is empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoList_DeletedWorkspace_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a soft deleted workspace entry via our helper "SeedWorkspace"
            // * We also create an AddTodoListRequest for adding a new TodoList.
            SeedWorkspace("NichtVorhandenerWorkspace", DefaultUserID, "2001-01-01T00:00:00Z");
            AddTodoListRequest request = new AddTodoListRequest { Name = "Kamel", WorkspaceID = "NichtVorhandenerWorkspace" };

            // --- Act --- //
            // * We try to add the TodoList to the deleted Workspace.
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect to get a BadRequest from the controller
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoList_Personal_PersistsAndReturnsResponse_NoBroadcast()
        {
            // In this test we check if the added TodoList is persistent
            // and is not getting shared via SignalR with others, since it gets added into the private space.

            // --- Arrange --- //
            // * We provide a valid request for adding a TodoList
            AddTodoListRequest request = new AddTodoListRequest { Name = "  Zigaretten  " };

            // --- Act --- //
            // * We add the TodoList to the private space
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);      // We expect the repsonse to be type TodoListRepsonse

            Assert.Equal("Zigaretten", body.Name);                                  // We expect the name to be "Zigaretten"
            Assert.Equal(DefaultUserID, body.OwnerID);                              // We expect to get the DefaultUser (TestUser) as owner
            Assert.Null(body.WorkspaceID);                                          // We expect the workspace to be null, since we add the Todolist to a private workspace
            Assert.Equal(1, body.Status);                                           // We expact the status to be "1" (pending - offen)
            Assert.Equal(0, body.IsArchived);                                       // We expect the archived status to be 0 (false)
            Assert.False(string.IsNullOrWhiteSpace(body.ID));                       // We expect the ID to contain a valid value. -> string should not onlycontain whitespaces and should not be empty.

            TodoList? stored = ReloadList(body.ID);                                 // We check if the todolist is existent/persistent in the database.
            Assert.NotNull(stored);                                                 // We expect the todolist to be present and NotNull.
            Assert.Equal("Zigaretten", stored!.Name);                               // We expect the stored todolist still has the name "Zigaretten"

            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR didn't share the sotred data, since it got added into the private space.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoList_InWorkspace_BroadcastsCreated()
        {
            // In this function we test if an added todolist gets broadcasted via SignalR to the Hub

            // --- Arrange --- //
            // * We seed/create a new workspace
            // * We create a request to add a todoList in this workspace
            SeedWorkspace("ws-1");
            AddTodoListRequest request = new AddTodoListRequest { Name = "Sprint", WorkspaceID = "ws-1" };

            // --- Act --- //
            // * We add a TodoList to the prepared shared-workspace.
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success-message from the server/api.
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);      // We expect the body to be type TodoListRepsonse.
            Assert.Equal("ws-1", body.WorkspaceID);                                 // We expect the id from the resposne to be equal with our created workspace.

            SignalRMock.AssertBroadcast(_hubSpy, "TodoListCreated", Times.Once());  // We expect to find a broadcast from SignalR. Changes should be shared via SignalR. This is a public workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------- //
        // --- AddTodoEntry --- //
        // -------------------- //

        [Fact]
        public async Task AddTodoEntry_MissingName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create an entry-request with an empty name, but a (random) listID.
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "", TodoListID = "tl-1" };

            // --- Act --- //
            // * We try to add the entry without a name.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_MissingTodoListId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create an AddTodoEntry-request without a corresponding TodoListID
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Duftbaum kaufen", TodoListID = " " };

            // --- Act --- //
            // * We try to add the TodoEntry without telling to which TodoList it belongs.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the TodoListID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_UnknownList_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create an AddTodoEntry-request with a non existing Workspace
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Duftbaum kaufen", TodoListID = "nope" };

            // --- Act --- //
            // * We try to add the TodoEntry into the non existing TodoList.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the TodoList shouldnt be found.
            Assert.Empty(_db.TodoEntries);                                          // We expect the database to be empty, since no TodoEntry should be created.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_Valid_PersistsTrimmedFields()
        {
            // In this test we check if a valid TodoEntry gets persisted and its name/description get trimmed.

            // --- Arrange --- //
            // * We create a TodoList via the seeder
            // * We create a valid AddTodoList-request, where name and description are padded with whitespaces.
            SeedList("tl-1");
            AddTodoEntryRequest request = new AddTodoEntryRequest
            {
                Name = "  Duftbaum kaufen  ",
                Description = "  2 Stück  ",
                TodoListID = "tl-1"
            };

            // --- Act --- //
            // * We add the entry to the created TodoList.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);    // We expect the response to be type TodoEntryResponse.

            Assert.Equal("Duftbaum kaufen", body.Name);                             // We expect the name to be existent and correctly trimmed.
            Assert.Equal("2 Stück", body.Description);                              // We expect the description to be existent and correctly trimmed.
            Assert.Equal("tl-1", body.TodoListID);                                  // We expect the TodoEntry to belong to our created TodoList.
            Assert.Equal(1, body.Status);                                           // We expect the status to be "1" (pending - offen).

            TodoEntry? stored = ReloadEntry(body.ID);                               // We check if the entry is existent/persistent in the database.
            Assert.NotNull(stored);                                                 // We expect the entry to be present and NotNull.
            Assert.Equal("Duftbaum kaufen", stored!.Name);                          // We expect the stored TodoEntry to still have the same name.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_BlankDescription_StoredAsNull()
        {
            // In this test we check that a blank (whitespace only) description gets normalized to null.

            // --- Arrange --- //
            // * We create a TodoList and create a request with a description that only contains whitespaces.
            SeedList("tl-1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", Description = "   ", TodoListID = "tl-1" };

            // --- Act --- //
            // * We add the entry with the blank description.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);    // We expect the response to be type TodoEntryResponse.
            Assert.Null(body.Description);                                          // We expect a blank description to be stored as null.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_InWorkspaceList_BroadcastsCreated()
        {
            // In this test we check if an entry, added to a workspace-list, gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and TodoList.
            // * We create a request to add a TodoEntry into the created TodoList and its created Workspace.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", TodoListID = "tl-1" };

            // --- Act --- //
            // * We add the TodoEntry to the created TodoList.
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "TodoEntryCreated", Times.Once()); // We expect a broadcast, since the list lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddTodoEntry_PersonalList_NoBroadcast()
        {
            // --- Arrange --- //
            // * We seed a personal list (no workspace) and create a request for an entry on it.
            SeedList("tl-1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", TodoListID = "tl-1" };

            // --- Act --- //
            // * We add the entry to the personal list.
            await _controller.AddTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since the list is private.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------- //
        // --- SaveTodoList --- //
        // -------------------- //

        [Fact]
        public async Task SaveTodoList_MissingId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a save-request without an ID.
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "" };

            // --- Act --- //
            // * We try to save without telling which list should be updated.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the ID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a save-request for a list-ID that doesn't exist in the database.
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "ghost" };

            // --- Act --- //
            // * We try to save the non existing list.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the list could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot edit a list owned by someone else.

            // --- Arrange --- //
            // * We seed a list that belongs to another user.
            // * We create a request trying to rename that foreign list.
            SeedList("tl-1", "someone-else");
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "tl-1", Name = "Hacked" };

            // --- Act --- //
            // * We (the DefaultUser) try to save the foreign list.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed to edit.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_EmptyName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own list and create a request that tries to set the name to whitespaces only.
            SeedList("tl-1");
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "tl-1", Name = "   " };

            // --- Act --- //
            // * We try to save the list with an empty name.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(-1)]
        public async Task SaveTodoList_InvalidStatus_ReturnsBadRequest(int status)
        {
            // In this theory we check that only the valid status values (1 or 2) are accepted.

            // --- Arrange --- //
            // * We seed our own list and create a request with an invalid status (0, 3 or -1).
            SeedList("tl-1");
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "tl-1", Status = status };

            // --- Act --- //
            // * We try to save the list with the invalid status.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the status must be 1 or 2.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_RenameTodoList_UpdatesNameAndTimestamp()
        {

            // --- Arrange --- //
            // * We seed our own list.
            // * We create a request that renames, completes (status 2) and archives the list.
            SeedList("tl-1");
            SaveTodoListRequest request = new SaveTodoListRequest
            {
                ID = "tl-1",
                Name = "Renamed",
                Status = 2,
                IsArchived = true
            };

            // --- Act --- //
            // * We save the changes to the list.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);      // We expect the response to be type TodoListResponse.

            Assert.Equal("Renamed", body.Name);                                     // We expect the response to carry the new name.
            Assert.Equal(2, body.Status);                                           // We expect the response status to be 2 (done).
            Assert.Equal(1, body.IsArchived);                                       // We expect the archived flag to be 1 (true).

            TodoList? stored = ReloadList("tl-1");                                     // We reload the list from the db to inspect what was persisted.
            Assert.Equal("Renamed", stored!.Name);                                  // We expect the stored name to be updated.
            Assert.Equal(2, stored.Status);                                         // We expect the stored status to be updated.
            Assert.Equal(1, stored.IsArchived);                                     // We expect the stored archived flag to be updated.
            Assert.NotEqual(stored.CreatedAt, stored.UpdatedAt);                    // We expect UpdatedAt to have advanced past the seeded value.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_NoEffectiveChange_DoesNotTouchTimestampOrBroadcast()
        {
            // In this test we check that saving without a real change neither touches the timestamp nor broadcasts.

            // --- Arrange --- //
            // * We seed a workspace and a TodoList inside it.
            // * We create a request that reuses the seeded name -> nothing actually changes.
            SeedWorkspace("ws-1");
            TodoList seeded = SeedList("tl-1", DefaultUserID, "ws-1");
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "tl-1", Name = seeded.Name };

            // --- Act --- //
            // * We save the list without any effective change.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect a success status, even though nothing changed.

            TodoList? stored = ReloadList("tl-1");                                    // We reload the list to inspect its timestamp.
            Assert.Equal(OldTimestamp, stored!.UpdatedAt);                          // We expect UpdatedAt to stay untouched (still the seeded value).
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since there was no change to share.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoList_WorkspaceChange_BroadcastsUpdated()
        {
            // In this test we check that a real change on a workspace-list gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and a list inside it.
            // * We create a request that renames the list.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");
            SaveTodoListRequest request = new SaveTodoListRequest { ID = "tl-1", Name = "Renamed" };

            // --- Act --- //
            // * We save the renamed workspace-list.
            ActionResult<TodoListResponse> result = await _controller.SaveTodoList(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "TodoListUpdated", Times.Once());  // We expect a broadcast, since the list lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------- //
        // --- SaveTodoEntry --- //
        // --------------------- //

        [Fact]
        public async Task SaveTodoEntry_MissingId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a save-request without an ID.
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "" };

            // --- Act --- //
            // * We try to save without telling which entry should be updated.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the ID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a save-request for an entry-ID that doesn't exist.
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "ghost" };

            // --- Act --- //
            // * We try to save the non existing entry.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the entry could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot edit an entry owned by someone else.

            // --- Arrange --- //
            // * We seed a list and an entry that belongs to another user.
            // * We create a request trying to edit that foreign entry.
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1", "someone-else");
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Name = "x" };

            // --- Act --- //
            // * We (the DefaultUser) try to save the foreign entry.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed to edit.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_ParentListMissing_ReturnsNotFound()
        {
            // In this test we check that editing an entry fails if its parent list was soft-deleted.

            // --- Arrange --- //
            // * We seed a soft-deleted list and an (active) entry on it.
            // * We create a request to edit that orphaned entry.
            SeedList("tl-1", DefaultUserID, null, 1, 0, "2001-01-01T00:00:00Z");
            SeedEntry("t2-1", "tl-1");
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Name = "x" };

            // --- Act --- //
            // * We try to save the entry whose parent list is gone.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the parent list could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_InvalidStatus_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed a list and an entry, then create a request with an invalid status (9).
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1");
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Status = 9 };

            // --- Act --- //
            // * We try to save the entry with the invalid status.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the status must be 1 or 2.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_EmptyName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed a list and an entry, then create a request that tries to empty the name.
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1");
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Name = "   " };

            // --- Act --- //
            // * We try to save the entry with an empty name.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_CheckingLastOpenEntry_CompletesList()
        {
            // In this test we check that checking the last open entry auto-completes its parent list.

            // --- Arrange --- //
            // * We seed an open list (status 1) with a single open entry (status 1).
            // * We create a request that checks (status 2) this entry.
            SeedList("tl-1", DefaultUserID, null, 1);
            SeedEntry("t2-1", "tl-1", DefaultUserID, 1);
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Status = 2 };

            // --- Act --- //
            // * We check the last open entry.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);    // We expect the response to be type TodoEntryResponse.

            Assert.Equal(2, body.Status);                                           // We expect the entry status to be 2 (done).
            Assert.True(body.WasLastUnchecked);                                     // We expect the flag to be true, since it was the last open entry.

            TodoList? list = ReloadList("tl-1");                                      // We reload the parent list to check its status.
            Assert.Equal(2, list!.Status);                                          // We expect the list to be auto-completed (status 2).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_CheckingOneOfManyEntries_KeepsListOpen()
        {
            // In this test we check that completing one of many entries keeps the list open.

            // --- Arrange --- //
            // * We seed an open list with two open entries (so one stays open after we check the other).
            // * We create a request that checks (status 2) the first entry.
            SeedList("tl-1", DefaultUserID, null, 1);
            SeedEntry("t2-1", "tl-1", DefaultUserID, 1);
            SeedEntry("E2", "tl-1", DefaultUserID, 1);   // still open
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Status = 2 };

            // --- Act --- //
            // * We check one of the two entries.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);    // We expect the response to be type TodoEntryResponse.

            Assert.False(body.WasLastUnchecked);                                    // We expect the flag to be false, since another entry is still open.

            TodoList? list = ReloadList("tl-1");                                      // We reload the parent list to check its status.
            Assert.Equal(1, list!.Status);                                          // We expect the list to stay open (status 1).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_UncheckingEntry_ReopensList()
        {
            // In this test we check that unchecking an entry reopens a previously completed list.

            // --- Arrange --- //
            // * We seed a completed list (status 2) with a completed entry (status 2).
            // * We create a request that unchecks (status 1) this entry.
            SeedList("tl-1", DefaultUserID, null, 2);
            SeedEntry("t2-1", "tl-1", DefaultUserID, 2);
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Status = 1 };

            // --- Act --- //
            // * We uncheck the entry.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);    // We expect the response to be type TodoEntryResponse.

            Assert.False(body.WasLastUnchecked);                                    // We expect the flag to be false, since we unchecked instead of checked.

            TodoList? list = ReloadList("tl-1");                                      // We reload the parent list to check its status.
            Assert.Equal(1, list!.Status);                                          // We expect the list to be reopened (status 1).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_NoEffectiveChange_NoBroadcast()
        {
            // In this test we check that saving an entry without a real change does not broadcast.

            // --- Arrange --- //
            // * We seed a workspace, a list inside it and an entry.
            // * We create a request that reuses the seeded name and description -> nothing changes.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");
            TodoEntry seeded = SeedEntry("t2-1", "tl-1", DefaultUserID, 1, "keep");
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Name = seeded.Name, Description = "keep" };

            // --- Act --- //
            // * We save the entry without any effective change.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect a success status, even though nothing changed.
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since there was no change to share.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveTodoEntry_StatusChangeInWorkspace_BroadcastsEntryAndList()
        {
            // In this test we check that a status change in a workspace broadcasts BOTH the entry and the list.

            // --- Arrange --- //
            // * We seed a workspace, an open list inside it and an open entry.
            // * We create a request that checks (status 2) the entry.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1", 1);
            SeedEntry("t2-1", "tl-1", DefaultUserID, 1);
            SaveTodoEntryRequest request = new SaveTodoEntryRequest { ID = "t2-1", Status = 2 };

            // --- Act --- //
            // * We check the entry, which also completes its parent list.
            ActionResult<TodoEntryResponse> result = await _controller.SaveTodoEntry(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "TodoEntryUpdated", Times.Once()); // We expect a broadcast for the updated entry.
            SignalRMock.AssertBroadcast(_hubSpy, "TodoListUpdated", Times.Once());  // We expect a second broadcast, since the status change cascades to the list.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ---------------------- //
        // --- DeleteTodoList --- //
        // ---------------------- //

        [Fact]
        public async Task DeleteTodoList_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete a list, but pass only whitespaces as the ID.
            ActionResult result = await _controller.DeleteTodoList("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the todoListID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoList_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete a list-ID that doesn't exist.
            ActionResult result = await _controller.DeleteTodoList("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundResult>(result);                                  // We expect a NotFound, since the list could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoList_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete a list owned by someone else.

            // --- Arrange --- //
            // * We seed a list that belongs to another user.
            SeedList("tl-1", "someone-else");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign list.
            ActionResult result = await _controller.DeleteTodoList("tl-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadList("tl-1")!.DeletedAt);                               // We expect the list to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoList_Valid_SoftDeletes()
        {
            // In this test we check that deleting a list only soft-deletes it (the row stays present).

            // --- Arrange --- //
            // * We seed our own list.
            SeedList("tl-1");

            // --- Act --- //
            // * We delete the list.
            ActionResult result = await _controller.DeleteTodoList("tl-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadList("tl-1")!.DeletedAt);                            // We expect DeletedAt to be set -> soft-deleted, row still present.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoList_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting a workspace-list broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and a list inside it.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");

            // --- Act --- //
            // * We delete the workspace-list.
            await _controller.DeleteTodoList("tl-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "TodoListDeleted", Times.Once());  // We expect a broadcast, since the list lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ----------------------- //
        // --- DeleteTodoEntry --- //
        // ----------------------- //

        [Fact]
        public async Task DeleteTodoEntry_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete an entry, but pass only whitespace as the ID.
            ActionResult result = await _controller.DeleteTodoEntry(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the todoEntryID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoEntry_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete an entry-ID that doesn't exist.
            ActionResult result = await _controller.DeleteTodoEntry("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundResult>(result);                                  // We expect a NotFound, since the entry could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoEntry_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete an entry owned by someone else.

            // --- Arrange --- //
            // * We seed a list and an entry that belongs to another user.
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1", "someone-else");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign entry.
            ActionResult result = await _controller.DeleteTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadEntry("t2-1")!.DeletedAt);                              // We expect the entry to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoEntry_ParentListMissing_ReturnsNotFound()
        {
            // In this test we check that deleting an entry fails if its parent list was soft-deleted.

            // --- Arrange --- //
            // * We seed a soft-deleted list and an (active) entry on it.
            SeedList("tl-1", DefaultUserID, null, 1, 0, "2001-01-01T00:00:00Z");
            SeedEntry("t2-1", "tl-1");

            // --- Act --- //
            // * We try to delete the entry whose parent list is gone.
            ActionResult result = await _controller.DeleteTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result);                            // We expect a NotFound, since the parent list could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoEntry_Valid_SoftDeletes()
        {
            // In this test we check that deleting an entry only soft-deletes it (the row stays present).

            // --- Arrange --- //
            // * We seed our own list and an entry on it.
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1");

            // --- Act --- //
            // * We delete the entry.
            ActionResult result = await _controller.DeleteTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadEntry("t2-1")!.DeletedAt);                           // We expect DeletedAt to be set -> soft-deleted, row still present.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteTodoEntry_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting an entry in a workspace broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace, a list inside it and an entry.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");
            SeedEntry("t2-1", "tl-1");

            // --- Act --- //
            // * We delete the entry from the workspace-list.
            await _controller.DeleteTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "TodoEntryDeleted", Times.Once()); // We expect a broadcast, since the entry lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ----------------------- //
        // --- GetAllTodoLists --- //
        // ----------------------- //

        [Fact]
        public async Task GetAllTodoListsOfUser_ReturnsOnlyOwnPersonalActiveLists()
        {
            // In this test we check that the query returns ONLY the user's own, personal and active lists.

            // --- Arrange --- //
            // * We seed the owner and a second user (the query joins Users, so the owner must exist).
            // * We seed one list per exclusion case, only the personal/active/own one should remain.
            SeedUser();
            SeedUser("other");
            SeedWorkspace("ws-1");

            SeedList("personal");                                                   // owned
            SeedList("workspace", DefaultUserID, "ws-1");                           // public TodoList
            SeedList("deleted", DefaultUserID, null, 1, 0, "2001-01-01T00:00:00Z"); // public TodoList, but deleted
            SeedList("foreign", "other");                                           // public TodoList, but not the owner

            // --- Act --- //
            // * We query all personal lists of the current user.
            ActionResult<List<TodoListResponse>> result = await _controller.GetAllTodoListsOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            List<TodoListResponse> lists = Assert.IsAssignableFrom<List<TodoListResponse>>(ok.Value);   // We expect the body to be a list of TodoListResponse.

            Assert.Single(lists);                                                                       // We expect exactly one list to survive the filters.
            Assert.Equal("personal", lists[0].ID);                                                      // We expect it to be the personal/active/own one.
        }


        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllTodoListsOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We query the workspace-lists, but pass only whitespace as the workspaceID.
            ActionResult<List<TodoListResponse>> result = await _controller.GetAllTodoListsOfWorkspace(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllTodoListsOfWorkspace_ReturnsActiveWorkspaceLists()
        {
            // In this test we check that the query returns only the active lists of the given workspace.

            // --- Arrange --- //
            // * We seed the owner and a workspace, then one list per case.
            SeedUser();
            SeedWorkspace("ws-1");
            SeedList("a", DefaultUserID, "ws-1");
            SeedList("b", DefaultUserID, "ws-1", 1, 0, "2001-01-01T00:00:00Z");   // Public TodoList, but deleted
            SeedList("c");                                                        // Public TodoList

            // --- Act --- //
            // * We query all lists of the workspace.
            ActionResult<List<TodoListResponse>> result = await _controller.GetAllTodoListsOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            List<TodoListResponse> lists = Assert.IsAssignableFrom<List<TodoListResponse>>(ok.Value);   // We expect the body to be a list of TodoListResponse.
            Assert.Single(lists);                                                                       // We expect exactly one list to survive the filters.
            Assert.Equal("a", lists[0].ID);                                                             // We expect it to be the active workspace-list.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------------- //
        // --- GetAllTodoEntries --- //
        // ------------------------- //

        [Fact]
        public async Task GetAllTodoEntriesOfUser_ReturnsOnlyOwnPersonalActiveEntries()
        {
            // In this test we check that the query returns ONLY the user's own, personal and active entries.

            // --- Arrange --- //
            // * We seed the owner, a workspace and a personal + a workspace list.
            // * We seed one entry per exclusion case, only the personal/active one should remain.
            SeedUser();
            SeedWorkspace("ws-1");
            SeedList("personalList");
            SeedList("workspaceList", DefaultUserID, "ws-1");

            SeedEntry("e-keep", "personalList");                                                    // owned
            SeedEntry("e-deleted", "personalList", DefaultUserID, 1, null, "2001-01-01T00:00:00Z"); // owned but deleted
            SeedEntry("e-workspace", "workspaceList");                                              // Public Workspace-TodoList

            // --- Act --- //
            // * We query all personal entries of the current user.
            ActionResult<List<TodoEntryResponse>> result = await _controller.GetAllTodoEntriesOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                               // We expect to get a success status.
            List<TodoEntryResponse> entries = Assert.IsAssignableFrom<List<TodoEntryResponse>>(ok.Value);   // We expect the body to be a list of TodoEntryResponse.
            Assert.Single(entries);                                                                         // We expect exactly one entry to survive the filters.
            Assert.Equal("e-keep", entries[0].ID);                                                          // We expect it to be the personal/active one.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllTodoEntriesOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We query the workspace-entries, but pass an empty workspaceID.
            ActionResult<List<TodoEntryResponse>> result = await _controller.GetAllTodoEntriesOfWorkspace("", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllTodoEntriesOfWorkspace_ReturnsActiveWorkspaceEntries()
        {
            // In this test we check that the query returns only the entries of lists in the given workspace.

            // --- Arrange --- //
            // * We seed the owner, a workspace, a workspace-list and a personal list.
            // * We seed one entry on each list, only the workspace one should remain.
            SeedUser();
            SeedWorkspace("ws-1");
            SeedList("wl", DefaultUserID, "ws-1");
            SeedList("pl");
            SeedEntry("t2-1", "wl");
            SeedEntry("e2", "pl");   // excluded - personal list

            // --- Act --- //
            // * We query all entries of the workspace.
            ActionResult<List<TodoEntryResponse>> result = await _controller.GetAllTodoEntriesOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                               // We expect to get a success status.
            List<TodoEntryResponse> entries = Assert.IsAssignableFrom<List<TodoEntryResponse>>(ok.Value);   // We expect the body to be a list of TodoEntryResponse.
            Assert.Single(entries);                                                                         // We expect exactly one entry to survive the filters.
            Assert.Equal("t2-1", entries[0].ID);                                                            // We expect it to be the entry of the workspace-list.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------------- //
        // --- GetTodoListWithID --- //
        // ------------------------- //

        [Fact]
        public async Task GetTodoListWithID_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We query a single list, but pass only whitespace as the ID.
            ActionResult<TodoListResponse> result = await _controller.GetTodoListWithID(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the todoListID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListWithID_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We seed only the user (the query joins Users), but no list.
            SeedUser();

            // --- Act --- //
            // * We query a list-ID that doesn't exist.
            ActionResult<TodoListResponse> result = await _controller.GetTodoListWithID("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the list could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListWithID_Found_ReturnsList()
        {
            // --- Arrange --- //
            // * We seed the user (for the join) and one list.
            SeedUser();
            SeedList("tl-1");

            // --- Act --- //
            // * We query the seeded list by its ID.
            ActionResult<TodoListResponse> result = await _controller.GetTodoListWithID("tl-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);      // We expect the body to be type TodoListResponse.
            Assert.Equal("tl-1", body.ID);                                            // We expect to get back the list we asked for.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------------ //
        // --- GetTodoListCount --- //
        // ------------------------ //

        [Fact]
        public async Task GetTodoListCountOfUser_CountsOnlyPersonalActiveLists()
        {
            // In this test we check that the count only includes the user's personal and active lists.

            // --- Arrange --- //
            // * We seed two personal/active lists and one per exclusion case.
            SeedWorkspace("ws-1");
            SeedList("p1");
            SeedList("p2");
            SeedList("w1", DefaultUserID, "ws-1");                          // excluded
            SeedList("d1", DefaultUserID, null, 1, 0, "2001-01-01T00:00:00Z"); // excluded

            // --- Act --- //
            // * We ask for the count of the current user's personal lists.
            ActionResult<int> result = await _controller.GetTodoListCountOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two personal/active lists.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListCountOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace count, but pass only whitespace as the ID.
            ActionResult<int> result = await _controller.GetTodoListCountOfWorkspace("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListCountOfWorkspace_UnknownWorkspace_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the count of a workspace that doesn't exist.
            ActionResult<int> result = await _controller.GetTodoListCountOfWorkspace("ws-x", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the workspace could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListCountOfWorkspace_NotMember_ReturnsForbid()
        {
            // In this test we check that a non-member cannot read the workspace's list count.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");

            // --- Act --- //
            // * We (non-member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetTodoListCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetTodoListCountOfWorkspace_Member_ReturnsCount()
        {
            // In this test we check that a member gets the correct count of active workspace-lists.

            // --- Arrange --- //
            // * We seed a workspace and add the current user as a member.
            // * We seed two active lists and one deleted one (which should be excluded).
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedList("tl-1", DefaultUserID, "ws-1");
            SeedList("l2", DefaultUserID, "ws-1");
            SeedList("l3", DefaultUserID, "ws-1", 1, 0, "2001-01-01T00:00:00Z");   // excluded

            // --- Act --- //
            // * We (member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetTodoListCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two active workspace-lists.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // -------------------------------- //
        // --- GetSpaceInfosOfTodoEntry --- //
        // -------------------------------- //

        [Fact]
        public async Task GetSpaceInfosOfTodoEntry_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace info, but pass only whitespace as the entry-ID.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfTodoEntry(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the todoEntryID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfTodoEntry_PersonalEntry_ReturnsNotFound()
        {
            // In this test we check that a personal entry has no workspace to resolve.

            // --- Arrange --- //
            // * We seed a personal list (no workspace) and an entry on it.
            SeedList("tl-1");
            SeedEntry("t2-1", "tl-1");

            // --- Act --- //
            // * We ask for the workspace info of the personal entry.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since a personal entry has no workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfTodoEntry_WorkspaceEntry_ReturnsWorkspace()
        {
            // In this test we check that a workspace-entry resolves to its workspace.

            // --- Arrange --- //
            // * We seed a workspace, a list inside it and an entry on that list.
            SeedWorkspace("ws-1");
            SeedList("tl-1", DefaultUserID, "ws-1");
            SeedEntry("t2-1", "tl-1");

            // --- Act --- //
            // * We ask for the workspace info of the workspace-entry.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfTodoEntry("t2-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            WorkspaceResponse body = Assert.IsType<WorkspaceResponse>(ok.Value);    // We expect the body to be type WorkspaceResponse.
            Assert.Equal("ws-1", body.ID);                                          // We expect to get back the workspace the entry belongs to.
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

        // Inserts a TodoList.
        private TodoList SeedList(
            string id,
            string ownerID = DefaultUserID,
            string? workspaceID = null,
            int status = 1,
            int isArchived = 0,
            string? deletedAt = null)
        {
            TodoList list = new TodoList
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = workspaceID,
                Name = "list_" + id,
                Status = status,
                IsArchived = isArchived,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.TodoLists.Add(list);
            _db.SaveChanges();

            return list;
        }

        // Inserts a TodoEntry.
        private TodoEntry SeedEntry(
            string id,
            string listID,
            string ownerID = DefaultUserID,
            int status = 1,
            string? description = null,
            string? deletedAt = null)
        {
            TodoEntry entry = new TodoEntry
            {
                ID = id,
                TodoListID = listID,
                OwnerID = ownerID,
                Name = "entry_" + id,
                Description = description,
                Status = status,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.TodoEntries.Add(entry);
            _db.SaveChanges();

            return entry;
        }

        // Reloads a TodoList from the context so a test can inspect what was persisted.
        private TodoList? ReloadList(string id)
        {
            return _db.TodoLists.Find(id);
        }

        // Reloads a TodoEntry from the context so a test can inspect what was persisted.
        private TodoEntry? ReloadEntry(string id)
        {
            return _db.TodoEntries.Find(id);
        }
    }
}

