/* EventsControllerTests
 * Unit tests for every action of: EventsController.
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
    public class EventsControllerTests : IDisposable
    {
        private const string DefaultUserID = "user-1";
        private const string OldTimestamp = "2000-01-01T00:00:00.0000000Z";

        private readonly LumifyDbContext _db;
        private readonly IHubContext<EventHub> _hub;
        private readonly Mock<IClientProxy> _hubSpy;
        private readonly EventsController _controller;


        public EventsControllerTests()
        {
            _db = TestDbFactory.Create();

            (IHubContext<EventHub> hub, Mock<IClientProxy> spy) = SignalRMock.Create<EventHub>();
            _hub = hub;
            _hubSpy = spy;

            _controller = new EventsController(NullLogger<EventsController>.Instance, _db, _hub);
            ControllerContextFactory.SignIn(_controller, DefaultUserID);
        }

        // Runs after every single test.
        public void Dispose()
        {
            _db.Dispose();
        }



        // ---------------- //
        // --- AddEvent --- //
        // ---------------- //

        [Fact]
        public async Task AddEvent_MissingName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request that has only whitespaces for the name-property.
            AddEventRequest request = new AddEventRequest { Name = "   ", StartTime = "2026-06-10T09:00:00Z", EndTime = "2026-06-10T10:00:00Z" };

            // --- Act --- //
            // * We send the request with an empty name.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name is required.
            Assert.Empty(_db.Events);                                               // We expect the db to be empty, since a request without name shouldn't be saved.
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since nothing got created.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_MissingStartTime_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request that has only whitespaces for the startTime-property.
            AddEventRequest request = new AddEventRequest { Name = "Termin", StartTime = "   ", EndTime = "2026-06-10T10:00:00Z" };

            // --- Act --- //
            // * We send the request without a startTime.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the startTime is required.
            Assert.Empty(_db.Events);                                               // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_MissingEndTime_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request that has only whitespaces for the endTime-property.
            AddEventRequest request = new AddEventRequest { Name = "Termin", StartTime = "2026-06-10T09:00:00Z", EndTime = "   " };

            // --- Act --- //
            // * We send the request without an endTime.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the endTime is required.
            Assert.Empty(_db.Events);                                               // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_UnknownWorkspace_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a request to add an event into a non existing workspace.
            AddEventRequest request = new AddEventRequest { Name = "Termin", WorkspaceID = "ghost-ws", StartTime = "2026-06-10T09:00:00Z", EndTime = "2026-06-10T10:00:00Z" };

            // --- Act --- //
            // * We try to add the event into the non existing workspace.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspace does not exist.
            Assert.Empty(_db.Events);                                               // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_NotWorkspaceMember_ReturnsForbid()
        {
            // In this test we check that a user cannot add an event to a workspace he is not a member of.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");
            AddEventRequest request = new AddEventRequest { Name = "Termin", WorkspaceID = "ws-1", StartTime = "2026-06-10T09:00:00Z", EndTime = "2026-06-10T10:00:00Z" };

            // --- Act --- //
            // * We (non-member) try to add the event into the workspace.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
            Assert.Empty(_db.Events);                                               // We expect the db to be empty. (No add happened)
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_Personal_PersistsAndReturnsResponse_NoBroadcast()
        {
            // In this test we check if a personal event is persistent
            // and is not getting shared via SignalR, since it gets added into the private space.

            // --- Arrange --- //
            // * We provide a valid request for adding a personal all-day event (name/description padded with whitespaces).
            AddEventRequest request = new AddEventRequest
            {
                Name = "  Mein Termin  ",
                Description = "  Beschreibung  ",
                IsAllDay = true,
                StartTime = "  2026-06-10T09:00:00Z  ",
                EndTime = "  2026-06-10T10:00:00Z  "
            };

            // --- Act --- //
            // * We add the event to the private space.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            EventResponse body = Assert.IsType<EventResponse>(ok.Value);            // We expect the response to be type EventResponse.

            Assert.Equal("Mein Termin", body.Name);                                 // We expect the name to be trimmed.
            Assert.Equal("Beschreibung", body.Description);                         // We expect the description to be trimmed.
            Assert.Equal(DefaultUserID, body.OwnerID);                              // We expect to get the DefaultUser (TestUser) as owner.
            Assert.Null(body.WorkspaceID);                                          // We expect the workspace to be null, since we add into the private space.
            Assert.True(body.IsAllDay);                                             // We expect the all-day flag to be carried over.
            Assert.Equal(1, body.Status);                                           // We expect a new event to get the default status (1).
            Assert.Equal("2026-06-10T09:00:00Z", body.StartTime);                   // We expect the startTime to be trimmed.
            Assert.Equal("2026-06-10T10:00:00Z", body.EndTime);                     // We expect the endTime to be trimmed.
            Assert.False(string.IsNullOrWhiteSpace(body.ID));                       // We expect the ID to contain a valid value.

            Event? stored = ReloadEvent(body.ID);                                   // We check if the event is existent/persistent in the database.
            Assert.NotNull(stored);                                                 // We expect the event to be present and NotNull.
            Assert.Equal("Mein Termin", stored!.Name);                              // We expect the stored event to keep the trimmed name.
            Assert.Equal(1, stored.IsAllDay);                                       // We expect the all-day flag to be stored as 1 (DB uses an integer).

            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since the event is private.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_PersonalWithoutDescription_PersistsNullDescription()
        {
            // In this test we check that an empty description is normalized to null.

            // --- Arrange --- //
            // * We provide a valid request, but leave the description as whitespaces only.
            AddEventRequest request = new AddEventRequest
            {
                Name = "Termin",
                Description = "   ",
                StartTime = "2026-06-10T09:00:00Z",
                EndTime = "2026-06-10T10:00:00Z"
            };

            // --- Act --- //
            // * We add the event.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            EventResponse body = Assert.IsType<EventResponse>(ok.Value);            // We expect the response to be type EventResponse.
            Assert.Null(body.Description);                                          // We expect the empty description to be normalized to null.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task AddEvent_InWorkspace_BroadcastsCreated()
        {
            // In this test we check that an event added to a workspace gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and add the current user as a member.
            // * We create a request to add an event into this workspace.
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            AddEventRequest request = new AddEventRequest { Name = "Sprint Termin", WorkspaceID = "ws-1", StartTime = "2026-06-10T09:00:00Z", EndTime = "2026-06-10T10:00:00Z" };

            // --- Act --- //
            // * We add the event to the workspace.
            ActionResult<EventResponse> result = await _controller.AddEvent(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            EventResponse body = Assert.IsType<EventResponse>(ok.Value);            // We expect the response to be type EventResponse.
            Assert.Equal("ws-1", body.WorkspaceID);                                 // We expect the event to belong to our workspace.

            SignalRMock.AssertBroadcast(_hubSpy, "EventCreated", Times.Once());     // We expect a broadcast, since the event lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ----------------- //
        // --- SaveEvent --- //
        // ----------------- //

        [Fact]
        public async Task SaveEvent_MissingId_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We create a save-request without an ID.
            SaveEventRequest request = new SaveEventRequest { ID = "" };

            // --- Act --- //
            // * We try to save without telling which event should be updated.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the ID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_NotFound_ReturnsNotFound()
        {
            // --- Arrange --- //
            // * We create a save-request for an event-ID that doesn't exist.
            SaveEventRequest request = new SaveEventRequest { ID = "ghost" };

            // --- Act --- //
            // * We try to save the non existing event.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the event could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot edit an event owned by someone else.

            // --- Arrange --- //
            // * We seed an event that belongs to another user.
            SeedEvent("e-1", "someone-else");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", Name = "Hacked" };

            // --- Act --- //
            // * We (the DefaultUser) try to save the foreign event.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only the owner is allowed to edit.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_EmptyName_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own event and create a request that tries to set the name to whitespaces only.
            SeedEvent("e-1");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", Name = "   " };

            // --- Act --- //
            // * We try to save the event with an empty name.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the name cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_EmptyStartTime_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own event and create a request that tries to set the startTime to whitespaces only.
            SeedEvent("e-1");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", StartTime = "   " };

            // --- Act --- //
            // * We try to save the event with an empty startTime.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the startTime cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_EmptyEndTime_ReturnsBadRequest()
        {
            // --- Arrange --- //
            // * We seed our own event and create a request that tries to set the endTime to whitespaces only.
            SeedEvent("e-1");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", EndTime = "   " };

            // --- Act --- //
            // * We try to save the event with an empty endTime.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the endTime cannot be emptied.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_Valid_PersistsFields()
        {
            // In this test we check that a real change gets persisted with trimmed/normalized fields.

            // --- Arrange --- //
            // * We seed our own event.
            // * We request changes to every editable field (name padded with whitespaces, description emptied to null).
            SeedEvent("e-1");
            SaveEventRequest request = new SaveEventRequest
            {
                ID = "e-1",
                Name = "  Renamed  ",
                Description = "   ",
                IsAllDay = true,
                StartTime = "2026-07-01T08:00:00Z",
                EndTime = "2026-07-01T09:00:00Z"
            };

            // --- Act --- //
            // * We save the changed event.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            EventResponse body = Assert.IsType<EventResponse>(ok.Value);            // We expect the response to be type EventResponse.

            Assert.Equal("Renamed", body.Name);                                     // We expect the name to be trimmed.
            Assert.Null(body.Description);                                          // We expect the emptied description to be normalized to null.
            Assert.True(body.IsAllDay);                                             // We expect the all-day flag to be updated.
            Assert.Equal("2026-07-01T08:00:00Z", body.StartTime);                   // We expect the startTime to be updated.
            Assert.Equal("2026-07-01T09:00:00Z", body.EndTime);                     // We expect the endTime to be updated.

            Event? stored = ReloadEvent("e-1");                                     // We reload the event to inspect what was persisted.
            Assert.Equal("Renamed", stored!.Name);                                  // We expect the stored name to be updated.
            Assert.NotEqual(stored.CreatedAt, stored.UpdatedAt);                    // We expect UpdatedAt to have advanced past the seeded value.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_NoEffectiveChange_DoesNotTouchTimestampOrBroadcast()
        {
            // In this test we check that saving without a real change neither touches the timestamp nor broadcasts.

            // --- Arrange --- //
            // * We seed a workspace and an event inside it.
            // * We request a save that reuses the seeded name -> nothing actually changes.
            SeedWorkspace("ws-1");
            Event seeded = SeedEvent("e-1", DefaultUserID, "ws-1");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", Name = seeded.Name };

            // --- Act --- //
            // * We save the event without any effective change.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect a success status, even though nothing changed.

            Event? stored = ReloadEvent("e-1");                                     // We reload the event to inspect its timestamp.
            Assert.Equal(OldTimestamp, stored!.UpdatedAt);                          // We expect UpdatedAt to stay untouched (still the seeded value).
            SignalRMock.AssertSilent(_hubSpy);                                      // We expect SignalR to stay silent, since there was no change to share.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task SaveEvent_WorkspaceChange_BroadcastsUpdated()
        {
            // In this test we check that a real change on a workspace-event gets broadcasted via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and an event inside it, then request a rename.
            SeedWorkspace("ws-1");
            SeedEvent("e-1", DefaultUserID, "ws-1");
            SaveEventRequest request = new SaveEventRequest { ID = "e-1", Name = "Renamed" };

            // --- Act --- //
            // * We save the renamed workspace-event.
            ActionResult<EventResponse> result = await _controller.SaveEvent(request, CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result.Result);                           // We expect to get a success status.
            SignalRMock.AssertBroadcast(_hubSpy, "EventUpdated", Times.Once());     // We expect a broadcast, since the event lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ------------------- //
        // --- DeleteEvent --- //
        // ------------------- //

        [Fact]
        public async Task DeleteEvent_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We try to delete an event, but pass only whitespace as the ID.
            ActionResult result = await _controller.DeleteEvent("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result);                          // We expect a BadRequest, since the eventID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteEvent_NotFound_ReturnsNotFound()
        {
            // --- Act --- //
            // * We try to delete an event-ID that doesn't exist.
            ActionResult result = await _controller.DeleteEvent("ghost", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundResult>(result);                                  // We expect a NotFound, since the event could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteEvent_NotOwner_ReturnsForbid()
        {
            // In this test we check that a user cannot delete an event owned by someone else.

            // --- Arrange --- //
            // * We seed an event that belongs to another user.
            SeedEvent("e-1", "someone-else");

            // --- Act --- //
            // * We (the DefaultUser) try to delete the foreign event.
            ActionResult result = await _controller.DeleteEvent("e-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result);                                    // We expect a Forbid, since only the owner is allowed to delete.
            Assert.Null(ReloadEvent("e-1")!.DeletedAt);                             // We expect the event to stay untouched (DeletedAt still null).
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteEvent_Valid_SoftDeletes()
        {
            // In this test we check that deleting an event only soft-deletes it (the row stays present).

            // --- Arrange --- //
            // * We seed our own event.
            SeedEvent("e-1");

            // --- Act --- //
            // * We delete the event.
            ActionResult result = await _controller.DeleteEvent("e-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<OkObjectResult>(result);                                  // We expect to get a success status.
            Assert.NotNull(ReloadEvent("e-1")!.DeletedAt);                          // We expect DeletedAt to be set -> soft-deleted, row still present.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task DeleteEvent_InWorkspace_BroadcastsDeleted()
        {
            // In this test we check that deleting a workspace-event broadcasts the deletion via SignalR.

            // --- Arrange --- //
            // * We seed a workspace and an event inside it.
            SeedWorkspace("ws-1");
            SeedEvent("e-1", DefaultUserID, "ws-1");

            // --- Act --- //
            // * We delete the workspace-event.
            await _controller.DeleteEvent("e-1", CancellationToken.None);

            // --- Assert --- //
            SignalRMock.AssertBroadcast(_hubSpy, "EventDeleted", Times.Once());     // We expect a broadcast, since the event lives in a shared workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------- //
        // --- GetEventCount --- //
        // --------------------- //

        [Fact]
        public async Task GetEventCountOfUser_CountsOnlyPersonalActiveEvents()
        {
            // In this test we check that the count only includes the user's personal and active events.

            // --- Arrange --- //
            // * We seed two personal/active events and one per exclusion case.
            SeedWorkspace("ws-1");
            SeedEvent("p1");
            SeedEvent("p2");
            SeedEvent("w1", DefaultUserID, "ws-1");                                  // excluded (workspace)
            SeedEvent("d1", DefaultUserID, null, deletedAt: "2001-01-01T00:00:00Z"); // excluded (deleted)

            // --- Act --- //
            // * We ask for the count of the current user's personal events.
            ActionResult<int> result = await _controller.GetEventCountOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two personal/active events.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetEventCountOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace count, but pass only whitespace as the ID.
            ActionResult<int> result = await _controller.GetEventCountOfWorkspace("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetEventCountOfWorkspace_UnknownWorkspace_ReturnsNotFound()
        {
            // --- Act --- //
            // * We ask for the count of a workspace that doesn't exist.
            ActionResult<int> result = await _controller.GetEventCountOfWorkspace("ws-x", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the workspace could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetEventCountOfWorkspace_NotMember_ReturnsForbid()
        {
            // In this test we check that a non-member cannot read the workspace's event count.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");

            // --- Act --- //
            // * We (non-member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetEventCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetEventCountOfWorkspace_Member_ReturnsCount()
        {
            // In this test we check that a member gets the correct count of active workspace-events.

            // --- Arrange --- //
            // * We seed a workspace and add the current user as a member.
            // * We seed two active events and one deleted one (which should be excluded).
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedEvent("e-1", DefaultUserID, "ws-1");
            SeedEvent("e-2", DefaultUserID, "ws-1");
            SeedEvent("e-3", DefaultUserID, "ws-1", deletedAt: "2001-01-01T00:00:00Z");   // excluded

            // --- Act --- //
            // * We (member) ask for the count of the workspace.
            ActionResult<int> result = await _controller.GetEventCountOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            int count = Assert.IsType<int>(ok.Value);                               // We expect the body to be an int.
            Assert.Equal(2, count);                                                 // We expect exactly the two active workspace-events.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // --------------------- //
        // --- GetAllEvents --- //
        // --------------------- //

        [Fact]
        public async Task GetAllEventsOfUser_ReturnsOnlyOwnPersonalActiveEvents_OrderedByStart()
        {
            // In this test we check that the query returns ONLY the user's own, personal and active events,
            // and that they come back ordered by their start date.

            // --- Arrange --- //
            // * We seed the owner and a second user (the query joins Users, so the owner must exist).
            // * We seed one event per exclusion case plus two valid ones (seeded out of order).
            SeedUser();
            SeedUser("other");
            SeedWorkspace("ws-1");

            SeedEvent("late", DefaultUserID, null, "2026-06-20T09:00:00Z");                          // owned, but later
            SeedEvent("early", DefaultUserID, null, "2026-06-01T09:00:00Z");                         // owned, earlier
            SeedEvent("workspace", DefaultUserID, "ws-1", "2026-06-05T09:00:00Z");                   // public event
            SeedEvent("deleted", DefaultUserID, null, "2026-06-03T09:00:00Z", deletedAt: "2001-01-01T00:00:00Z"); // deleted
            SeedEvent("foreign", "other", null, "2026-06-02T09:00:00Z");                             // not the owner

            // --- Act --- //
            // * We query all personal events of the current user.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<EventResponse> events = Assert.IsAssignableFrom<List<EventResponse>>(ok.Value);     // We expect the body to be a list of EventResponse.

            Assert.Equal(2, events.Count);                                                          // We expect exactly the two personal/active/own events.
            Assert.Equal("early", events[0].ID);                                                    // We expect the earlier event to come first.
            Assert.Equal("late", events[1].ID);                                                     // We expect the later event to come second.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllEventsOfUser_ExcludesEventsOfDeletedOwner()
        {
            // In this test we check that events of a soft-deleted owner are excluded by the Users-join.

            // --- Arrange --- //
            // * We seed the owner as soft-deleted and give him a (otherwise valid) personal event.
            SeedUser(DefaultUserID, "2001-01-01T00:00:00Z");
            SeedEvent("personal");

            // --- Act --- //
            // * We query all personal events of the (deleted) current user.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfUser(CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<EventResponse> events = Assert.IsAssignableFrom<List<EventResponse>>(ok.Value);     // We expect the body to be a list of EventResponse.
            Assert.Empty(events);                                                                   // We expect no event, since the owner is soft-deleted.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllEventsOfWorkspace_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We query the workspace events, but pass only whitespace as the ID.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfWorkspace("  ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the workspaceID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllEventsOfWorkspace_UnknownWorkspace_ReturnsNotFound()
        {
            // --- Act --- //
            // * We query the events of a workspace that doesn't exist.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfWorkspace("ws-x", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since the workspace could not be resolved.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllEventsOfWorkspace_NotMember_ReturnsForbid()
        {
            // In this test we check that a non-member cannot read the workspace's events.

            // --- Arrange --- //
            // * We seed a workspace, but the current user is NOT added as a member.
            SeedWorkspace("ws-1");

            // --- Act --- //
            // * We (non-member) query the events of the workspace.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<ForbidResult>(result.Result);                             // We expect a Forbid, since only members are allowed.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetAllEventsOfWorkspace_ReturnsActiveWorkspaceEvents()
        {
            // In this test we check that the query returns only the active events of the given workspace.

            // --- Arrange --- //
            // * We seed the owner, a workspace and add the current user as a member, then one event per case.
            SeedUser();
            SeedWorkspace("ws-1");
            SeedMember("ws-1", DefaultUserID);
            SeedEvent("a", DefaultUserID, "ws-1");
            SeedEvent("b", DefaultUserID, "ws-1", deletedAt: "2001-01-01T00:00:00Z");   // public event, but deleted
            SeedEvent("c");                                                             // personal event

            // --- Act --- //
            // * We query all events of the workspace.
            ActionResult<List<EventResponse>> result = await _controller.GetAllEventsOfWorkspace("ws-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);                       // We expect to get a success status.
            List<EventResponse> events = Assert.IsAssignableFrom<List<EventResponse>>(ok.Value);     // We expect the body to be a list of EventResponse.
            Assert.Single(events);                                                                  // We expect exactly one event to survive the filters.
            Assert.Equal("a", events[0].ID);                                                        // We expect it to be the active workspace-event.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------




        // ---------------------------- //
        // --- GetSpaceInfosOfEvent --- //
        // ---------------------------- //

        [Fact]
        public async Task GetSpaceInfosOfEvent_MissingId_ReturnsBadRequest()
        {
            // --- Act --- //
            // * We ask for the workspace info, but pass only whitespace as the eventID.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfEvent(" ", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<BadRequestObjectResult>(result.Result);                   // We expect a BadRequest, since the eventID is required.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfEvent_PersonalEvent_ReturnsNotFound()
        {
            // In this test we check that a personal event has no workspace to resolve.

            // --- Arrange --- //
            // * We seed a personal event (no workspace).
            SeedEvent("e-1");

            // --- Act --- //
            // * We ask for the workspace info of the personal event.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfEvent("e-1", CancellationToken.None);

            // --- Assert --- //
            Assert.IsType<NotFoundObjectResult>(result.Result);                     // We expect a NotFound, since a personal event has no workspace.
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        [Fact]
        public async Task GetSpaceInfosOfEvent_WorkspaceEvent_ReturnsWorkspace()
        {
            // In this test we check that a workspace-event resolves to its workspace.

            // --- Arrange --- //
            // * We seed a workspace and an event inside that workspace.
            SeedWorkspace("ws-1");
            SeedEvent("e-1", DefaultUserID, "ws-1");

            // --- Act --- //
            // * We ask for the workspace info of the workspace-event.
            ActionResult<WorkspaceResponse> result = await _controller.GetSpaceInfosOfEvent("e-1", CancellationToken.None);

            // --- Assert --- //
            OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);       // We expect to get a success status.
            WorkspaceResponse body = Assert.IsType<WorkspaceResponse>(ok.Value);    // We expect the body to be type WorkspaceResponse.
            Assert.Equal("ws-1", body.ID);                                          // We expect to get back the workspace the event belongs to.
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

        // Inserts an Event.
        private Event SeedEvent(
            string id,
            string ownerID = DefaultUserID,
            string? workspaceID = null,
            string startDate = "2026-06-10T09:00:00.0000000Z",
            string? endDate = "2026-06-10T10:00:00.0000000Z",
            int isAllDay = 0,
            string? deletedAt = null)
        {
            Event calendarEvent = new Event
            {
                ID = id,
                OwnerID = ownerID,
                WorkspaceID = workspaceID,
                Name = "event_" + id,
                Description = null,
                Status = 1,
                StartDate = startDate,
                EndDate = endDate,
                IsAllDay = isAllDay,
                DueDate = null,
                CreatedAt = OldTimestamp,
                UpdatedAt = OldTimestamp,
                DeletedAt = deletedAt
            };

            _db.Events.Add(calendarEvent);
            _db.SaveChanges();

            return calendarEvent;
        }

        // Reloads an Event from the context so a test can inspect what was persisted.
        private Event? ReloadEvent(string id)
        {
            return _db.Events.Find(id);
        }
    }
}
