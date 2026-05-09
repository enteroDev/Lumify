using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
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

        public UsersController(ILogger<UsersController> logger, LumifyDbContext db, IPresenceService presenceService)
        {
            _logger = logger;
            _db = db;
        }




        // ------------ //
        // --- SAVE --- //
        // ------------ //
        [HttpPatch]
        [ActionName("saveUserProfile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponse>> SaveUserProfile([FromBody] SaveUserProfileRequest request, CancellationToken ct)
        {



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

        // ----------------------
        // USERINFOS: USER BY ID 
        // ----------------------

        // Gets userProfile of specific user by ID
        [HttpGet]
        [ActionName("getUserProfileWithID")]
        public async Task<ActionResult<UserProfileResponse>> GetUserProfileWithID(string userID, CancellationToken ct)
        {


        }

        // Gets userPreview of specific user by ID
        [HttpGet]
        [ActionName("getUserPreviewWithID")]
        public async Task<ActionResult<UserPreviewResponse>> GetUserPreviewWithID(string userID, CancellationToken ct)
        {


        }


        // ------------------------
        // USERINFOS: CURRENT USER
        // ------------------------

        // Get userProfile of current User
        [HttpGet]
        [ActionName("getUserProfile")]
        public async Task<ActionResult<UserAccountInfoResponse>> GetUserProfile(CancellationToken ct)
        {


        }

        // Get accountInfos of current User
        [HttpGet]
        [ActionName("getUserAccountInfo")]
        public async Task<ActionResult<UserAccountInfoResponse>> GetUserAccountInfo(CancellationToken ct)
        {


        }


        // ----------------------
        // AVATAR: CURRENT USER
        // ----------------------

        // Get avatar of User
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


        // ---------------------------
        // RELATED USERS: CURRENT USER
        // ---------------------------
        [HttpGet]
        [ActionName("getRelatedUsers")]
        public async Task<IActionResult> GetRelatedUsers(CancellationToken ct)
        {


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


        }

        // Search users that can join the given workspace - Users that are not already in the given workspace
        [HttpGet]
        [ActionName("searchAvailableUsersForWorkspace")]
        [Authorize]
        public async Task<IActionResult> SearchAvailableUsersForWorkspace([FromQuery] string workspaceID, [FromQuery] string query, CancellationToken ct)
        {


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

