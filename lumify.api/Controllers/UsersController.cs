using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace lumify.api.Controllers
{
    [ApiController]
    [Route("users/[action]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly LumifyDbContext _db;
        private readonly IPresenceService _presenceService;

        public UsersController(ILogger<UsersController> logger, LumifyDbContext db, IPresenceService presenceService)
        {
            _logger = logger;
            _db = db;
            _presenceService = presenceService;
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
        [HttpPatch]
        [ActionName("saveUserProfile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponse>> SaveUserProfile([FromBody] SaveUserProfileRequest request, CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest("Request is required");
            }

            var userID = GetCurrentUserID();

            if (string.IsNullOrWhiteSpace(userID))
            {
                return Unauthorized("User ID claim missing");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.ID == userID && x.DeletedAt == null, ct);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Only update provided fields
            if (request.DisplayName != null)
            {
                user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                    ? null
                    : request.DisplayName.Trim();
            }

            if (request.AvatarUrl != null)
            {
                user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl)
                    ? null
                    : request.AvatarUrl.Trim();
            }

            if (request.Bio != null)
            {
                user.Bio = string.IsNullOrWhiteSpace(request.Bio)
                    ? null
                    : request.Bio.Trim();
            }

            user.UpdatedAt = DateTime.UtcNow.ToString("o");

            await _db.SaveChangesAsync(ct);

            var response = new UserProfileResponse
            {
                ID = user.ID,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(response);
        }

        [HttpPost]
        [ActionName("saveUserAvatar")]
        [Authorize]
        [RequestSizeLimit(10_000_000)] // 10 MB
        public async Task<ActionResult<string>> SaveUserAvatar(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            // Only allow supported image types
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif"
            };

            if (!allowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest("Only image files are allowed");
            }

            var userID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(userID))
            {
                return Unauthorized("User ID claim missing");
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.ID == userID && x.DeletedAt == null, ct);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Ensure avatar folder exists
            var avatarFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data", "avatars");
            if (!Directory.Exists(avatarFolder)) Directory.CreateDirectory(avatarFolder);

            Console.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"AvatarFolder: {avatarFolder}");
            // Console.WriteLine($"FullFilePath: {fullFilePath}");
            Console.WriteLine($"Incoming FileName: {file.FileName}");
            Console.WriteLine($"Incoming ContentType: {file.ContentType}");
            Console.WriteLine($"Incoming Length: {file.Length}");

            // Create safe unique file name - Use username based filename for uniqueness: avatar_[username].png
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                return BadRequest("Invalid file extension");
            }

            var storedFileName = $"avatar_{user.Username}{fileExtension}";
            var fullFilePath = Path.Combine(avatarFolder, storedFileName);

            // Save file to disk
            await using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Store relative url in DB
            user.AvatarUrl = $"/Data/avatars/{storedFileName}";
            await _db.SaveChangesAsync(ct);

            return Ok(user.AvatarUrl);
        }



        // ----------- //
        // --- GET --- //
        // ----------- //

        // ------------
        // USER BY ID
        // ------------

        // Gets userProfile of specific user by ID
        [HttpGet]
        [ActionName("getUserProfileWithID")]
        public async Task<ActionResult<UserProfileResponse>> GetUserProfileWithID(string userID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                return BadRequest("userID is required");
            }

            var user = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new UserProfileResponse
                {
                    ID = x.ID,
                    AvatarUrl = x.AvatarUrl,
                    DisplayName = x.DisplayName,
                    Bio = x.Bio,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }

        // Gets accountInfo of specific user by ID
        [HttpGet]
        [ActionName("getUserAccountInfoWithID")]
        public async Task<ActionResult<UserAccountInfoResponse>> GetUserAccountInfoWithID(string userID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                return BadRequest("userID is required");
            }

            var user = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new UserAccountInfoResponse
                {
                    ID = x.ID,
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }

        // Gets userPreview of specific user by ID
        [HttpGet]
        [ActionName("getUserPreviewWithID")]
        public async Task<ActionResult<UserPreviewResponse>> GetUserPreviewWithID(string userID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                return BadRequest("userID is required");
            }

            var user = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new UserPreviewResponse
                {
                    ID = x.ID,
                    Username = x.Username,
                    Email = x.Email,
                    DisplayName = x.DisplayName,
                    AvatarUrl = x.AvatarUrl,
                })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                return NotFound("User not found");
            }

            user.PresenceStatus = _presenceService.GetPresenceStatus(user.ID);

            return Ok(user);
        }


        // -------------
        // CURRENT USER
        // -------------

        // Get userProfile of current User
        [HttpGet]
        [ActionName("getUserProfile")]
        public async Task<ActionResult<UserAccountInfoResponse>> GetUserProfile(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var userProfile = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new UserProfileResponse
                {
                    ID = x.ID,
                    AvatarUrl = x.AvatarUrl,
                    DisplayName = x.DisplayName,
                    Bio = x.Bio,
                })
                .FirstOrDefaultAsync(ct);

            if (userProfile == null)
            {
                return NotFound
                (
                @"Userprofile could not be found.
                Either no valid userID was given, or no search results were found in the database."
                );
            }

            return Ok(userProfile);
        }

        // Get accountInfos of current User
        [HttpGet]
        [ActionName("getUserAccountInfo")]
        public async Task<ActionResult<UserAccountInfoResponse>> GetUserAccountInfo(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var userAccountInfo = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new UserAccountInfoResponse
                {
                    ID = x.ID,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Email = x.Email,
                    Username = x.Username,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (userAccountInfo == null)
            {
                return NotFound
                (
                @"Accountinfo could not be found.
                Either no valid userID was given, or no search results were found in the database."
                );
            }

            return Ok(userAccountInfo);
        }


        // Get avatar of current user
        [HttpGet]
        [ActionName("getAvatarOfUser")]
        public async Task<ActionResult<string>> GetAvatarOfUser(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var user = await _db.Users
                .Where(x => x.ID == userID && x.DeletedAt == null)
                .Select(x => new { x.AvatarUrl })
                .FirstOrDefaultAsync(ct);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user.AvatarUrl);
        }



        // Get related users to current user
        [HttpGet]
        [ActionName("getRelatedUsers")]
        public async Task<IActionResult> GetRelatedUsers(CancellationToken ct)
        {
            // Get current user and verify a valid user exists
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return BadRequest("Current user could not be determined.");
            }

            // Get all workspace IDs where the current user is a member
            var workspaceIDs = await _db.WorkspaceMembers
                .Where(x => x.UserID == currentUserID && x.DeletedAt == null)
                .Select(x => x.WorkspaceID)
                .Distinct()
                .ToListAsync(ct);

            if (!workspaceIDs.Any())
            {
                return Ok(new List<UserPreviewResponse>());
            }

            // Get all related users from these workspaces, excluding the current user him/her-self (Whom is also a member of the ws)
            var relatedUsers = await _db.WorkspaceMembers
                .Where(x => workspaceIDs.Contains(x.WorkspaceID) && x.UserID != currentUserID && x.DeletedAt == null && x.User.DeletedAt == null)
                .Select(x => new UserPreviewResponse
                {
                    ID = x.User.ID,
                    AvatarUrl = x.User.AvatarUrl,
                    DisplayName = x.User.DisplayName ?? string.Empty,
                    Username = x.User.Username,
                    Email = x.User.Email,
                })
                .Distinct()
                .ToListAsync(ct);

            // Add presenceStatus for each user.
            foreach (var user in relatedUsers)
            {
                user.PresenceStatus = _presenceService.GetPresenceStatus(user.ID);
            }

            return Ok(relatedUsers);
        }


        // Get recent TODO-activities of current user
        [HttpGet]
        [ActionName("get5LastModifiedTodosOfUser")]
        public async Task<IActionResult> Get5LastModifiedTodosOfUser(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var todos = await _db.TodoLists
                .Where(x => x.OwnerID == userID && x.DeletedAt == null)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Take(5)
                .Select(x => new TodoListResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    WorkspaceID = x.WorkspaceID,
                    Name = x.Name,
                    Status = x.Status,
                    IsArchived = x.IsArchived,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return Ok(todos);
        }

        // Get recent Event-activities of current user
        [HttpGet]
        [ActionName("getLast5ModifiedEventsOfUser")]
        public async Task<IActionResult> GetLast5ModifiedEventsOfUser(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var events = await _db.Events
                .Where(x => x.OwnerID == userID && x.DeletedAt == null)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Take(5)
                .Select(x => new EventResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    WorkspaceID = x.WorkspaceID,

                    Name = x.Name,
                    Description = x.Description,
                    Status = x.Status,

                    IsAllDay = x.IsAllDay == 1,

                    StartTime = x.StartDate,
                    EndTime = x.EndDate,

                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return Ok(events);
        }

        // Get recent Note-activities of current user
        [HttpGet]
        [ActionName("getLast5ModifiedNotesOfUser")]
        public async Task<IActionResult> GetLast5ModifiedNotesOfUser(CancellationToken ct)
        {
            var userID = GetCurrentUserID();

            var notes = await _db.Notes
                .Where(x => x.OwnerID == userID && x.DeletedAt == null)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Take(5)
                .Select(x => new NoteResponse
                {
                    ID = x.ID,
                    OwnerID = x.OwnerID,
                    WorkspaceID = x.WorkspaceID,
                    FolderID = x.FolderID,
                    Name = x.Name,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return Ok(notes);
        }



        // -------------- //
        // --- SEARCH --- //
        // -------------- //

        // General search of user in database
        [HttpGet]
        [ActionName("searchUsers")]
        [Authorize]
        public async Task<IActionResult> SearchUsers([FromQuery] string query, CancellationToken ct)
        {
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return BadRequest("Current user could not be determined.");
            }

            query = query?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<UserPreviewResponse>());
            }

            var loweredQuery = query.ToLower();

            var users = await _db.Users
                .Where(x =>
                    x.DeletedAt == null &&
                    x.ID != currentUserID &&
                    (
                        (x.DisplayName != null && x.DisplayName.ToLower().Contains(loweredQuery)) ||
                        x.Username.ToLower().Contains(loweredQuery) ||
                        x.Email.ToLower().Contains(loweredQuery)
                    )
                )
                .OrderBy(x => x.DisplayName ?? x.Username)
                .Take(20)
                .Select(x => new UserPreviewResponse
                {
                    ID = x.ID,
                    AvatarUrl = x.AvatarUrl,
                    DisplayName = x.DisplayName ?? string.Empty,
                    Username = x.Username,
                    Email = x.Email,
                })
                .ToListAsync(ct);

            // Add presenceStatus for each user.
            foreach (var user in users)
            {
                user.PresenceStatus = _presenceService.GetPresenceStatus(user.ID);
            }

            return Ok(users);
        }

        // Search users that can join the given workspace
        [HttpGet]
        [ActionName("searchAvailableUsersForWorkspace")]
        [Authorize]
        public async Task<IActionResult> SearchAvailableUsersForWorkspace([FromQuery] string workspaceID, [FromQuery] string query, CancellationToken ct)
        {
            // Get current user and verify a valid user exists
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return BadRequest("Current user could not be determined.");
            }

            // Check if needed data is given and/or valid
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return BadRequest("WorkspaceID is required.");
            }

            // Trimm searchQuery and check if valid query is given.
            query = query?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<RelatedUserResponse>());
            }

            // Check if workspace for given ID exists
            var workspaceExists = await _db.Workspaces
                .AnyAsync(x => x.ID == workspaceID && x.DeletedAt == null, ct);
            if (!workspaceExists)
            {
                return NotFound("Workspace could not be found.");
            }

            // Get userID's of workspaceMembers
            var workspaceMemberIDs = await _db.WorkspaceMembers
                .Where(x => x.WorkspaceID == workspaceID && x.DeletedAt == null)
                .Select(x => x.UserID)
                .ToListAsync(ct);

            var loweredQuery = query.ToLower();

            // Get all relevant users that can be added to the workspace. (Filter currentUser and users that are already workspaceMembers)
            var users = await _db.Users
                .Where(x =>
                    x.DeletedAt == null &&
                    x.ID != currentUserID &&
                    !workspaceMemberIDs.Contains(x.ID) &&
                    (
                        (x.DisplayName != null && x.DisplayName.ToLower().Contains(loweredQuery)) ||
                        x.Username.ToLower().Contains(loweredQuery) ||
                        x.Email.ToLower().Contains(loweredQuery)
                    )
                )
                .OrderBy(x => x.DisplayName ?? x.Username)
                .Take(20)
                .Select(x => new RelatedUserResponse
                {
                    UserID = x.ID,
                    AvatarUrl = x.AvatarUrl,
                    DisplayName = x.DisplayName ?? string.Empty,
                    Username = x.Username,
                    Email = x.Email,
                })
                .ToListAsync(ct);

            return Ok(users);
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

