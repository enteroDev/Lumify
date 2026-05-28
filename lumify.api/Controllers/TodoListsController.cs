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
    [ApiController]
    [Route("todos/[action]")]
    [Authorize]
    public class TodoListsController : ControllerBase
    {
        private readonly ILogger<TodoListsController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<TodoHub> _todoHub;

        public TodoListsController(ILogger<TodoListsController> logger, LumifyDbContext db, IHubContext<TodoHub> todoHub)
        {
            _logger = logger;
            _db = db;
            _todoHub = todoHub;
        }


        // ----------- //
        // --- ADD --- //
        // ----------- //
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

            // Add new TodoEntry to the EF-Context and save it into the database
            _db.TodoEntries.Add(todoEntry);
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
            }

            // Return the result
            return Ok(result);
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
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

            // Only the owner is allowed to update the TodoList
            if (todoList.OwnerID != userID)
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

            // Only the owner is allowed to update the TodoEntry
            if (todoEntry.OwnerID != userID)
            {
                return Forbid();
            }

            // Find the parent TodoList to resolve workspace for SignalR and track status changes
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == todoEntry.TodoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound("Parent TodoList not found");
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

            // Only the owner is allowed to delete the TodoList
            if (todoList.OwnerID != userID)
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

            // Only the owner is allowed to delete the entry
            if (todoEntry.OwnerID != userID)
            {
                return Forbid();
            }

            // Find the parent TodoList to resolve the workspace for SignalR notification
            var todoList = await _db.TodoLists
                .FirstOrDefaultAsync(x => x.ID == todoEntry.TodoListID && x.DeletedAt == null, ct);

            if (todoList == null)
            {
                return NotFound("Parent TodoList not found");
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
                    && user.DeletedAt == null
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


        // Get count of users todo-lists -> Will be shown on the Feature Cards of the proformer SpaceHub.
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

        // Get count of workspace todo-list -> Will be shown on the Feature Cards of the proformer SpaceHub.
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

        // Will be needed to display corresponding workspace infos to a specific TodoEntry. -> Info will be shown in recents-module of Frontend. (User can be in multiple workspaces. He prob. wants to know where this recent comes from )
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
        private string GetCurrentUserID()
        {
            var userID = User.FindFirst("UserID")?.Value;

            if (string.IsNullOrWhiteSpace(userID))
            {
                throw new UnauthorizedAccessException("Kein User eingeloggt.");
            }

            return userID;
        }

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