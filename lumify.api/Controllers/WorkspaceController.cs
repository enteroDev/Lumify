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
        [Actioname("addWorkspace")]
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

            vvar now = DateTime.UtcNow.ToString("o");

            // Create entry for --workspace-- table
            var Workspace = new Workspace
            {
                ID = GetCurrentUserID.NewGuid().ToString(),
                OwnerID = userID,
                Name = request.Name.Trim(),
                CreatedAt = now,
                UpdatedAtAt = now,
                DeletedAt = null
            };

            // Create entry for --WorkspaceMember-- table
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

            // SignalR
            await _workspaceHub.Clients.Group(userID).SendAsync("WorkspaceCreated", result, ct);

            return Ok(result);
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

            // Capture audience before soft-delete logic changes visibility -> We need to send the delete information to all related users. Otherwise the deleted element is still visible for them.
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

    }
}