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
    /// Manages todo lists and their entries, both personal (no workspace) and workspace-shared,
    /// including creation, editing, deletion and various queries. All endpoints require an
    /// authenticated user. Changes to workspace items are pushed live to the workspace group
    /// over the <see cref="Hubs.TodoHub"/>.
    /// </summary>
    /// <remarks>
    /// Status values are integers: 1 = pending/open, 2 = done. A list's status is kept in sync
    /// with its entries: checking the last open entry marks the list done, while adding or
    /// unchecking an entry reopens it. Permission rule throughout: a personal item may only be
    /// modified by its owner, a workspace item by any active member (see <c>CanModify</c>).
    /// </remarks>
    [ApiController]
    [Route("todos/[action]")]
    [Authorize]
    public class TodoListsController : ControllerBase
    {
        private readonly ILogger<TodoListsController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<TodoHub> _todoHub;

        /// <summary>
        /// Creates the controller with its injected logger, database context and the todo hub
        /// used for live notifications.
        /// </summary>
        public TodoListsController(ILogger<TodoListsController> logger, LumifyDbContext db, IHubContext<TodoHub> todoHub)
        {
            _logger = logger;
            _db = db;
            _todoHub = todoHub;
        }


        // ----------- //
        // --- ADD --- //
        // ----------- //
        /// <summary>
        /// Creates a new todo list (status 1 = pending), optionally inside a workspace.
        /// </summary>
        /// <remarks>On success in a workspace, a <c>TodoListCreated</c> event is broadcast to
        /// the workspace group.</remarks>
        /// <param name="request">List data (name required; optional workspace).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created list; 400 if the name is missing or the workspace does
        /// not exist.</returns>
        [HttpPost]
        [ActionName("addTodoList")]
        public async Task<ActionResult<TodoListResponse>> AddTodoList([FromBody] AddTodoListRequest request, CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required.");
            }

            // Handle optional workspaceID - Check if workspace exists
            if (!string.IsNullOrWhiteSpace(request.WorkspaceID))
            {
                var workspaceExists = await _db.Workspaces
                    .AnyAsync(x => x.ID == request.WorkspaceID && x.DeletedAt == null, ct);

                if (!workspaceExists)
                {
                    return BadRequest("WorkspaceID not found.");
                }
            }

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create TodoList based on the request
            var todoList = new TodoList
            {
                ID = Guid.NewGuid().ToString(),
                OwnerID = userID,
                WorkspaceID = request.WorkspaceID,
                Name = request.Name.Trim(),
                Status = 1,
                IsArchived = 0,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new TodoList to the EF-Context and save it into the database
            _db.TodoLists.Add(todoList);
            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new TodoListResponse
            {
                ID = todoList.ID,
                OwnerID = todoList.OwnerID,
                WorkspaceID = todoList.WorkspaceID,
                Name = todoList.Name,
                Status = todoList.Status,
                IsArchived = todoList.IsArchived,
                CreatedAt = todoList.CreatedAt,
                UpdatedAt = todoList.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoListCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }

        /// <summary>
        /// Adds a new entry (status 1 = pending) to a todo list.
        /// </summary>
        /// <remarks>
        /// If the parent list was already marked done (status 2), it is reopened. On success in
        /// a workspace, a <c>TodoEntryCreated</c> event (and a <c>TodoListUpdated</c> event if
        /// the list reopened) is broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">Entry data (name and parent list ID required; optional description).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created entry; 400 if the name or list ID is missing or the
        /// list does not exist.</returns>
        [HttpPost]
        [ActionName("addTodoEntry")]
        public async Task<ActionResult<TodoEntryResponse>> AddTodoEntry([FromBody] AddTodoEntryRequest request, CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.TodoListID))
            {
                return BadRequest("TodoListID is required.");
            }

            // Check if the referenced TodoList exists
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == request.TodoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return BadRequest("TodoListID not found.");
            }

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create TodoEntry based on the request
            var todoEntry = new TodoEntry
            {
                ID = Guid.NewGuid().ToString(),
                TodoListID = request.TodoListID,
                OwnerID = userID,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Status = 1,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new TodoEntry to the EF-Context
            _db.TodoEntries.Add(todoEntry);

            // A freshly added entry is pending (Status 1). If the list was already
            // marked as done (Status 2), it can no longer be done -> reopen it.
            var todoListReopened = false;
            if (todoList.Status == 2)
            {
                todoList.Status = 1;
                todoList.UpdatedAt = now;
                todoListReopened = true;
            }

            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new TodoEntryResponse
            {
                ID = todoEntry.ID,
                TodoListID = todoEntry.TodoListID,
                OwnerID = todoEntry.OwnerID,
                Name = todoEntry.Name,
                Description = todoEntry.Description,
                Status = todoEntry.Status,
                CreatedAt = todoEntry.CreatedAt,
                UpdatedAt = todoEntry.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoEntryCreated", result, ct);

                // If the list got reopened -> notify workspace clients about the TodoList update too
                if (todoListReopened)
                {
                    var todoListResult = new TodoListResponse
                    {
                        ID = todoList.ID,
                        OwnerID = todoList.OwnerID,
                        WorkspaceID = todoList.WorkspaceID,
                        Name = todoList.Name,
                        Status = todoList.Status,
                        IsArchived = todoList.IsArchived,
                        CreatedAt = todoList.CreatedAt,
                        UpdatedAt = todoList.UpdatedAt
                    };

                    await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoListUpdated", todoListResult, ct);
                }
            }

            // Return the result
            return Ok(result);
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
        /// <summary>
        /// Updates a todo list's name, status (1 or 2) and/or archived flag. Only provided
        /// fields are changed, and the database is only written if something changed.
        /// </summary>
        /// <remarks>On a successful change in a workspace, a <c>TodoListUpdated</c> event is
        /// broadcast to the workspace group.</remarks>
        /// <param name="request">The list ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated list; 400 on invalid input (e.g. status not 1/2);
        /// 403 if the user may not modify it; 404 if the list does not exist.</returns>
        [HttpPatch]
        [ActionName("saveTodoList")]
        public async Task<ActionResult<TodoListResponse>> SaveTodoList([FromBody] SaveTodoListRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TodoList in the database
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound("TodoList not found");
            }

            // Personal list: only the owner may modify. Workspace list: any workspace member may.
            if (!await CanModify(todoList.WorkspaceID, todoList.OwnerID, userID, ct))
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

                if (todoList.Name != trimmed)
                {
                    todoList.Name = trimmed;
                    changed = true;
                }
            }

            if (request.Status.HasValue)
            {
                if (request.Status.Value != 1 && request.Status.Value != 2)
                {
                    return BadRequest("Status must be 1 or 2");
                }

                if (todoList.Status != request.Status.Value)
                {
                    todoList.Status = request.Status.Value;
                    changed = true;
                }
            }

            if (request.IsArchived.HasValue)
            {
                var targetIsArchived = request.IsArchived.Value ? 1 : 0;

                if (todoList.IsArchived != targetIsArchived)
                {
                    todoList.IsArchived = targetIsArchived;
                    changed = true;
                }
            }




            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                var now = DateTime.UtcNow.ToString("o");
                todoList.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
            }



            // ::: Respond ::: //

            // Create result object
            var result = new TodoListResponse
            {
                ID = todoList.ID,
                OwnerID = todoList.OwnerID,
                WorkspaceID = todoList.WorkspaceID,
                Name = todoList.Name,
                Status = todoList.Status,
                IsArchived = todoList.IsArchived,
                CreatedAt = todoList.CreatedAt,
                UpdatedAt = todoList.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (changed && !string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoListUpdated", result, ct);
            }

            // Return the result
            return Ok(result);
        }

        /// <summary>
        /// Updates a todo entry's name, description and/or status (1 or 2). Only provided fields
        /// are changed, and the database is only written if something changed.
        /// </summary>
        /// <remarks>
        /// Toggling the status keeps the parent list in sync: checking the last open entry marks
        /// the list done, unchecking an entry reopens it. The response's <c>WasLastUnchecked</c>
        /// flag signals the former. Relevant <c>TodoEntryUpdated</c>/<c>TodoListUpdated</c> events
        /// are broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">The entry ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated entry; 400 on invalid input (e.g. status not 1/2);
        /// 403 if the user may not modify it; 404 if the entry or its parent list does not exist.</returns>
        [HttpPatch]
        [ActionName("saveTodoEntry")]
        public async Task<ActionResult<TodoEntryResponse>> SaveTodoEntry([FromBody] SaveTodoEntryRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TodoEntry in the database
            var todoEntry = await _db.TodoEntries
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (todoEntry == null)
            {
                return NotFound("TodoEntry not found");
            }

            // Find the parent TodoList first - it carries the WorkspaceID needed for the permission check,
            // resolves the workspace for SignalR and lets us track status changes.
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == todoEntry.TodoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound("Parent TodoList not found");
            }

            // Personal entry: only the owner may modify. Workspace entry: any workspace member may.
            if (!await CanModify(todoList.WorkspaceID, todoEntry.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Track whether any field actually changed to avoid unnecessary DB writes
            var changed = false;
            var statusChanged = false;
            var wasLastUnchecked = false;



            // ::: Apply ::: //

            // TodoEntry.Name
            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("Name cannot be empty");
                }

                if (todoEntry.Name != trimmed)
                {
                    todoEntry.Name = trimmed;
                    changed = true;
                }
            }

            // TodoEntry.Description
            if (request.Description != null)
            {
                var trimmed = request.Description.Trim();
                var targetDescription = trimmed.Length == 0 ? null : trimmed;

                if (todoEntry.Description != targetDescription)
                {
                    todoEntry.Description = targetDescription;
                    changed = true;
                }
            }

            // TodoEntry.Status
            if (request.Status.HasValue)
            {
                if (request.Status.Value != 1 && request.Status.Value != 2)
                {
                    return BadRequest("Status must be 1 or 2");
                }

                if (todoEntry.Status != request.Status.Value)
                {
                    todoEntry.Status = request.Status.Value;
                    changed = true;
                    statusChanged = true;
                }
            }




            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                var now = DateTime.UtcNow.ToString("o");
                todoEntry.UpdatedAt = now;

                // If entry status changed -> Check if it was the last unchecked entry of this TodoList
                if (statusChanged)
                {
                    wasLastUnchecked = await CheckIfLastUnchecked(todoEntry.TodoListID, todoEntry.ID, todoEntry.Status, ct);

                    // If last unchecked entry was checked -> mark TodoList as done
                    if (wasLastUnchecked)
                    {
                        todoList.Status = 2;
                        todoList.UpdatedAt = now;
                    }

                    // If entry was unchecked again -> mark TodoList as pending
                    if (todoEntry.Status != 2)
                    {
                        todoList.Status = 1;
                        todoList.UpdatedAt = now;
                    }
                }

                await _db.SaveChangesAsync(ct);
            }




            // ::: Respond ::: //

            // Create result object
            var result = new TodoEntryResponse
            {
                ID = todoEntry.ID,
                OwnerID = todoEntry.OwnerID,
                TodoListID = todoEntry.TodoListID,
                Name = todoEntry.Name,
                Description = todoEntry.Description,
                Status = todoEntry.Status,
                WasLastUnchecked = wasLastUnchecked,
                CreatedAt = todoEntry.CreatedAt,
                UpdatedAt = todoEntry.UpdatedAt
            };

            // If TodoEntry changed -> Notify workspace clients
            if (changed && !string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoEntryUpdated", result, ct);
            }

            // If entry status changed -> Notify workspace clients about TodoList update
            if (statusChanged && !string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                var todoListResult = new TodoListResponse
                {
                    ID = todoList.ID,
                    OwnerID = todoList.OwnerID,
                    WorkspaceID = todoList.WorkspaceID,
                    Name = todoList.Name,
                    Status = todoList.Status,
                    IsArchived = todoList.IsArchived,
                    CreatedAt = todoList.CreatedAt,
                    UpdatedAt = todoList.UpdatedAt
                };

                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoListUpdated", todoListResult, ct);
            }

            return Ok(result);
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //
        /// <summary>
        /// Soft-deletes a todo list.
        /// </summary>
        /// <remarks>The record is kept and marked with <c>DeletedAt</c>. On a workspace list, a
        /// <c>TodoListDeleted</c> event is broadcast to the workspace group.</remarks>
        /// <param name="todoListID">The list to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the list ID; 400 if the ID is missing;
        /// 403 if the user may not delete it; 404 if the list does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteTodoList")]
        public async Task<ActionResult> DeleteTodoList(string todoListID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(todoListID))
            {
                return BadRequest("todoListID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TodoList in the database
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == todoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound();
            }

            // Personal list: only the owner may delete. Workspace list: any workspace member may.
            if (!await CanModify(todoList.WorkspaceID, todoList.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Soft-delete the TodoList by setting DeletedAt instead of removing the record
            var now = DateTime.UtcNow.ToString("o");
            todoList.DeletedAt = now;

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoListDeleted", new
                {
                    todoListID = todoList.ID
                }, ct);
            }

            // Return the deleted todoListID as confirmation
            return Ok(new
            {
                success = true,
                todoListID = todoListID
            });
        }

        /// <summary>
        /// Soft-deletes a todo entry.
        /// </summary>
        /// <remarks>The record is kept and marked with <c>DeletedAt</c>. On a workspace entry, a
        /// <c>TodoEntryDeleted</c> event is broadcast to the workspace group.</remarks>
        /// <param name="todoEntryID">The entry to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the entry ID; 400 if the ID is missing;
        /// 403 if the user may not delete it; 404 if the entry or its parent list does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteTodoEntry")]
        public async Task<ActionResult> DeleteTodoEntry(string todoEntryID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(todoEntryID))
            {
                return BadRequest("todoEntryID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TodoEntry in the database
            var todoEntry = await _db.TodoEntries
                .FirstOrDefaultAsync(x => x.ID == todoEntryID && x.DeletedAt == null, ct);

            if (todoEntry == null)
            {
                return NotFound();
            }

            // Find the parent TodoList first - it carries the WorkspaceID needed for the permission check
            // and resolves the workspace for the SignalR notification.
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == todoEntry.TodoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound("Parent TodoList not found");
            }

            // Personal entry: only the owner may delete. Workspace entry: any workspace member may.
            if (!await CanModify(todoList.WorkspaceID, todoEntry.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Soft-delete the TodoEntry by setting DeletedAt instead of removing the record
            var now = DateTime.UtcNow.ToString("o");
            todoEntry.DeletedAt = now;

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(todoList.WorkspaceID))
            {
                await _todoHub.Clients.Group(todoList.WorkspaceID).SendAsync("TodoEntryDeleted", new
                {
                    todoEntryID = todoEntry.ID
                }, ct);
            }

            // Return the deleted todoEntryID as confirmation
            return Ok(new
            {
                success = true,
                todoEntryID = todoEntryID
            });
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        /// <summary>
        /// Returns all personal todo lists (no workspace) of the current user, oldest first.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of todo lists.</returns>
        [HttpGet]
        [ActionName("getAllTodoListsOfUser")]
        public async Task<ActionResult<List<TodoListResponse>>> GetAllTodoListsOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Query all personal TodoLists of the user, joined with the user table to exclude deleted users, sorted by creation date
            var todoLists = await (
                from todoList in _db.TodoLists
                join user in _db.Users on todoList.OwnerID equals user.ID
                where todoList.OwnerID == userID
                    && todoList.DeletedAt == null
                    && todoList.WorkspaceID == null
                    && user.DeletedAt == null
                orderby todoList.CreatedAt
                select new TodoListResponse
                {
                    ID = todoList.ID,
                    OwnerID = todoList.OwnerID,
                    WorkspaceID = todoList.WorkspaceID,
                    Name = todoList.Name,
                    Status = todoList.Status,
                    IsArchived = todoList.IsArchived,
                    CreatedAt = todoList.CreatedAt,
                    UpdatedAt = todoList.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(todoLists);
        }

        /// <summary>
        /// Returns all todo lists of a workspace, oldest first.
        /// </summary>
        /// <param name="workspaceID">The workspace whose lists are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of todo lists; 400 if the ID is missing.</returns>
        [HttpGet]
        [ActionName("getAllTodoListsOfWorkspace")]
        public async Task<ActionResult<List<TodoListResponse>>> GetAllTodoListsOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("workspaceID is required");
            }

            // Query all TodoLists of the workspace, joined with the user table to exclude deleted users, sorted by creation date
            var todoLists = await (
                from todoList in _db.TodoLists
                join user in _db.Users on todoList.OwnerID equals user.ID
                where todoList.WorkspaceID == workspaceID
                    && todoList.DeletedAt == null
                orderby todoList.CreatedAt
                select new TodoListResponse
                {
                    ID = todoList.ID,
                    OwnerID = todoList.OwnerID,
                    WorkspaceID = todoList.WorkspaceID,
                    Name = todoList.Name,
                    Status = todoList.Status,
                    IsArchived = todoList.IsArchived,
                    CreatedAt = todoList.CreatedAt,
                    UpdatedAt = todoList.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(todoLists);
        }


        /// <summary>
        /// Returns all entries of the current user's personal todo lists (no workspace),
        /// oldest first.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of entries.</returns>
        [HttpGet]
        [ActionName("getAllTodoEntriesOfUser")]
        public async Task<ActionResult<List<TodoEntryResponse>>> GetAllTodoEntriesOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Query all personal TodoEntries of the user via a join through TodoLists, sorted by creation date
            var todoEntries = await (
                from todoEntry in _db.TodoEntries
                join todoList in _db.TodoLists on todoEntry.TodoListID equals todoList.ID
                join user in _db.Users on todoEntry.OwnerID equals user.ID
                where todoEntry.OwnerID == userID
                    && todoEntry.DeletedAt == null
                    && todoList.DeletedAt == null
                    && todoList.WorkspaceID == null
                    && user.DeletedAt == null
                orderby todoEntry.CreatedAt
                select new TodoEntryResponse
                {
                    ID = todoEntry.ID,
                    OwnerID = todoEntry.OwnerID,
                    TodoListID = todoEntry.TodoListID,
                    Name = todoEntry.Name,
                    Description = todoEntry.Description,
                    Status = todoEntry.Status,
                    CreatedAt = todoEntry.CreatedAt,
                    UpdatedAt = todoEntry.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(todoEntries);
        }

        /// <summary>
        /// Returns all entries of all todo lists in a workspace, oldest first.
        /// </summary>
        /// <param name="workspaceID">The workspace whose entries are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of entries; 400 if the ID is missing.</returns>
        [HttpGet]
        [ActionName("getAllTodoEntriesOfWorkspace")]
        public async Task<ActionResult<List<TodoEntryResponse>>> GetAllTodoEntriesOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("workspaceID is required");
            }

            // Query all TodoEntries of the workspace via a join through TodoLists, sorted by creation date
            var todoEntries = await (
                from todoEntry in _db.TodoEntries
                join todoList in _db.TodoLists on todoEntry.TodoListID equals todoList.ID
                join user in _db.Users on todoEntry.OwnerID equals user.ID
                where todoEntry.DeletedAt == null
                    && todoList.DeletedAt == null
                    && todoList.WorkspaceID == workspaceID
                orderby todoEntry.CreatedAt
                select new TodoEntryResponse
                {
                    ID = todoEntry.ID,
                    OwnerID = todoEntry.OwnerID,
                    TodoListID = todoEntry.TodoListID,
                    Name = todoEntry.Name,
                    Description = todoEntry.Description,
                    Status = todoEntry.Status,
                    CreatedAt = todoEntry.CreatedAt,
                    UpdatedAt = todoEntry.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(todoEntries);
        }


        /// <summary>
        /// Returns a single todo list by its ID.
        /// </summary>
        /// <param name="todoListID">The list to look up.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list; 400 if the ID is missing; 404 if it does not exist.</returns>
        [HttpGet]
        [ActionName("getTodoListWithID")]
        public async Task<ActionResult<TodoListResponse>> GetTodoListWithID(string todoListID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(todoListID))
            {
                return BadRequest("todoListID is required");
            }

            // Query the TodoList by ID, joined with the user table to exclude deleted users
            var todoList = await (
                from tdl in _db.TodoLists
                join user in _db.Users on tdl.OwnerID equals user.ID
                where tdl.ID == todoListID
                    && tdl.DeletedAt == null
                select new TodoListResponse
                {
                    ID = tdl.ID,
                    OwnerID = tdl.OwnerID,
                    WorkspaceID = tdl.WorkspaceID,
                    Name = tdl.Name,
                    Status = tdl.Status,
                    IsArchived = tdl.IsArchived,
                    CreatedAt = tdl.CreatedAt,
                    UpdatedAt = tdl.UpdatedAt
                }
            ).FirstOrDefaultAsync(ct);

            if (todoList == null)
            {
                return NotFound("TodoList not found");
            }

            // Return the result
            return Ok(todoList);
        }


        /// <summary>
        /// Returns the number of personal todo lists (no workspace) owned by the current user.
        /// Used on the feature cards of the SpaceHub.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list count.</returns>
        [HttpGet]
        [ActionName("getTodoListCountOfUser")]
        public async Task<ActionResult<int>> GetTodoListCountOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Count only personal TodoLists (WorkspaceID == null) of the user
            var todoListCount = await _db.TodoLists.CountAsync(x =>
                x.OwnerID == userID &&
                x.WorkspaceID == null &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(todoListCount);
        }

        /// <summary>
        /// Returns the number of todo lists in a workspace. The user must be a member. Used on
        /// the feature cards of the SpaceHub.
        /// </summary>
        /// <param name="workspaceID">The workspace to count lists for.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list count; 400 if the ID is missing; 403 if the user is not a
        /// member; 404 if the workspace does not exist.</returns>
        [HttpGet]
        [ActionName("getTodoListCountOfWorkspace")]
        public async Task<ActionResult<int>> GetTodoListCountOfWorkspace(string workspaceID, CancellationToken ct)
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

            var todoListCount = await _db.TodoLists.CountAsync(x =>
                x.WorkspaceID == trimmedWorkspaceID &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(todoListCount);
        }

        /// <summary>
        /// Returns the workspace that the given todo entry belongs to, if any. Used by the
        /// frontend's recents module to show where a recent item comes from.
        /// </summary>
        /// <param name="todoEntryID">The entry whose workspace is requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the workspace; 400 if the ID is missing; 404 if the entry has no
        /// linked workspace or it was deleted.</returns>
        [HttpGet]
        [ActionName("getSpaceInfosOfTodoEntry")]
        public async Task<ActionResult<WorkspaceResponse>> GetSpaceInfosOfTodoEntry(string todoEntryID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(todoEntryID))
            {
                return BadRequest("todoEntryID is required");
            }

            // Query the workspace that is linked to the given TodoEntry via a double join through TodoList
            var result = await (
                from t in _db.TodoEntries
                join list in _db.TodoLists on t.TodoListID equals list.ID
                join ws in _db.Workspaces on list.WorkspaceID equals ws.ID
                where t.ID == todoEntryID
                    && t.DeletedAt == null
                    && list.DeletedAt == null
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

            // Return 404 if the TodoEntry has no linked workspace or workspace was deleted
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

        /// <summary>
        /// Checks whether the given entry was the last still-open entry of its list — i.e. after
        /// setting it to done (status 2) no other open entries remain.
        /// </summary>
        /// <param name="todoListID">The parent list to inspect.</param>
        /// <param name="todoEntryID">The entry being changed (excluded from the check).</param>
        /// <param name="newStatus">The entry's new status; only status 2 (done) can trigger this.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns><c>true</c> if this was the last open entry and the list can be marked done.</returns>
        private async Task<bool> CheckIfLastUnchecked(string todoListID, string todoEntryID, int newStatus, CancellationToken ct)
        {
            // Only relevant if entry is now done
            if (newStatus != 2)
            {
                return false;
            }

            var hasUncheckedEntries = await _db.TodoEntries
                .AnyAsync(x => x.TodoListID == todoListID
                    && x.DeletedAt == null
                    && x.ID != todoEntryID
                    && x.Status != 2, ct);

            return !hasUncheckedEntries;
        }
    }
}