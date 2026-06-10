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
    [Route("folders/[action]")]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly ILogger<FoldersController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<NoteHub> _noteHub;


        public FoldersController(ILogger<FoldersController> logger, LumifyDbContext db, IHubContext<NoteHub> noteHub)
        {
            _logger = logger;
            _db = db;
            _noteHub = noteHub;
        }


        // ----------- //
        // --- ADD --- //
        // ----------- //
        [HttpPost]
        [ActionName("addFolder")]
        public async Task<ActionResult<FolderResponse>> AddFolder([FromBody] AddFolderRequest request, CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required");
            }

            // Prepare workspaceID and parentFolderID of request
            var workspaceID = string.IsNullOrWhiteSpace(request.WorkspaceID) ? null : request.WorkspaceID.Trim();
            var parentFolderID = string.IsNullOrWhiteSpace(request.ParentFolderID) ? null : request.ParentFolderID.Trim();

            // Handle optional workspaceID - Check if workspace exists and if the user is a member
            if (workspaceID != null)
            {
                var workspaceExists = await _db.Workspaces.AnyAsync(x => x.ID == workspaceID && x.DeletedAt == null, ct);
                if (!workspaceExists)
                {
                    return BadRequest("Workspace does not exist");
                }

                var isMember = await _db.WorkspaceMembers.AnyAsync(x => x.WorkspaceID == workspaceID && x.UserID == userID && x.DeletedAt == null, ct);
                if (!isMember)
                {
                    return Forbid();
                }
            }

            // Handle optional parentFolderID - Check if parent folder exists and belongs to the same space
            if (parentFolderID != null)
            {
                var parent = await _db.Folders
                    .Where(x => x.ID == parentFolderID && x.DeletedAt == null)
                    .Select(x => new { x.ID, x.WorkspaceID, x.OwnerID })
                    .FirstOrDefaultAsync(ct);

                if (parent == null)
                {
                    return BadRequest("ParentFolderID not found");
                }

                var parentWorkspaceID = string.IsNullOrWhiteSpace(parent.WorkspaceID) ? null : parent.WorkspaceID;

                if (parentWorkspaceID != workspaceID)
                {
                    return BadRequest("Parent folder is in a different space");
                }

                if (workspaceID == null && parent.OwnerID != userID)
                {
                    return Forbid();
                }
            }

            // Get current time via UTCNow, since it is the base time wherever the user currently is. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create Folder based on the request
            var folder = new Folder
            {
                ID = Guid.NewGuid().ToString(),
                OwnerID = userID,
                WorkspaceID = workspaceID,
                ParentFolderID = parentFolderID,
                Name = request.Name.Trim(),
                Description = request.Description,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new Folder to the EF-Context and save it into the database
            _db.Folders.Add(folder);
            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new FolderResponse
            {
                ID = folder.ID,
                OwnerID = folder.OwnerID,
                ParentFolderID = folder.ParentFolderID,
                WorkspaceID = folder.WorkspaceID,
                Name = folder.Name,
                Description = folder.Description,
                CreatedAt = folder.CreatedAt,
                UpdatedAt = folder.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(folder.WorkspaceID))
            {
                await _noteHub.Clients.Group(folder.WorkspaceID).SendAsync("FolderCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
        [HttpPatch]
        [ActionName("saveFolder")]
        public async Task<ActionResult<FolderResponse>> SaveFolder([FromBody] SaveFolderRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the Folder in the database
            var folder = await _db.Folders
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (folder == null)
            {
                return NotFound("Folder not found");
            }

            // Only the owner is allowed to update the Folder
            if (folder.OwnerID != userID)
            {
                return Forbid();
            }

            // Track whether any field actually changed to avoid unnecessary DB writes
            var changed = false;



            // ::: Apply ::: //

            // Folder.Name
            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("Name cannot be empty");
                }

                if (folder.Name != trimmed)
                {
                    folder.Name = trimmed;
                    changed = true;
                }
            }

            // Folder.Description
            if (request.Description != null)
            {
                var trimmed = request.Description.Trim();

                // Allow empty description (means clear description)
                if (folder.Description != trimmed)
                {
                    folder.Description = trimmed;
                    changed = true;
                }
            }

            // Folder.ParentFolderID
            if (request.ParentFolderID != null)
            {
                // Allow empty string. That means: "move folder to root"
                var targetFolderID = string.IsNullOrWhiteSpace(request.ParentFolderID) ? null : request.ParentFolderID.Trim();

                if (targetFolderID != null)
                {
                    // Prevent moving a folder into itself
                    if (targetFolderID == folder.ID)
                    {
                        return BadRequest("Folder cannot be moved into itself");
                    }

                    // Check if target folder exists and is not deleted
                    var targetFolder = await _db.Folders
                        .Where(x => x.ID == targetFolderID && x.DeletedAt == null)
                        .Select(x => new { x.ID, x.OwnerID, x.WorkspaceID })
                        .FirstOrDefaultAsync(ct);

                    if (targetFolder == null)
                    {
                        return BadRequest("ParentFolderID not found");
                    }

                    // Private folder may only be moved into own private folders
                    if (folder.WorkspaceID == null)
                    {
                        if (targetFolder.OwnerID != userID)
                        {
                            return Forbid();
                        }

                        if (!string.IsNullOrWhiteSpace(targetFolder.WorkspaceID))
                        {
                            return BadRequest("Folder is in a different space");
                        }
                    }
                    else
                    {
                        // Workspace folder may only be moved inside the same workspace
                        if (targetFolder.WorkspaceID != folder.WorkspaceID)
                        {
                            return BadRequest("Folder is in a different workspace");
                        }
                    }
                }

                if (folder.ParentFolderID != targetFolderID)
                {
                    folder.ParentFolderID = targetFolderID;
                    changed = true;
                }
            }



            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                folder.UpdatedAt = DateTime.UtcNow.ToString("o");

                await _db.SaveChangesAsync(ct);
            }



            // ::: Respond ::: //

            // Create result object
            var result = new FolderResponse
            {
                ID = folder.ID,
                OwnerID = folder.OwnerID,
                ParentFolderID = folder.ParentFolderID,
                WorkspaceID = folder.WorkspaceID,
                Name = folder.Name,
                Description = folder.Description,
                CreatedAt = folder.CreatedAt,
                UpdatedAt = folder.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (changed && !string.IsNullOrWhiteSpace(folder.WorkspaceID))
            {
                await _noteHub.Clients.Group(folder.WorkspaceID).SendAsync("FolderUpdated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //

        // Deletes not only the folder itself, but recursively iterates through sub-folders and their notes to soft-delete them as well.
        [HttpDelete]
        [ActionName("deleteFolder")]
        public async Task<ActionResult> DeleteFolder(string folderID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(folderID))
            {
                return BadRequest("folderID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            _logger.LogInformation("DeleteFolder started for {FolderID}...", folderID);

            // Find the root folder in the database
            var rootFolder = await _db.Folders
                .Where(x => x.ID == folderID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (rootFolder == null)
            {
                _logger.LogInformation("Root folder loaded: {Exists}.", rootFolder != null);
                return NotFound();
            }

            // Only the owner is allowed to delete the Folder
            if (rootFolder.OwnerID != userID)
            {
                _logger.LogInformation("DeleteFolder aborted: owner mismatch. Expected {ExpectedOwnerID}, actual {ActualOwnerID}.", userID, rootFolder.OwnerID);
                return Forbid();
            }

            _logger.LogInformation("Start collecting child folders...");

            // Collect all folder IDs to delete via BFS (breadth-first) to handle nested sub-folders
            var folderIDsToDelete = new List<string>();
            var pendingFolderIDs = new Queue<string>();
            pendingFolderIDs.Enqueue(rootFolder.ID);

            while (pendingFolderIDs.Count > 0)
            {
                var currentFolderID = pendingFolderIDs.Dequeue();
                folderIDsToDelete.Add(currentFolderID);

                var childFolderIDs = await _db.Folders
                    .Where(x => x.ParentFolderID == currentFolderID && x.DeletedAt == null)
                    .Select(x => x.ID)
                    .ToListAsync(ct);

                foreach (var childFolderID in childFolderIDs)
                {
                    pendingFolderIDs.Enqueue(childFolderID);
                }
            }

            _logger.LogInformation("Collected {Count} folders.", folderIDsToDelete.Count);

            var now = DateTime.UtcNow.ToString("o");

            // Soft-delete all notes inside the affected folders
            var notesToDelete = await _db.Notes
                .Where(x => x.FolderID != null && folderIDsToDelete.Contains(x.FolderID) && x.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var note in notesToDelete)
            {
                note.DeletedAt = now;
            }

            // Soft-delete all collected folders
            var foldersToDelete = await _db.Folders
                .Where(x => folderIDsToDelete.Contains(x.ID) && x.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var folder in foldersToDelete)
            {
                folder.DeletedAt = now;
            }

            _logger.LogInformation("Saving changes...");
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("DeleteFolder finished!");

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(rootFolder.WorkspaceID))
            {
                await _noteHub.Clients.Group(rootFolder.WorkspaceID).SendAsync("FolderDeleted", new
                {
                    folderID = rootFolder.ID
                }, ct);
            }

            // Return the deleted folderID as confirmation
            return Ok(new
            {
                success = true,
                folderID = folderID
            });
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        [HttpGet]
        [ActionName("getAllFoldersOfUser")]
        public async Task<ActionResult<List<FolderResponse>>> GetAllFoldersOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Query all personal Folders of the user (no workspace), sorted by creation date
            var folders = await _db.Folders
                .Where(x => x.OwnerID == userID && x.DeletedAt == null && x.WorkspaceID == null)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new FolderResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    ParentFolderID = x.ParentFolderID,
                    WorkspaceID = x.WorkspaceID,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .ToListAsync(ct);

            // Return the result list
            return Ok(folders);
        }

        [HttpGet]
        [ActionName("getAllFoldersOfWorkspace")]
        public async Task<ActionResult<List<FolderResponse>>> GetAllFoldersOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Query all Folders of the workspace, sorted by creation date
            var folders = await _db.Folders
                .Where(x => x.WorkspaceID == workspaceID && x.DeletedAt == null)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new FolderResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    ParentFolderID = x.ParentFolderID,
                    WorkspaceID = x.WorkspaceID,
                    Name = x.Name,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                })
                .ToListAsync(ct);

            // Return the result list
            return Ok(folders);
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
