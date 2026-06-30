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
    /// Manages note folders, both personal (no workspace) and workspace-shared, including
    /// creation, editing, moving and recursive deletion. All endpoints require an
    /// authenticated user. Changes to workspace folders are pushed live to the workspace
    /// group over the <see cref="Hubs.NoteHub"/>.
    /// </summary>
    /// <remarks>
    /// Permission rule throughout: a personal folder may only be modified by its owner, while
    /// a workspace folder may be modified by any active member of that workspace
    /// (see <c>CanModify</c>).
    /// </remarks>
    [ApiController]
    [Route("folders/[action]")]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly ILogger<FoldersController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<NoteHub> _noteHub;


        /// <summary>
        /// Creates the controller with its injected logger, database context and the note hub
        /// used for live notifications.
        /// </summary>
        public FoldersController(ILogger<FoldersController> logger, LumifyDbContext db, IHubContext<NoteHub> noteHub)
        {
            _logger = logger;
            _db = db;
            _noteHub = noteHub;
        }


        // ----------- //
        // --- ADD --- //
        // ----------- //
        /// <summary>
        /// Creates a new folder, optionally inside a workspace and/or a parent folder.
        /// </summary>
        /// <remarks>
        /// If a workspace is given, the user must be a member of it; if a parent folder is
        /// given, it must live in the same space. On success in a workspace, a
        /// <c>FolderCreated</c> event is broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">Folder data (name required; optional description, workspace, parent).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created folder; 400 on missing name or invalid workspace/parent;
        /// 403 if the user is not a member of the target workspace or owner of the parent.</returns>
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
        /// <summary>
        /// Updates a folder's name, description and/or parent (move). Only the provided fields
        /// are changed; the database is only written if something actually changed.
        /// </summary>
        /// <remarks>
        /// Move rules: a folder cannot be moved into itself; a personal folder may only move
        /// into another personal folder of the same owner; a workspace folder may only move
        /// within the same workspace. On a successful change in a workspace, a
        /// <c>FolderUpdated</c> event is broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">The folder ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated folder; 400 on invalid input or move target; 403 if
        /// the user may not modify it; 404 if the folder does not exist.</returns>
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

            // Personal folder: only the owner may modify. Workspace folder: any workspace member may.
            if (!await CanModify(folder.WorkspaceID, folder.OwnerID, userID, ct))
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

        /// <summary>
        /// Soft-deletes a folder together with all of its descendant sub-folders and their
        /// notes (recursive, breadth-first).
        /// </summary>
        /// <remarks>
        /// Nothing is physically removed — all affected rows get a <c>DeletedAt</c> timestamp.
        /// On a workspace folder, a <c>FolderDeleted</c> event is broadcast to the workspace group.
        /// </remarks>
        /// <param name="folderID">The root folder to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the folder ID; 400 if the ID is missing;
        /// 403 if the user may not delete it; 404 if the folder does not exist.</returns>
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

            // Personal folder: only the owner may delete. Workspace folder: any workspace member may.
            if (!await CanModify(rootFolder.WorkspaceID, rootFolder.OwnerID, userID, ct))
            {
                _logger.LogInformation("DeleteFolder aborted: user {UserID} is not allowed to delete folder {FolderID}.", userID, rootFolder.ID);
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
        /// <summary>
        /// Returns all personal folders of the current user (no workspace), oldest first.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of personal folders.</returns>
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

        /// <summary>
        /// Returns all folders of the given workspace, oldest first.
        /// </summary>
        /// <param name="workspaceID">The workspace whose folders are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of workspace folders.</returns>
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
