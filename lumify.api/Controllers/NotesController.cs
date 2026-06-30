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
    /// Manages notes and their content modules — text blocks and link items — both personal
    /// (no workspace) and workspace-shared. Covers creating, editing, moving, ordering and
    /// deleting notes and modules, plus various queries. All endpoints require an authenticated
    /// user. Changes to workspace notes are pushed live to the workspace group over the
    /// <see cref="Hubs.NoteHub"/>.
    /// </summary>
    /// <remarks>
    /// A note is a container; its body is an ordered list of modules positioned via
    /// <c>NotePos</c>. Deleting a note cascades a soft-delete to all of its modules and
    /// attachments. Permission rule throughout: a personal note may only be modified by its
    /// owner, a workspace note by any active member (see <c>CanModify</c>).
    /// </remarks>
    [ApiController]
    [Route("notes/[action]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly ILogger<NotesController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<NoteHub> _noteHub;

        /// <summary>
        /// Creates the controller with its injected logger, database context and the note hub
        /// used for live notifications.
        /// </summary>
        public NotesController(ILogger<NotesController> logger, LumifyDbContext db, IHubContext<NoteHub> noteHub)
        {
            _logger = logger;
            _db = db;
            _noteHub = noteHub;
        }


        // ----------- //
        // --- ADD --- //
        // ----------- //

        // --- Note --- //
        /// <summary>
        /// Creates a new (empty) note, optionally inside a workspace and/or a folder.
        /// </summary>
        /// <remarks>
        /// If a workspace is given, the user must be a member; if a folder is given, it must
        /// live in the same space. On success in a workspace, a <c>NoteCreated</c> event is
        /// broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">Note data (name required; optional workspace and folder).</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created note; 400 on missing name or invalid workspace/folder;
        /// 403 if the user is not a member of the target workspace or owner of the folder.</returns>
        [HttpPost]
        [ActionName("addNote")]
        public async Task<ActionResult<NoteResponse>> AddNote([FromBody] AddNoteRequest request, CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required");
            }

            // Get workspaceID and folderID of request
            var workspaceID = string.IsNullOrWhiteSpace(request.WorkspaceID) ? null : request.WorkspaceID.Trim();
            var folderID = string.IsNullOrWhiteSpace(request.FolderID) ? null : request.FolderID.Trim();

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

            // Handle optional folderID - Check if folder exists and belongs to the same space
            if (folderID != null)
            {
                var parent = await _db.Folders
                    .Where(x => x.ID == folderID && x.DeletedAt == null)
                    .Select(x => new { x.ID, x.WorkspaceID, x.OwnerID })
                    .FirstOrDefaultAsync(ct);

                if (parent == null)
                {
                    return BadRequest("FolderID not found");
                }

                var parentWorkspaceID = string.IsNullOrWhiteSpace(parent.WorkspaceID) ? null : parent.WorkspaceID;

                if (parentWorkspaceID != workspaceID)
                {
                    return BadRequest("Folder is in a different space");
                }

                if (workspaceID == null && parent.OwnerID != userID)
                {
                    return Forbid();
                }
            }

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create Note based on the request
            var note = new Note
            {
                ID = Guid.NewGuid().ToString(),
                OwnerID = userID,
                WorkspaceID = workspaceID,
                FolderID = folderID,
                Name = request.Name.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new Note to the EF-Context and save it into the database
            _db.Notes.Add(note);
            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new NoteResponse
            {
                ID = note.ID,
                OwnerID = note.OwnerID,
                FolderID = note.FolderID,
                WorkspaceID = note.WorkspaceID,
                Name = note.Name,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("NoteCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }


        // --- NoteModules --- //
        /// <summary>
        /// Appends a text block module (text, heading, code, …) to a note, placed at the end
        /// (next free <c>NotePos</c>).
        /// </summary>
        /// <remarks>On success in a workspace, a <c>TextBlockCreated</c> event is broadcast to
        /// the workspace group.</remarks>
        /// <param name="request">Text block data including the parent note ID, type and content.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created text block; 400 if the note ID is missing; 403 if the
        /// user may not modify the note; 404 if the note does not exist.</returns>
        [HttpPost]
        [ActionName("addTextBlock")]
        public async Task<ActionResult<TextblockResponse>> AddTextBlock([FromBody] AddTextBlockRequest request, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.NoteID))
            {
                return BadRequest("NoteID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the parent Note in the database
            var note = await _db.Notes
                .Where(x => x.ID == request.NoteID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            // Personal note: only the owner may modify. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Determine the next position by taking the current max NotePos and incrementing it
            var maxNotePos = await _db.Note_TextBlocks
                .Where(x => x.NoteID == request.NoteID && x.DeletedAt == null)
                .MaxAsync(x => (int?)x.NotePos, ct) ?? -1;

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create TextBlock based on the request
            var textBlock = new Note_TextBlock
            {
                ID = Guid.NewGuid().ToString(),
                NoteID = request.NoteID,
                Type = request.Type,
                Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
                Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content,
                CodeLanguage = string.IsNullOrWhiteSpace(request.CodeLanguage) ? null : request.CodeLanguage.Trim(),
                IsCollapsed = 0,
                NotePos = maxNotePos + 1,
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new TextBlock to the EF-Context and save it into the database
            _db.Note_TextBlocks.Add(textBlock);
            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new TextblockResponse
            {
                ID = textBlock.ID,
                NoteID = textBlock.NoteID,
                Type = textBlock.Type,
                Name = textBlock.Name,
                Content = textBlock.Content,
                CodeLanguage = textBlock.CodeLanguage,
                IsCollapsed = textBlock.IsCollapsed == 1,
                NotePos = textBlock.NotePos,
                CreatedAt = textBlock.CreatedAt,
                UpdatedAt = textBlock.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("TextBlockCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }

        /// <summary>
        /// Appends a link item module (labelled URL) to a note, placed at the end (next free
        /// <c>NotePos</c>).
        /// </summary>
        /// <remarks>On success in a workspace, a <c>LinkItemCreated</c> event is broadcast to
        /// the workspace group.</remarks>
        /// <param name="request">Link item data including the parent note ID, URL and optional label.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the created link item; 400 if the note ID or URL is missing; 403 if
        /// the user may not modify the note; 404 if the note does not exist.</returns>
        [HttpPost]
        [ActionName("addLinkItem")]
        public async Task<ActionResult<LinkItemResponse>> AddLinkItem([FromBody] AddLinkItemRequest request, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.NoteID))
            {
                return BadRequest("NoteID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the parent Note in the database
            var note = await _db.Notes
                .Where(x => x.ID == request.NoteID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            // Personal note: only the owner may modify. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Determine the next position by taking the current max NotePos and incrementing it
            var maxNotePos = await _db.Note_LinkItems
                .Where(x => x.NoteID == request.NoteID && x.DeletedAt == null)
                .MaxAsync(x => (int?)x.NotePos, ct) ?? -1;

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest("Url is required");
            }

            // Get current time via UTCNow, since it is the base time wherever the user is currently. Otherwise it would differ if one user is from america and another is from china.
            var now = DateTime.UtcNow.ToString("o");

            // Create LinkItem based on the request
            var linkItem = new Note_LinkItem
            {
                ID = Guid.NewGuid().ToString(),
                NoteID = request.NoteID,
                NotePos = maxNotePos + 1,
                Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
                Url = request.Url.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            // Add new LinkItem to the EF-Context and save it into the database
            _db.Note_LinkItems.Add(linkItem);
            await _db.SaveChangesAsync(ct);

            // Create result object
            var result = new LinkItemResponse
            {
                ID = linkItem.ID,
                NoteID = linkItem.NoteID,
                NotePos = linkItem.NotePos,
                Label = linkItem.Label,
                Url = linkItem.Url,
                CreatedAt = linkItem.CreatedAt,
                UpdatedAt = linkItem.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("LinkItemCreated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //

        // --- Note --- //
        /// <summary>
        /// Updates a note's name and/or its folder (move). Only provided fields are changed, and
        /// the database is only written if something changed.
        /// </summary>
        /// <remarks>
        /// Move rules: a personal note may only move into another personal folder of the same
        /// owner; a workspace note may only move within the same workspace; an empty folder ID
        /// moves the note to the root. On a successful change in a workspace, a <c>NoteUpdated</c>
        /// event is broadcast to the workspace group.
        /// </remarks>
        /// <param name="request">The note ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated note; 400 on invalid input or move target; 403 if the
        /// user may not modify it; 404 if the note does not exist.</returns>
        [HttpPatch]
        [ActionName("saveNote")]
        public async Task<ActionResult<NoteResponse>> SaveNote([FromBody] SaveNoteRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the Note in the database
            var note = await _db.Notes
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            // Personal note: only the owner may modify. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Track whether any field actually changed to avoid unnecessary DB writes
            var changed = false;



            // ::: Apply ::: //

            // Note.Name
            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();

                if (trimmed.Length == 0)
                {
                    return BadRequest("Name cannot be empty");
                }

                if (note.Name != trimmed)
                {
                    note.Name = trimmed;
                    changed = true;
                }
            }

            // Note.FolderID
            if (request.FolderID != null)
            {
                // Allow empty string. That means: "move note to root"
                var targetFolderID = string.IsNullOrWhiteSpace(request.FolderID) ? null : request.FolderID.Trim();

                if (targetFolderID != null)
                {
                    // Check if target folder exists and is not deleted
                    var targetFolder = await _db.Folders
                        .Where(x => x.ID == targetFolderID && x.DeletedAt == null)
                        .Select(x => new { x.ID, x.OwnerID, x.WorkspaceID })
                        .FirstOrDefaultAsync(ct);

                    if (targetFolder == null)
                    {
                        return BadRequest("FolderID not found");
                    }

                    // Private note may only be moved into own private folders
                    if (note.WorkspaceID == null)
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
                        // Workspace note may only be moved inside the same workspace
                        if (targetFolder.WorkspaceID != note.WorkspaceID)
                        {
                            return BadRequest("Folder is in a different workspace");
                        }
                    }
                }

                if (note.FolderID != targetFolderID)
                {
                    note.FolderID = targetFolderID;
                    changed = true;
                }
            }




            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                note.UpdatedAt = DateTime.UtcNow.ToString("o");
                await _db.SaveChangesAsync(ct);
            }



            // ::: Respond ::: //

            // Create result object
            var result = new NoteResponse
            {
                ID = note.ID,
                OwnerID = note.OwnerID,
                WorkspaceID = note.WorkspaceID,
                FolderID = note.FolderID,
                Name = note.Name,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (changed && !string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("NoteUpdated", result, ct);
            }

            // Return the result
            return Ok(result);
        }


        // --- NoteModules --- //
        /// <summary>
        /// Updates a text block's type, name, content, code language, collapsed state and/or
        /// position. Only provided fields are changed, and the database is only written if
        /// something changed.
        /// </summary>
        /// <remarks>Changing <c>NotePos</c> reorders the block within the note. On a successful
        /// change in a workspace, a <c>TextBlockUpdated</c> event is broadcast to the workspace
        /// group; the parent note's timestamp is bumped too.</remarks>
        /// <param name="request">The text block ID plus the fields to change.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the updated text block; 400 on invalid input (e.g. negative
        /// position); 403 if the user may not modify the note; 404 if the block or its parent
        /// note does not exist.</returns>
        [HttpPatch]
        [ActionName("saveTextBlock")]
        public async Task<ActionResult<TextblockResponse>> SaveTextBlock([FromBody] SaveTextblockRequest request, CancellationToken ct)
        {
            // ::: Prepare ::: //

            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(request.ID))
            {
                return BadRequest("ID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TextBlock in the database
            var textBlock = await _db.Note_TextBlocks
                .FirstOrDefaultAsync(x => x.ID == request.ID && x.DeletedAt == null, ct);

            if (textBlock == null)
            {
                return NotFound("TextBlock not found");
            }

            // Find the parent Note to verify ownership and resolve the workspace for SignalR
            var note = await _db.Notes
                .FirstOrDefaultAsync(x => x.ID == textBlock.NoteID && x.DeletedAt == null, ct);

            if (note == null)
            {
                return NotFound("Parent note not found");
            }

            // Personal note: only the owner may modify. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Track whether any field actually changed to avoid unnecessary DB writes
            var changed = false;



            // ::: Apply ::: //

            // Note_TextBlock.Type
            if (request.Type.HasValue)
            {
                if (textBlock.Type != request.Type.Value)
                {
                    textBlock.Type = request.Type.Value;
                    changed = true;
                }
            }

            // Note_TextBlock.Name
            if (request.Name != null)
            {
                var trimmed = request.Name.Trim();
                var targetName = trimmed.Length == 0 ? null : trimmed;

                if (textBlock.Name != targetName)
                {
                    textBlock.Name = targetName;
                    changed = true;
                }
            }

            // Note_TextBlock.Content
            if (request.Content != null)
            {
                if (textBlock.Content != request.Content)
                {
                    textBlock.Content = request.Content;
                    changed = true;
                }
            }

            // Note_TextBlock.CodeLanguage
            if (request.CodeLanguage != null)
            {
                var trimmed = request.CodeLanguage.Trim();
                var targetCodeLanguage = trimmed.Length == 0 ? null : trimmed;

                if (textBlock.CodeLanguage != targetCodeLanguage)
                {
                    textBlock.CodeLanguage = targetCodeLanguage;
                    changed = true;
                }
            }

            // Note_TextBlock.IsCollapsed
            if (request.IsCollapsed.HasValue)
            {
                var targetIsCollapsed = request.IsCollapsed.Value ? 1 : 0;

                if (textBlock.IsCollapsed != targetIsCollapsed)
                {
                    textBlock.IsCollapsed = targetIsCollapsed;
                    changed = true;
                }
            }

            // Note_TextBlock.NotePos
            if (request.NotePos.HasValue)
            {
                if (request.NotePos.Value < 0)
                {
                    return BadRequest("NotePos cannot be negative");
                }

                if (textBlock.NotePos != request.NotePos.Value)
                {
                    textBlock.NotePos = request.NotePos.Value;
                    changed = true;
                }
            }




            // ::: Persist ::: //

            // Only persist and update the timestamp if something actually changed
            if (changed)
            {
                var now = DateTime.UtcNow.ToString("o");

                textBlock.UpdatedAt = now;
                note.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
            }



            // ::: Respond ::: //

            // Create result object
            var result = new TextblockResponse
            {
                ID = textBlock.ID,
                NoteID = textBlock.NoteID,
                Type = textBlock.Type,
                Name = textBlock.Name,
                Content = textBlock.Content,
                CodeLanguage = textBlock.CodeLanguage,
                IsCollapsed = textBlock.IsCollapsed == 1,
                NotePos = textBlock.NotePos,
                CreatedAt = textBlock.CreatedAt,
                UpdatedAt = textBlock.UpdatedAt
            };

            // Handle sharing via SignalR to the Hub
            if (changed && !string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("TextBlockUpdated", result, ct);
            }

            // Return the result
            return Ok(result);
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //

        // --- Note --- //
        /// <summary>
        /// Soft-deletes a note together with all of its modules (text blocks, link items) and
        /// attachments.
        /// </summary>
        /// <remarks>Everything is kept and marked with <c>DeletedAt</c>. On a workspace note, a
        /// <c>NoteDeleted</c> event is broadcast to the workspace group.</remarks>
        /// <param name="noteID">The note to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the note ID; 400 if the ID is missing;
        /// 403 if the user may not delete it; 404 if the note does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteNote")]
        public async Task<ActionResult> DeleteNote(string noteID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(noteID))
            {
                return BadRequest("noteID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the Note in the database
            var note = await _db.Notes
                .Where(x => x.ID == noteID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound();
            }

            // Personal note: only the owner may delete. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            var now = DateTime.UtcNow.ToString("o");

            // Soft-delete the Note and all its child modules
            note.DeletedAt = now;

            var textBlocks = await _db.Note_TextBlocks
                .Where(x => x.NoteID == noteID && x.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var textBlock in textBlocks)
            {
                textBlock.DeletedAt = now;
                textBlock.UpdatedAt = now;
            }

            var linkItems = await _db.Note_LinkItems
                .Where(x => x.NoteID == noteID && x.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var linkItem in linkItems)
            {
                linkItem.DeletedAt = now;
                linkItem.UpdatedAt = now;
            }

            var attachments = await _db.NoteAttachments
                .Where(x => x.NoteID == noteID && x.DeletedAt == null)
                .ToListAsync(ct);

            foreach (var attachment in attachments)
            {
                attachment.DeletedAt = now;
            }

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("NoteDeleted", new
                {
                    noteID = note.ID
                }, ct);
            }

            // Return the deleted noteID as confirmation
            return Ok(new
            {
                success = true,
                noteID = noteID
            });
        }

        // --- NoteModules --- //
        /// <summary>
        /// Soft-deletes a single text block from its note.
        /// </summary>
        /// <remarks>On a workspace note, a <c>TextBlockDeleted</c> event is broadcast to the
        /// workspace group; the parent note's timestamp is bumped.</remarks>
        /// <param name="textblockID">The text block to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the text block ID; 400 if the ID is
        /// missing; 403 if the user may not modify the note; 404 if the block or its parent note
        /// does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteTextBlock")]
        public async Task<ActionResult> DeleteTextBlock(string textblockID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(textblockID))
            {
                return BadRequest("textblockID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the TextBlock in the database
            var textBlock = await _db.Note_TextBlocks
                .Where(x => x.ID == textblockID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (textBlock == null)
            {
                return NotFound("TextBlock not found");
            }

            // Find the parent Note to verify ownership and resolve the workspace for SignalR
            var note = await _db.Notes
                .Where(x => x.ID == textBlock.NoteID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound("Parent note not found");
            }

            // Personal note: only the owner may delete. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Soft-delete the TextBlock by setting DeletedAt instead of removing the record
            var now = DateTime.UtcNow.ToString("o");

            textBlock.DeletedAt = now;
            textBlock.UpdatedAt = now;

            note.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("TextBlockDeleted", new
                {
                    textBlockID = textBlock.ID
                }, ct);
            }

            // Return the deleted textblockID as confirmation
            return Ok(new
            {
                success = true,
                textblockID = textblockID
            });
        }

        /// <summary>
        /// Soft-deletes a single link item from its note.
        /// </summary>
        /// <remarks>On a workspace note, a <c>LinkItemDeleted</c> event is broadcast to the
        /// workspace group; the parent note's timestamp is bumped.</remarks>
        /// <param name="linkItemID">The link item to delete.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with <c>success = true</c> and the link item ID; 400 if the ID is
        /// missing; 403 if the user may not modify the note; 404 if the link item or its parent
        /// note does not exist.</returns>
        [HttpDelete]
        [ActionName("deleteLinkItem")]
        public async Task<ActionResult> DeleteLinkItem(string linkItemID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(linkItemID))
            {
                return BadRequest("linkItemID is required");
            }

            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Find the LinkItem in the database
            var linkItem = await _db.Note_LinkItems
                .Where(x => x.ID == linkItemID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (linkItem == null)
            {
                return NotFound("LinkItem not found");
            }

            // Find the parent Note to verify ownership and resolve the workspace for SignalR
            var note = await _db.Notes
                .Where(x => x.ID == linkItem.NoteID && x.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound("Parent note not found");
            }

            // Personal note: only the owner may delete. Workspace note: any workspace member may.
            if (!await CanModify(note.WorkspaceID, note.OwnerID, userID, ct))
            {
                return Forbid();
            }

            // Soft-delete the LinkItem by setting DeletedAt instead of removing the record
            var now = DateTime.UtcNow.ToString("o");

            linkItem.DeletedAt = now;
            linkItem.UpdatedAt = now;

            note.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            // Notify workspace members via SignalR
            if (!string.IsNullOrWhiteSpace(note.WorkspaceID))
            {
                await _noteHub.Clients.Group(note.WorkspaceID).SendAsync("LinkItemDeleted", new
                {
                    linkItemID = linkItem.ID
                }, ct);
            }

            // Return the deleted linkItemID as confirmation
            return Ok(new
            {
                success = true,
                linkItemID = linkItemID
            });
        }



        // ----------- //
        // --- GET --- //
        // ----------- //

        // --- Note --- //
        /// <summary>
        /// Returns all personal notes (no workspace) of the current user, oldest first.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of notes.</returns>
        [HttpGet]
        [ActionName("getAllNotesOfUser")]
        public async Task<ActionResult<List<NoteResponse>>> GetAllNotesOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Query all personal Notes of the user, joined with the user table to include OwnerName and exclude deleted users, sorted by creation date
            var notes = await (
                from note in _db.Notes
                join user in _db.Users on note.OwnerID equals user.ID
                where note.OwnerID == userID
                    && note.DeletedAt == null
                    && note.WorkspaceID == null
                    && user.DeletedAt == null
                orderby note.CreatedAt
                select new NoteResponse
                {
                    ID = note.ID,
                    OwnerID = note.OwnerID,
                    OwnerName = user.FirstName != null && user.LastName != null
                        ? user.FirstName + " " + user.LastName
                        : user.FirstName ?? user.LastName ?? user.ID,
                    FolderID = note.FolderID,
                    WorkspaceID = note.WorkspaceID,
                    Name = note.Name,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(notes);
        }

        /// <summary>
        /// Returns all notes of a workspace, oldest first.
        /// </summary>
        /// <remarks>Notes whose creator was soft-deleted are kept (content belongs to the
        /// workspace) and surface the creator as "Gelöschter Benutzer".</remarks>
        /// <param name="workspaceID">The workspace whose notes are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of notes.</returns>
        [HttpGet]
        [ActionName("getAllNotesOfWorkspace")]
        public async Task<ActionResult<List<NoteResponse>>> GetAllNotesOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Query all Notes of the workspace, joined with the user table to include OwnerName, sorted by creation date.
            // We intentionally keep notes whose creator was soft-deleted (content belongs to the workspace) and
            // surface the creator as "Gelöschter Benutzer" in that case.
            var notes = await (
                from note in _db.Notes
                join user in _db.Users on note.OwnerID equals user.ID
                where note.WorkspaceID == workspaceID
                    && note.DeletedAt == null
                orderby note.CreatedAt
                select new NoteResponse
                {
                    ID = note.ID,
                    OwnerID = note.OwnerID,
                    OwnerName = user.DeletedAt != null
                        ? "Gelöschter Benutzer"
                        : user.FirstName != null && user.LastName != null
                        ? user.FirstName + " " + user.LastName
                        : user.FirstName ?? user.LastName ?? user.ID,
                    FolderID = note.FolderID,
                    WorkspaceID = note.WorkspaceID,
                    Name = note.Name,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                }
            ).ToListAsync(ct);

            // Return the result list
            return Ok(notes);
        }

        /// <summary>
        /// Returns a single note by its ID (metadata only; modules are fetched separately).
        /// </summary>
        /// <param name="noteID">The note to look up.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the note; 400 if the ID is missing; 404 if it does not exist.</returns>
        [HttpGet]
        [ActionName("getNoteWithID")]
        public async Task<ActionResult<NoteResponse>> GetNoteWithID(string noteID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(noteID))
            {
                return BadRequest("noteID is required");
            }

            // Query the Note by ID, joined with the user table to include OwnerName
            var note = await (
                from n in _db.Notes
                join user in _db.Users on n.OwnerID equals user.ID
                where n.ID == noteID
                    && n.DeletedAt == null
                select new NoteResponse
                {
                    ID = n.ID,
                    OwnerID = n.OwnerID,
                    OwnerName = user.DeletedAt != null
                        ? "Gelöschter Benutzer"
                        : user.FirstName != null && user.LastName != null
                        ? user.FirstName + " " + user.LastName
                        : user.FirstName ?? user.LastName ?? user.ID,
                    FolderID = n.FolderID,
                    WorkspaceID = n.WorkspaceID,
                    Name = n.Name,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                }
            ).FirstOrDefaultAsync(ct);

            if (note == null)
            {
                return NotFound("Note not found");
            }

            // Return the result
            return Ok(note);
        }

        /// <summary>
        /// Returns the number of personal notes (no workspace) owned by the current user.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the note count.</returns>
        [HttpGet]
        [ActionName("getNoteCountOfUser")]
        public async Task<ActionResult<int>> GetNoteCountOfUser(CancellationToken ct)
        {
            // Get userID from JWT/Cookie
            var userID = GetCurrentUserID();

            // Count only personal Notes (WorkspaceID == null) of the user
            var noteCount = await _db.Notes.CountAsync(x =>
                x.OwnerID == userID &&
                x.WorkspaceID == null &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(noteCount);
        }

        /// <summary>
        /// Returns the number of notes in a workspace. The user must be a member.
        /// </summary>
        /// <param name="workspaceID">The workspace to count notes for.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the note count; 400 if the ID is missing; 403 if the user is not a
        /// member; 404 if the workspace does not exist.</returns>
        [HttpGet]
        [ActionName("getNoteCountOfWorkspace")]
        public async Task<ActionResult<int>> GetNoteCountOfWorkspace(string workspaceID, CancellationToken ct)
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

            var noteCount = await _db.Notes.CountAsync(x =>
                x.WorkspaceID == trimmedWorkspaceID &&
                x.DeletedAt == null,
                ct
            );

            // Return the count
            return Ok(noteCount);
        }


        // --- NoteModules --- //
        /// <summary>
        /// Returns all text blocks of a note, in display order (<c>NotePos</c>).
        /// </summary>
        /// <param name="noteID">The note whose text blocks are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the ordered text blocks; 400 if the ID is missing; 404 if the note
        /// does not exist.</returns>
        [HttpGet]
        [ActionName("getTextBlocksOfNote")]
        public async Task<ActionResult<List<TextblockResponse>>> GetTextBlocksOfNote(string noteID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(noteID))
            {
                return BadRequest("noteID is required");
            }

            // Check if the Note exists
            var noteExists = await _db.Notes.AnyAsync(x => x.ID == noteID && x.DeletedAt == null, ct);

            if (!noteExists)
            {
                return NotFound("Note not found");
            }

            // Query all TextBlocks of the Note, sorted by NotePos
            var textBlocks = await _db.Note_TextBlocks
                .Where(x => x.NoteID == noteID && x.DeletedAt == null)
                .OrderBy(x => x.NotePos)
                .Select(x => new TextblockResponse
                {
                    ID = x.ID,
                    NoteID = x.NoteID,
                    Type = x.Type,
                    Name = x.Name,
                    Content = x.Content,
                    CodeLanguage = x.CodeLanguage,
                    IsCollapsed = x.IsCollapsed == 1,
                    NotePos = x.NotePos,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            // Return the result list
            return Ok(textBlocks);
        }

        /// <summary>
        /// Returns all link items of a note, in display order (<c>NotePos</c>).
        /// </summary>
        /// <param name="noteID">The note whose link items are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the ordered link items; 400 if the ID is missing; 404 if the note
        /// does not exist.</returns>
        [HttpGet]
        [ActionName("getLinkItemsOfNote")]
        public async Task<ActionResult<List<LinkItemResponse>>> GetLinkItemsOfNote(string noteID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(noteID))
            {
                return BadRequest("noteID is required");
            }

            // Check if the Note exists
            var noteExists = await _db.Notes.AnyAsync(x => x.ID == noteID && x.DeletedAt == null, ct);

            if (!noteExists)
            {
                return NotFound("Note not found");
            }

            // Query all LinkItems of the Note, sorted by NotePos
            var linkItems = await _db.Note_LinkItems
                .Where(x => x.NoteID == noteID && x.DeletedAt == null)
                .OrderBy(x => x.NotePos)
                .Select(x => new LinkItemResponse
                {
                    ID = x.ID,
                    NoteID = x.NoteID,
                    Label = x.Label,
                    Url = x.Url,
                    NotePos = x.NotePos,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            // Return the result list
            return Ok(linkItems);
        }

        /// <summary>
        /// Returns the workspace that the given note belongs to, if any.
        /// </summary>
        /// <param name="noteID">The note whose workspace is requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the workspace; 400 if the ID is missing; 404 if the note has no
        /// linked workspace or it was deleted.</returns>
        [HttpGet]
        [ActionName("getSpaceInfosOfNote")]
        public async Task<ActionResult<WorkspaceResponse>> getSpaceInfosOfNote(string noteID, CancellationToken ct)
        {
            // Check all neccessary params if available
            if (string.IsNullOrWhiteSpace(noteID))
            {
                return BadRequest("noteID is required");
            }

            // Query the workspace that is linked to the given Note via a join
            var result = await (
                from n in _db.Notes
                join ws in _db.Workspaces on n.WorkspaceID equals ws.ID
                where n.ID == noteID
                    && n.DeletedAt == null
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

            // Return 404 if the Note has no linked workspace or workspace was deleted
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
