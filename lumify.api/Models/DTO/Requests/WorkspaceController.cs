using lumify.api.Hubs;
using lumify.api.Models.Context;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace lumify.api.Controllers
{
    [ApiController]
    [Route("workspace/[action]")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly ILogger<WorkspaceController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<WorkspaceHub> _workspaceHub;

        public WorkspaceController(ILogger<WorkspaceController> logger, LumifyDbContext db, IHubContext<WorkspaceHub> workspaceHub)
        {
            _logger = logger;
            _db = db;
            _workspaceHub = workspaceHub;
        }



        // ----------- //
        // --- ADD --- //
        // ----------- //
        [HttpPost]
        [ActionName("addWorkspace")]
        public async Task<ActionResult<WorkspaceResponse>> AddWorkspace([FromBody] AddWorkspaceRequest request, CancellationToken ct)
        {
            // Get user from Claim/Token
            var userID = GetCurrentUserID();
            if (string.IsNullOrEmpty(userID))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required");
            }

            var now = DateTime.UtcNow.ToString("o");

            // Create entry for Workspace table
            var workspace = new Workspace
            {
                ID = Guid.NewGuid().ToString(),
                OwnerID = userID,
                Name = request.Name.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Create entry for WorkspaceMember table
            var ownerMember = new WorkspaceMember
            {
                ID = Guid.NewGuid().ToString(),
                WorkspaceID = workspace.ID,
                UserID = userID,
                Role = 1,           // 1 = Owner | 2 = Admin | 3 = User
                CreatedAt = now,
                DeletedAt = null
            };

            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(ownerMember);

            await _db.SaveChangesAsync(ct);

            var result = new WorkspaceResponse
            {
                ID = workspace.ID,
                OwnerID = userID,
                Name = workspace.Name,
                CreatedAt = workspace.CreatedAt,
                UpdatedAt = workspace.UpdatedAt
            };

            // Signal
            await _workspaceHub.Clients.Group(userID).SendAsync("WorkspaceCreated", result, ct);

            return Ok(result);
        }

        [HttpPost]
        [ActionName("addWorkspaceMember")]
        public async Task<ActionResult> AddWorkspaceMember([FromBody] AddWorkspaceMemberRequest request, CancellationToken ct)
        {
            // Verify User
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrEmpty(currentUserID))
            {
                return Unauthorized();
            }

            // Verify request content
            if (string.IsNullOrWhiteSpace(request.WorkspaceID))
            {
                return BadRequest("WorkspaceID is required.");
            }
            if (string.IsNullOrWhiteSpace(request.UserID))
            {
                return BadRequest("UserID is required.");
            }

            // Get workspace
            var workspace = await _db.Workspaces
                .FirstOrDefaultAsync(x => x.ID == request.WorkspaceID && x.DeletedAt == null, ct);

            // Verify workspace is given and valid.
            if (workspace == null)
            {
                return NotFound("Workspace could not be found.");
            }

            // Get workspace-role of current user
            var currentUserRole = await _db.WorkspaceMembers
                .Where(x => x.WorkspaceID == request.WorkspaceID && x.UserID == currentUserID && x.DeletedAt == null)
                .Select(x => x.Role)
                .FirstOrDefaultAsync(ct);

            // Verify user can add members
            if (currentUserRole != 1)
            {
                return Forbid();
            }

            // Get user to add and verify user exists
            var user = await _db.Users
                .FirstOrDefaultAsync(x => x.ID == request.UserID && x.DeletedAt == null, ct);

            if (user == null)
            {
                return NotFound("User could not be found.");
            }

            // Check if user is already member of space
            var memberAlreadyExists = await _db.WorkspaceMembers
                .AnyAsync(x => x.WorkspaceID == request.WorkspaceID && x.UserID == request.UserID && x.DeletedAt == null, ct);
            if (memberAlreadyExists)
            {
                return BadRequest("User is already a member of this workspace.");
            }

            // Get current time
            var now = DateTime.UtcNow.ToString("o");

            // Create object
            var workspaceMember = new WorkspaceMember
            {
                ID = Guid.NewGuid().ToString(),
                WorkspaceID = request.WorkspaceID,
                UserID = request.UserID,
                Role = 3,
                CreatedAt = now,
                DeletedAt = null
            };

            // Add to database
            _db.WorkspaceMembers.Add(workspaceMember);
            await _db.SaveChangesAsync(ct);

            // Build response
            var response = new WorkspaceMemberResponse
            {
                UserID = user.ID,
                AvatarUrl = user.AvatarUrl,
                DisplayName = user.DisplayName,
                Username = user.Username,
                Email = user.Email,
                Role = workspaceMember.Role
            };

            // Signal
            var audienceUserIDs = await GetWorkspaceAudienceUserIDs(request.WorkspaceID, ct);

            foreach (var audienceUserID in audienceUserIDs)
            {
                await _workspaceHub.Clients.Group(audienceUserID).SendAsync("WorkspaceMemberAdded", new
                {
                    WorkspaceID = request.WorkspaceID,
                    UserID = user.ID,
                    Member = response
                }, ct);
            }

            return Ok(response);
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //
        [HttpDelete]
        [ActionName("deleteWorkspace")]
        public async Task<ActionResult> DeleteWorkspace(string workspaceID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("workspaceID is required");
            }

            // Get user from Claim/Token
            var userID = GetCurrentUserID();

            var workspace = await _db.Workspaces
                .Where(x => x.ID == workspaceID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (workspace == null)
            {
                return NotFound();
            }

            if (workspace.OwnerID != userID)
            {
                return Forbid();
            }

            // Capture audience before soft-delete logic changes visibility
            var audienceUserIDs = await GetWorkspaceAudienceUserIDs(workspaceID, ct);

            var now = DateTime.UtcNow.ToString("o");
            workspace.DeletedAt = now;

            await _db.SaveChangesAsync(ct);

            // Signal
            foreach (var audienceUserID in audienceUserIDs)
            {
                await _workspaceHub.Clients.Group(audienceUserID).SendAsync("WorkspaceDeleted", new
                {
                    WorkspaceID = workspaceID
                }, ct);
            }

            return Ok(new
            {
                success = true,
                workspaceID = workspaceID
            });
        }

        [HttpDelete]
        [ActionName("removeWorkspaceMember")]
        public async Task<ActionResult> RemoveWorkspaceMember([FromQuery] string workspaceID, [FromQuery] string userID, CancellationToken ct)
        {
            // Check if params are given
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("WorkspaceID is required.");
            }
            if (string.IsNullOrWhiteSpace(userID))
            {
                return BadRequest("UserID is required.");
            }

            // Get current user from JWT
            var currentUserID = GetCurrentUserID();

            // Get workspace with id and check if available/found
            var workspace = await _db.Workspaces
                .FirstOrDefaultAsync(x => x.ID == workspaceID && x.DeletedAt == null, ct);
            if (workspace == null)
            {
                return NotFound("Workspace not found.");
            }

            // Get role of user in given workspace
            var currentUserRole = await _db.WorkspaceMembers
                .Where(x => x.WorkspaceID == workspaceID && x.UserID == currentUserID && x.DeletedAt == null)
                .Select(x => x.Role)
                .FirstOrDefaultAsync(ct);

            // Check if user is alowwed to remove members. (1 = owner / 2 = Admin / 3 = User)
            if (currentUserRole != 1)
            {
                return Forbid();
            }

            // Additionally check if user to delete is the owner of the group. (Should never happen, but just in case we lock possible bugs)
            if (workspace.OwnerID == userID)
            {
                return BadRequest("Owner cannot be removed.");
            }

            // Get membership of the user that should be removed
            var member = await _db.WorkspaceMembers
                .FirstOrDefaultAsync(x => x.WorkspaceID == workspaceID && x.UserID == userID && x.DeletedAt == null, ct);

            // Check if membership was found
            if (member == null)
            {
                return NotFound("Member not found.");
            }

            // Capture audience before member is removed
            var audienceUserIDs = await GetWorkspaceAudienceUserIDs(workspaceID, ct);

            // DELETE membership of user
            member.DeletedAt = DateTime.UtcNow.ToString("o");
            await _db.SaveChangesAsync(ct);

            // Signal
            foreach (var audienceUserID in audienceUserIDs)
            {
                await _workspaceHub.Clients.Group(audienceUserID).SendAsync("WorkspaceMemberRemoved", new
                {
                    WorkspaceID = workspaceID,
                    UserID = userID
                }, ct);
            }

            return Ok();
        }




        // ------------ //
        // --- SAVE --- //
        // ------------ //
        [HttpPatch]
        [ActionName("saveWorkspace")]
        public async Task<ActionResult<WorkspaceResponse>> SaveWorkspace([FromBody] SaveWorkspaceRequest request, CancellationToken ct)
        {
            // ::: Prepare Steps ::: //
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required.");
            }

            var userID = GetCurrentUserID();

            var workspace = await _db.Workspaces
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (workspace == null)
            {
                return NotFound("Workspace not found.");
            }

            if (workspace.OwnerID != userID)
            {
                return Forbid();
            }


            // ::: Patch Progress ::: //
            var changed = false;

            if (request.Name != null)
            {
                var trimmedName = request.Name.Trim();

                if (string.IsNullOrWhiteSpace(trimmedName))
                {
                    return BadRequest("Name must not be empty.");
                }

                if (workspace.Name != trimmedName)
                {
                    workspace.Name = trimmedName;
                    changed = true;
                }
            }


            // Save to Database if anything got changed
            if (changed)
            {
                workspace.UpdatedAt = DateTime.UtcNow.ToString("o");
                await _db.SaveChangesAsync(ct);
            }


            // ::: Build Response ::: //
            var result = new WorkspaceResponse
            {
                ID = workspace.ID,
                OwnerID = workspace.OwnerID,
                Name = workspace.Name,
                CreatedAt = workspace.CreatedAt,
                UpdatedAt = workspace.UpdatedAt,
            };

            // Signal
            if (changed)
            {
                var audienceUserIDs = await GetWorkspaceAudienceUserIDs(workspace.ID, ct);

                foreach (var audienceUserID in audienceUserIDs)
                {
                    await _workspaceHub.Clients.Group(audienceUserID).SendAsync("WorkspaceUpdated", result, ct);
                }
            }

            return Ok(result);
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        [HttpGet]
        [ActionName("getWorkspaceWithID")]
        public async Task<IActionResult> GetWorkspaceWithID([FromQuery] string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Workspace ID is required.");
            }

            var workspace = await _db.Workspaces
                .Where(x => x.ID == id && x.DeletedAt == null)
                .Select(x => new WorkspaceResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .FirstOrDefaultAsync(ct);

            if (workspace == null)
            {
                return NotFound("Workspace could not be found.");
            }

            return Ok(workspace);
        }


        [HttpGet]
        [ActionName("GetWorkspacesOfUser")]
        public async Task<ActionResult<List<WorkspaceResponse>>> GetWorkspacesOfUser(CancellationToken ct)
        {
            var userID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(userID))
            {
                return BadRequest("Couldnt fetch UserID. Proccess aborted.");
            }

            var workspaces = await _db.Workspaces
                .Where(x =>
                    x.DeletedAt == null &&
                    (
                        x.OwnerID == userID ||
                        _db.WorkspaceMembers.Any(m =>
                            m.WorkspaceID == x.ID &&
                            m.UserID == userID &&
                            m.DeletedAt == null
                        )
                    )
                )
                .OrderBy(x => x.Name)
                .Select(x => new WorkspaceResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return Ok(workspaces);
        }


        [HttpGet]
        [ActionName("getWorkspaceMembers")]
        public async Task<IActionResult> GetWorkspaceMembers([FromQuery] string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Workspace ID is required.");
            }

            var exists = await _db.Workspaces
                .AnyAsync(x => x.ID == id && x.DeletedAt == null, ct);

            if (!exists)
            {
                return NotFound("Workspace could not be found.");
            }

            var members = await _db.WorkspaceMembers
                .Where(x => x.WorkspaceID == id && x.DeletedAt == null)
                .Select(x => new WorkspaceMemberResponse
                {
                    UserID = x.User.ID,
                    AvatarUrl = x.User.AvatarUrl,
                    DisplayName = x.User.DisplayName ?? string.Empty,
                    Username = x.User.Username,
                    Email = x.User.Email,
                    Role = x.Role,
                })
                .ToListAsync(ct);

            return Ok(members);
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

        // Resolve all current workspace audience users for live updates
        private async Task<List<string>> GetWorkspaceAudienceUserIDs(string workspaceID, CancellationToken ct)
        {
            return await _db.WorkspaceMembers
                .Where(x => x.WorkspaceID == workspaceID && x.DeletedAt == null)
                .Select(x => x.UserID)
                .Distinct()
                .ToListAsync(ct);
        }
    }
}