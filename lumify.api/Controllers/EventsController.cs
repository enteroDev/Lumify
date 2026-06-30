using System.Globalization;
using lumify.api.Hubs;
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;


namespace lumify.api.Controllers
{
    /// <summary>
    /// Manages calendar events, both personal (no workspace) and workspace-shared, including
    /// creation, editing, deletion and various queries (counts, lists, linked workspace). All
    /// endpoints require an authenticated user. Changes to workspace events are pushed live to
    /// the workspace group over the <see cref="Hubs.EventHub"/>.
    /// </summary>
    /// <remarks>
    /// Permission rule throughout: a personal event may only be modified by its owner, while a
    /// workspace event may be modified by any active member of that workspace (see <c>CanModify</c>).
    /// For privacy, the creator is surfaced only as display name → username → ID, never the
    /// real first/last name.
    /// </remarks>
    [ApiController]
    [Route("events/[action]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ILogger<EventsController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<EventHub> _eventHub;

        /// <summary>
        /// Creates the controller with its injected logger, database context and the event hub
        /// used for live notifications.
        /// </summary>
        public EventsController(ILogger<EventsController> logger, LumifyDbContext db, IHubContext<EventHub> eventHub)
        {
            _logger = logger;
            _db = db;
            _eventHub = eventHub;
        }



        // ----------- //
        // --- ADD --- //
        // ----------- //
        /// <summary>
        /// Creates a new calendar event, optionally inside a workspace.
        /// </summary>
        /// <remarks>
        /// Start and end times must be valid dates and end must not be before start. If a
        /// workspace is given, the user must be a member of it. On success in a workspace, an
        /// <c>EventCreated</c> event is broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">Event data (name, start/end time required; optional description, all-day, workspace).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created event; 400 on missing/invalid fields; 403 if the user
        /// is not a member of the target workspace.</returns>
        [HttpPost]
        [ActionName("addEvent")]
        public async Task<ActionResult<EventResponse>> AddEvent([FromBody] AddEventRequest request, CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.StartTime))
            {
                return BadRequest("StartTime is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EndTime))
            {
                return BadRequest("EndTime is required.");
            }

            // Non-empty is not enough: the form produces strings like "T00:00" when the
            // date field is left blank, which must NOT be accepted as a valid event.
            if (!DateTime.TryParse(request.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
            {
                return BadRequest("StartTime is invalid.");
            }

            if (!DateTime.TryParse(request.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
            {
                return BadRequest("EndTime is invalid.");
            }

            if (parsedEnd < parsedStart)
            {
                return BadRequest("EndTime must not be before StartTime.");
            }

            // Get workspaceID of request
            var workspaceID = string.IsNullOrWhiteSpace(request.WorkspaceID) ? null : request.WorkspaceID.Trim();

            // Handle missing workspaceID - Check if workspace exists and/or if the user is part of the workspace
            if (!string.IsNullOrWhiteSpace(workspaceID))
            {
                var workspaceExists = await _db.Workspaces.AnyAsync(x => x.ID == workspaceID && x.DeletedAt == null, ct);

                if (!workspaceExists)
                {
                    return BadRequest("WorkspaceID not found.");
                }

                var isMember = await _db.WorkspaceMembers.AnyAsync(x => x.WorkspaceID == workspaceID && x.UserID == userID && x.DeletedAt == null, ct);

                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create event based on the request
            var calendarEvent = new Event
            {
                ID = Guid.NewGuid().ToString(),
                OwnerID = userID,
                WorkspaceID = workspaceID,

                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = 1,

                StartDate = request.StartTime.Trim(),
                EndDate = request.EndTime.Trim(),
                IsAllDay = request.IsAllDay ? 1 : 0,
                DueDate = null,

                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new event to the EF-Context and save it into the database
            _db.Events.Add(calendarEvent);
            await _db.SaveChangesAsync(ct);

            // Resolve the creator's display name for the response.
            // Privacy: never expose the real name (FirstName/LastName) - only DisplayName, then Username, then ID.
            var owner = await _db.Users.FirstOrDefaultAsync(x => x.ID == calendarEvent.OwnerID, ct);

            // Create result object
            var result = new EventResponse
            {
                ID = calendarEvent.ID,
                OwnerID = calendarEvent.OwnerID,
                CreatedBy = owner?.DisplayName ?? owner?.Username ?? calendarEvent.OwnerID,
                WorkspaceID = calendarEvent.WorkspaceID,

                Name = calendarEvent.Name,
                Description = calendarEvent.Description,
                Status = calendarEvent.Status,
                IsAllDay = calendarEvent.IsAllDay == 1,

                StartTime = calendarEvent.StartDate,
                EndTime = calendarEvent.EndDate,

                CreatedAt = calendarEvent.CreatedAt,
                UpdatedAt = calendarEvent.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(calendarEvent.WorkspaceID))
            {
                await _eventHub.Clients.Group(calendarEvent.WorkspaceID).SendAsync("EventCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //
        /// <summary>
        /// Soft-deletes a calendar event.
        /// </summary>
        /// <remarks>The record is kept and marked with <c>DeletedAt</c>. On a workspace event,
        /// an <c>EventDeleted</c> event is broadcast to the workspace group.</remarks>
        /// <param name="eventID">The event to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the event ID; 400 if the ID is missing;
        /// 403 if the user may not delete it; 404 if the event does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteEvent")]
        public async Task<ActionResult> DeleteEvent(string eventID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(eventID))
            {
                return BadRequest("eventID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the event in the database
            var eventEntry = await _db.Events
                .FirstOrDefaultAsync(x => x.ID == eventID && x.DeletedAt == null, ct);

            if (eventEntry == null)
            {
                return NotFound();
            }

            // Personal event: only the owner may delete. Workspace event: any workspace member may.
            if (!await CanModify(eventEntry.WorkspaceID, eventEntry.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Soft-delete the event by setting DeletedAt instead of removing the record
            var now = DateTime.UtcNow.ToString("o");
            eventEntry.DeletedAt = now;

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(eventEntry.WorkspaceID))
            {
                await _eventHub.Clients.Group(eventEntry.WorkspaceID).SendAsync("EventDeleted", new
                {
                    eventID = eventEntry.ID
                }, ct);
            }

            // Return the deleted eventID as confirmation
            return Ok(new
            {
                success = true,
                eventID = eventID
            });
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
        /// <summary>
        /// Updates an event's name, description, all-day flag and/or start/end times. Only the
        /// provided fields are changed, and the database is only written if something changed.
        /// </summary>
        /// <remarks>On a successful change in a workspace, an <c>EventUpdated</c> event is
        /// broadcast to the workspace group.</remarks>
        /// <param name="request">The event ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated event; 400 on invalid input; 403 if the user may not
        /// modify it; 404 if the event does not exist.</returns>
        [HttpPatch]
        [ActionName("saveEvent")]
        public async Task<ActionResult<EventResponse>> SaveEvent([FromBody] SaveEventRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the event in the database
            var eventEntry = await _db.Events
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (eventEntry == null)
            {
                return NotFound("Event not found");
            }

            // Personal event: only the owner may modify. Workspace event: any workspace member may.
            if (!await CanModify(eventEntry.WorkspaceID, eventEntry.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Track whether any field actually changed to avoid unnecessary DB writes
            var changed = false;



            // ::: Apply ::: //

            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("Name cannot be empty");
                }

                if (eventEntry.Name != trimmed)
                {
                    eventEntry.Name = trimmed;
                    changed = true;
                }
            }

            if (request.Description != null)
            {
                var trimmed = request.Description.Trim();
                var targetDescription = trimmed.Length == 0 ? null : trimmed;

                if (eventEntry.Description != targetDescription)
                {
                    eventEntry.Description = targetDescription;
                    changed = true;
                }
            }

            if (request.IsAllDay.HasValue)
            {
                var targetIsAllDay = request.IsAllDay.Value ? 1 : 0;

                if (eventEntry.IsAllDay != targetIsAllDay)
                {
                    eventEntry.IsAllDay = targetIsAllDay;
                    changed = true;
                }
            }

            if (request.StartTime != null)
            {
                var trimmed = request.StartTime.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("StartTime cannot be empty");
                }

                if (eventEntry.StartDate != trimmed)
                {
                    eventEntry.StartDate = trimmed;
                    changed = true;
                }
            }

            if (request.EndTime != null)
            {
                var trimmed = request.EndTime.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("EndTime cannot be empty");
                }

                if (eventEntry.EndDate != trimmed)
                {
                    eventEntry.EndDate = trimmed;
                    changed = true;
                }
            }




            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                var now = DateTime.UtcNow.ToString("o");
                eventEntry.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
            }



            // ::: Respond ::: //

            // Resolve the owner's display name for the response.
            // Privacy: never expose the real name (FirstName/LastName) - only DisplayName, then Username, then ID.
            var owner = await _db.Users.FirstOrDefaultAsync(x => x.ID == eventEntry.OwnerID, ct);

            // Create result object
            var result = new EventResponse
            {
                ID = eventEntry.ID,
                OwnerID = eventEntry.OwnerID,
                CreatedBy = owner?.DisplayName ?? owner?.Username ?? eventEntry.OwnerID,
                WorkspaceID = eventEntry.WorkspaceID,

                Name = eventEntry.Name,
                Description = eventEntry.Description,
                Status = eventEntry.Status,
                IsAllDay = eventEntry.IsAllDay == 1,

                StartTime = eventEntry.StartDate,
                EndTime = eventEntry.EndDate,

                CreatedAt = eventEntry.CreatedAt,
                UpdatedAt = eventEntry.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (changed && !string.IsNullOrWhiteSpace(eventEntry.WorkspaceID))
            {
                await _eventHub.Clients.Group(eventEntry.WorkspaceID).SendAsync("EventUpdated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        /// <summary>
        /// Returns the number of personal events (no workspace) owned by the current user.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the event count.</returns>
        [HttpGet]
        [ActionName("getEventCountOfUser")]
        public async Task<ActionResult<int>> GetEventCountOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Count only personal events (WorkspaceID == null) of the user
            var eventCount = await _db.Events.CountAsync(x =>
                x.OwnerID == userID &&
                x.WorkspaceID == null &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(eventCount);
        }

        /// <summary>
        /// Returns the number of events in a workspace. The user must be a member.
        /// </summary>
        /// <param name="workspaceID">The workspace to count events for.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the event count; 400 if the ID is missing; 403 if the user is not
        /// a member; 404 if the workspace does not exist.</returns>
        [HttpGet]
        [ActionName("getEventCountOfWorkspace")]
        public async Task<ActionResult<int>> GetEventCountOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Check params
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("workspaceID is required");
            }

            // Get userID and prepare workspaceID
            var userID = GetCurrentUserID();
            var trimmedWorkspaceID = workspaceID.Trim();

            // Check if workspace exists
            var workspaceExists = await _db.Workspaces.AnyAsync(x =>
                x.ID == trimmedWorkspaceID &&
                x.DeletedAt == null,
                ct
            );
            if (!workspaceExists)
            {
                return NotFound("Workspace not found");
            }

            // Check if user has permission
            var isMember = await _db.WorkspaceMembers.AnyAsync(x =>
                x.WorkspaceID == trimmedWorkspaceID &&
                x.UserID == userID &&
                x.DeletedAt == null,
                ct
            );
            if (!isMember)
            {
                return Forbid();
            }

            var eventCount = await _db.Events.CountAsync(x =>
                x.WorkspaceID == trimmedWorkspaceID &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(eventCount);
        }

        /// <summary>
        /// Returns all personal events (no workspace) of the current user, ordered by start date.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of events.</returns>
        [HttpGet]
        [ActionName("getAllEventsOfUser")]
        public async Task<ActionResult<List<EventResponse>>> GetAllEventsOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Query all personal events of the user, joined with the user table to exclude deleted users, sorted by start date
            var events = await (
                from calendarEvent in _db.Events
                join user in _db.Users on calendarEvent.OwnerID equals user.ID
                where calendarEvent.OwnerID == userID
                    && calendarEvent.WorkspaceID == null
                    && calendarEvent.DeletedAt == null
                    && user.DeletedAt == null
                orderby calendarEvent.StartDate
                select new EventResponse
                {
                    ID = calendarEvent.ID,
                    OwnerID = calendarEvent.OwnerID,
                    // Privacy: never expose the real name - only DisplayName, then Username, then ID.
                    CreatedBy = user.DisplayName ?? user.Username ?? user.ID,
                    WorkspaceID = calendarEvent.WorkspaceID,

                    Name = calendarEvent.Name,
                    Description = calendarEvent.Description,

                    Status = calendarEvent.Status,
                    IsAllDay = calendarEvent.IsAllDay == 1, // Mapping from DB to FE. (DB has integer 0 or 1 - In FE we use true or false.)

                    StartTime = calendarEvent.StartDate,
                    EndTime = calendarEvent.EndDate,

                    CreatedAt = calendarEvent.CreatedAt,
                    UpdatedAt = calendarEvent.UpdatedAt
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(events);
        }

        /// <summary>
        /// Returns all events of a workspace, ordered by start date. The user must be a member.
        /// </summary>
        /// <remarks>Events whose creator was soft-deleted are kept (content belongs to the
        /// workspace) and surface the creator as "Gelöschter Benutzer".</remarks>
        /// <param name="workspaceID">The workspace whose events are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of events; 400 if the ID is missing; 403 if the user is
        /// not a member; 404 if the workspace does not exist.</returns>
        [HttpGet]
        [ActionName("getAllEventsOfWorkspace")]
        public async Task<ActionResult<List<EventResponse>>> GetAllEventsOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("workspaceID is required");
            }

            // Get userID and prepare workspaceID
            var userID = GetCurrentUserID();
            var trimmedWorkspaceID = workspaceID.Trim();

            // Check if workspace exists
            var workspaceExists = await _db.Workspaces.AnyAsync(x => x.ID == trimmedWorkspaceID && x.DeletedAt == null, ct);

            if (!workspaceExists)
            {
                return NotFound("Workspace not found");
            }

            // Check if user has permission
            var isMember = await _db.WorkspaceMembers.AnyAsync(x => x.WorkspaceID == trimmedWorkspaceID && x.UserID == userID && x.DeletedAt == null, ct);

            if (!isMember)
            {
                return Forbid();
            }

            // Query all events of the workspace, joined with the user table to exclude deleted users, sorted by start date
            var events = await (
                from calendarEvent in _db.Events
                join user in _db.Users on calendarEvent.OwnerID equals user.ID
                where calendarEvent.WorkspaceID == trimmedWorkspaceID
                    && calendarEvent.DeletedAt == null
                orderby calendarEvent.StartDate
                select new EventResponse
                {
                    ID = calendarEvent.ID,
                    OwnerID = calendarEvent.OwnerID,
                    // Keep events whose creator was soft-deleted (content belongs to the workspace)
                    // and surface the creator as "Gelöschter Benutzer" in that case.
                    // Privacy: never expose the real name - only DisplayName, then Username, then ID.
                    CreatedBy = user.DeletedAt != null
                        ? "Gelöschter Benutzer"
                        : user.DisplayName ?? user.Username ?? user.ID,
                    WorkspaceID = calendarEvent.WorkspaceID,

                    Name = calendarEvent.Name,
                    Description = calendarEvent.Description,

                    Status = calendarEvent.Status,
                    IsAllDay = calendarEvent.IsAllDay == 1, // Mapping from DB to FE. (DB has integer 0 or 1 - In FE we use true or false.)

                    StartTime = calendarEvent.StartDate,
                    EndTime = calendarEvent.EndDate,

                    CreatedAt = calendarEvent.CreatedAt,
                    UpdatedAt = calendarEvent.UpdatedAt
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(events);
        }

        /// <summary>
        /// Returns the workspace that the given event belongs to, if any.
        /// </summary>
        /// <param name="eventID">The event whose workspace is requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the workspace; 400 if the ID is missing; 404 if the event has no
        /// linked workspace or it was deleted.</returns>
        [HttpGet]
        [ActionName("getSpaceInfosOfEvent")]
        public async Task<ActionResult<WorkspaceResponse>> GetSpaceInfosOfEvent(string eventID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(eventID))
            {
                return BadRequest("eventID is required");
            }

            // Query the workspace that is linked to the given event via a join
            var result = await (
                from e in _db.Events
                join ws in _db.Workspaces on e.WorkspaceID equals ws.ID
                where e.ID == eventID
                    && e.DeletedAt == null
                    && ws.DeletedAt == null
                select new WorkspaceResponse
                {
                    ID = ws.ID,
                    OwnerID = ws.OwnerID,
                    Name = ws.Name,
                    CreatedAt = ws.CreatedAt,
                    UpdatedAt = ws.UpdatedAt
                }
            ).FirstOrDefaultAsync(ct);

            // Return 404 if the event has no linked workspace or workspace was deleted
            if (result == null)
            {
                return NotFound("Workspace not found");
            }

            // Return the result
            return Ok(result);
        }




        // --- Helper --- //
        /// <summary>
        /// Reads the current user's ID from the <c>UserID</c> claim of the authenticated request.
        /// </summary>
        /// <returns>The current user's ID.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when no user is logged in.</exception>
        private string GetCurrentUserID()
        {
            var userID = User.FindFirst("UserID")?.Value;
            if (string.IsNullOrWhiteSpace(userID))
            {
                throw new UnauthorizedAccessException("Kein User eingeloggt.");
            }

            return userID;
        }

        /// <summary>
        /// Decides whether the current user may modify or delete an item. A personal item
        /// (no workspace) may only be changed by its owner; a workspace item may be changed by
        /// any active member of that workspace.
        /// </summary>
        /// <param name="workspaceID">The item's workspace, or <c>null</c> for a personal item.</param>
        /// <param name="ownerID">The item's owner.</param>
        /// <param name="userID">The current user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns><c>true</c> if the user is allowed to modify the item.</returns>
        private async Task<bool> CanModify(string? workspaceID, string ownerID, string userID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return ownerID == userID;
            }

            return await _db.WorkspaceMembers.AnyAsync(
                x => x.WorkspaceID == workspaceID && x.UserID == userID && x.DeletedAt == null, ct);
        }
    }
}
