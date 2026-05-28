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
    [Route("notes/[action]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly ILogger<NotesController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IHubContext<NoteHub> _noteHub;

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

            // Only the owner is allowed to add modules to the Note
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to add modules to the Note
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to update the Note
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to update the TextBlock
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to delete the Note
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to delete the TextBlock
            if (note.OwnerID != userID)
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

            // Only the owner is allowed to delete the LinkItem
            if (note.OwnerID != userID)
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

        [HttpGet]
        [ActionName("getAllNotesOfWorkspace")]
        public async Task<ActionResult<List<NoteResponse>>> GetAllNotesOfWorkspace(string workspaceID, CancellationToken ct)
        {
            // Query all Notes of the workspace, joined with the user table to include OwnerName and exclude deleted users, sorted by creation date
            var notes = await (
                from note in _db.Notes
                join user in _db.Users on note.OwnerID equals user.ID
                where note.WorkspaceID == workspaceID
                    && note.DeletedAt == null
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
                    && user.DeletedAt == null
                select new NoteResponse
                {
                    ID = n.ID,
                    OwnerID = n.OwnerID,
                    OwnerName = user.FirstName != null && user.LastName != null
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
