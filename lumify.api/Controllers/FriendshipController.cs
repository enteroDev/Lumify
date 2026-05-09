using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.Enum;
using lumify.api.Interfaces;
using lumify.api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace lumify.api.Controllers
{
    [ApiController]
    [Route("friendship/[action]")]
    [Authorize]
    public class FriendshipController : ControllerBase
    {
        private readonly ILogger<FriendshipController> _logger;
        private readonly LumifyDbContext _db;
        private readonly FriendshipService _friendshipService;
        private readonly IPresenceService _presenceService;


        public FriendshipController(ILogger<FriendshipController> logger, LumifyDbContext db, FriendshipService friendshipService, IPresenceService presenceService)
        {
            _logger = logger;
            _db = db;
            _friendshipService = friendshipService;
            _presenceService = presenceService;
        }



        // ----------- //
        // --- ADD --- //
        // ----------- //
        [HttpPost]
        [ActionName("sendFriendRequest")]
        public async Task<ActionResult> SendFriendRequest(string addresseeID)
        {
            var requesterID = GetCurrentUserID();

            if (string.IsNullOrWhiteSpace(addresseeID))
            {
                return BadRequest("AddresseeID is required.");
            }

            try
            {
                await _friendshipService.SendFriendRequest(requesterID, addresseeID);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send friend request from {RequesterID} to {AddresseeID}", requesterID, addresseeID);
                return BadRequest(ex.Message);
            }
        }



        // ------------ //
        // --- SAVE --- //
        // ------------ //
        [HttpPatch]
        [ActionName("acceptFriendRequest")]
        public async Task<ActionResult> AcceptFriendRequest(string friendshipID)
        {
            var currentUserID = GetCurrentUserID();

            if (string.IsNullOrWhiteSpace(friendshipID))
            {
                return BadRequest("FriendshipID is required.");
            }

            try
            {
                await _friendshipService.AcceptFriendRequest(friendshipID, currentUserID);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to accept friend request {FriendshipID} for user {CurrentUserID}", friendshipID, currentUserID);
                return BadRequest(ex.Message);
            }
        }



        // -------------- //
        // --- DELETE --- //
        // -------------- //
        [HttpPatch]
        [ActionName("rejectFriendRequest")]
        public async Task<ActionResult> RejectFriendRequest(string friendshipID)
        {
            var currentUserID = GetCurrentUserID();

            if (string.IsNullOrWhiteSpace(friendshipID))
            {
                return BadRequest("FriendshipID is required.");
            }

            try
            {
                await _friendshipService.RejectFriendRequest(friendshipID, currentUserID);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reject friend request {FriendshipID} for user {CurrentUserID}", friendshipID, currentUserID);
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch]
        [ActionName("removeFriend")]
        public async Task<ActionResult> RemoveFriend(string friendID)
        {
            var currentUserID = GetCurrentUserID();

            if (string.IsNullOrWhiteSpace(friendID))
            {
                return BadRequest("FriendID is required.");
            }

            try
            {
                await _friendshipService.RemoveFriend(currentUserID, friendID);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove friend {FriendID} for user {CurrentUserID}", friendID, currentUserID);
                return BadRequest(ex.Message);
            }
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        [HttpGet]
        [ActionName("getIncomingFriendRequests")]
        public async Task<ActionResult<List<FriendshipResponse>>> GetIncomingFriendRequests(CancellationToken ct)
        {
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return Unauthorized();
            }

            var incomingRequests = await _db.Friendships
                .Where(x =>
                    x.AddresseeID == currentUserID &&
                    x.Status == (int)FriendshipStatus.Pending &&
                    x.DeletedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new FriendshipResponse
                {
                    ID = x.ID,
                    RequesterID = x.RequesterID,
                    AddresseeID = x.AddresseeID,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,

                    RequesterUsername = x.Requester.Username,
                    RequesterDisplayName = x.Requester.DisplayName,
                    RequesterAvatarUrl = x.Requester.AvatarUrl
                })
                .ToListAsync(ct);

            return Ok(incomingRequests);
        }

        [HttpGet]
        [ActionName("getOutgoingFriendRequests")]
        public async Task<ActionResult<List<FriendshipResponse>>> GetOutgoingFriendRequests(CancellationToken ct)
        {
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return Unauthorized();
            }

            var outgoingRequests = await _db.Friendships
                .Where(x =>
                    x.RequesterID == currentUserID &&
                    x.Status == (int)FriendshipStatus.Pending &&
                    x.DeletedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new FriendshipResponse
                {
                    ID = x.ID,
                    RequesterID = x.RequesterID,
                    AddresseeID = x.AddresseeID,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,

                    FriendUserID = x.Addressee.ID,
                    FriendUsername = x.Addressee.Username,
                    FriendDisplayName = x.Addressee.DisplayName,
                    FriendAvatarUrl = x.Addressee.AvatarUrl
                })
                .ToListAsync(ct);

            return Ok(outgoingRequests);
        }

        [HttpGet]
        [ActionName("getFriendsOfUser")]
        public async Task<ActionResult<List<UserPreviewResponse>>> GetFriendsOfUser(CancellationToken ct)
        {
            var currentUserID = GetCurrentUserID();
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                return Unauthorized();
            }

            var friends = await _db.Friendships
                .Where(x =>
                    (x.UserLowID == currentUserID || x.UserHighID == currentUserID) &&
                    x.Status == (int)FriendshipStatus.Accepted &&
                    x.DeletedAt == null)
                .Select(x => x.UserLowID == currentUserID ? x.UserHigh : x.UserLow)
                .Select(x => new UserPreviewResponse
                {
                    ID = x.ID,
                    Username = x.Username,
                    Email = x.Email,
                    DisplayName = x.DisplayName,
                    AvatarUrl = x.AvatarUrl
                })
                .ToListAsync(ct);

            foreach (var friend in friends)
            {
                friend.PresenceStatus = _presenceService.GetPresenceStatus(friend.ID);
            }

            return Ok(friends);
        }



        // -------------- //
        // --- HELPER --- //
        // -------------- //
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