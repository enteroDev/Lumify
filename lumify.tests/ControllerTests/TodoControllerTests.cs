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
            // Arrange
            AddTodoListRequest request = new AddTodoListRequest { Name = "   " };

            // Act
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.TodoLists);
            SignalRMock.AssertSilent(_hubSpy);
        }

        [Fact]
        public async Task AddTodoList_UnknownWorkspace_ReturnsBadRequest()
        {
            // Arrange
            AddTodoListRequest request = new AddTodoListRequest { Name = "Karpfen", WorkspaceID = "Dudelsack" };

            // Act
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.TodoLists);
        }

        [Fact]
        public async Task AddTodoList_DeletedWorkspace_ReturnsBadRequest() // Deleted means softDelted in this case. So the workspace still exists in the database, but is "inactive"
        {
            // --- Arrange --- //
            // We create a non existing workspace via our helper "SeedWorkspace"
            // We also create an AddTodoListRequest for adding a new TodoList
            SeedWorkspace("NichtVorhandenerWorkspace", DefaultUserID, "2001-01-01T00:00:00Z");
            AddTodoListRequest request = new AddTodoListRequest { Name = "Kamel", WorkspaceID = "NichtVorhandenerWorkspace" };

            // --- Act --- //
            // We try to add the TodoList to the non existing, fictional Workspace
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // --- Assert --- //
            // We expect to get a BadRequest from the controller
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        // In this test we check if the added TodoList is persistent and is not getting shared via SignalR with others, since it gets added into the private space.
        [Fact]
        public async Task AddTodoList_Personal_PersistsAndReturnsResponse_NoBroadcast()
        {
            // Arrange
            AddTodoListRequest request = new AddTodoListRequest { Name = "  Zigaretten  " };

            // Act
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // Assert
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);

            Assert.Equal("Zigaretten", body.Name);   // trimmed
            Assert.Equal(DefaultUserID, body.OwnerID);
            Assert.Null(body.WorkspaceID);
            Assert.Equal(1, body.Status);
            Assert.Equal(0, body.IsArchived);
            Assert.False(string.IsNullOrWhiteSpace(body.ID));

            TodoList? stored = ReloadList(body.ID);
            Assert.NotNull(stored);
            Assert.Equal("Zigaretten", stored!.Name);

            // Personal lists are not broadcast to any workspace group.
            SignalRMock.AssertSilent(_hubSpy);
        }

        [Fact]
        public async Task AddTodoList_InWorkspace_BroadcastsCreated()
        {
            // Arrange
            SeedWorkspace("ws-1");
            AddTodoListRequest request = new AddTodoListRequest { Name = "Sprint", WorkspaceID = "ws-1" };

            // Act
            ActionResult<TodoListResponse> result = await _controller.AddTodoList(request, CancellationToken.None);

            // Assert
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            TodoListResponse body = Assert.IsType<TodoListResponse>(ok.Value);
            Assert.Equal("ws-1", body.WorkspaceID);

            SignalRMock.AssertBroadcast(_hubSpy, "TodoListCreated", Times.Once());
        }


        // -------------------- //
        // --- AddTodoEntry --- //
        // -------------------- //

        [Fact]
        public async Task AddTodoEntry_MissingName_ReturnsBadRequest()
        {
            // Arrange
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "", TodoListID = "L1" };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddTodoEntry_MissingTodoListId_ReturnsBadRequest()
        {
            // Arrange
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Buy milk", TodoListID = " " };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddTodoEntry_UnknownList_ReturnsBadRequest()
        {
            // Arrange
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Buy milk", TodoListID = "nope" };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(_db.TodoEntries);
        }

        [Fact]
        public async Task AddTodoEntry_Valid_PersistsTrimmedFields()
        {
            // Arrange
            SeedList("L1");
            AddTodoEntryRequest request = new AddTodoEntryRequest
            {
                Name = "  Buy milk  ",
                Description = "  2 litres  ",
                TodoListID = "L1"
            };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);

            Assert.Equal("Buy milk", body.Name);
            Assert.Equal("2 litres", body.Description);
            Assert.Equal("L1", body.TodoListID);
            Assert.Equal(1, body.Status);

            TodoEntry? stored = ReloadEntry(body.ID);
            Assert.NotNull(stored);
            Assert.Equal("Buy milk", stored!.Name);
        }

        [Fact]
        public async Task AddTodoEntry_BlankDescription_StoredAsNull()
        {
            // Arrange
            SeedList("L1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", Description = "   ", TodoListID = "L1" };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
            TodoEntryResponse body = Assert.IsType<TodoEntryResponse>(ok.Value);
            Assert.Null(body.Description);
        }

        [Fact]
        public async Task AddTodoEntry_InWorkspaceList_BroadcastsCreated()
        {
            // Arrange
            SeedWorkspace("ws-1");
            SeedList("L1", DefaultUserID, "ws-1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", TodoListID = "L1" };

            // Act
            ActionResult<TodoEntryResponse> result = await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            SignalRMock.AssertBroadcast(_hubSpy, "TodoEntryCreated", Times.Once());
        }

        [Fact]
        public async Task AddTodoEntry_PersonalList_NoBroadcast()
        {
            // Arrange
            SeedList("L1");
            AddTodoEntryRequest request = new AddTodoEntryRequest { Name = "Task", TodoListID = "L1" };

            // Act
            await _controller.AddTodoEntry(request, CancellationToken.None);

            // Assert
            SignalRMock.AssertSilent(_hubSpy);
        }

