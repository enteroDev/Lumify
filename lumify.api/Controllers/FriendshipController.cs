using lumify.api.Models.Context;
using lumify.api.Models.DTO.Requests;
using lumify.api.Models.DTO.Responses;
using lumify.api.Models.Enum;
using lumify.api.Interfaces;
using lumify.api.Services;
using lumify.api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace lumify.api.Controllers
{
    /// <summary>
    /// Manages friendships: sending, accepting and rejecting friend requests, removing
    /// friends, and listing incoming/outgoing requests and current friends. All endpoints
    /// require an authenticated user. Changes are pushed live to the affected user over the
    /// <see cref="Hubs.ChatHub"/> so clients update without a reload.
    /// </summary>
    [ApiController]
    [Route("friendship/[action]")]
    [Authorize]
    public class FriendshipController : ControllerBase
    {
        private readonly ILogger<FriendshipController> _logger;
        private readonly LumifyDbContext _db;
        private readonly FriendshipService _friendshipService;
        private readonly IPresenceService _presenceService;
        private readonly IHubContext<ChatHub> _chatHub;


        /// <summary>
        /// Creates the controller with its injected dependencies (logging, database context,
        /// friendship service, presence service and the chat hub used for live notifications).
        /// </summary>
        public FriendshipController(ILogger<FriendshipController> logger, LumifyDbContext db, FriendshipService friendshipService, IPresenceService presenceService, IHubContext<ChatHub> chatHub)
        {
            _logger = logger;
            _db = db;
            _friendshipService = friendshipService;
            _presenceService = presenceService;
            _chatHub = chatHub;
        }



        // ----------- //
        // --- ADD --- //
        // ----------- //
        /// <summary>
        /// Sends a friend request from the current user to another user.
        /// </summary>
        /// <param name="addresseeID">The user who should receive the request.</param>
        /// <returns>200 with <c>Success = true</c>; 400 if the ID is missing or the request
        /// is rejected by the friendship service (e.g. already friends or pending).</returns>
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

                // Live-notify the addressee so the incoming request appears without a reload.
                await NotifyFriendshipChanged(addresseeID);

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
        /// <summary>
        /// Accepts a pending friend request addressed to the current user.
        /// </summary>
        /// <param name="friendshipID">The pending friendship to accept.</param>
        /// <returns>200 with <c>Success = true</c>; 400 if the ID is missing or the request
        /// cannot be accepted (e.g. not addressed to the current user).</returns>
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
                var requesterID = await _friendshipService.AcceptFriendRequest(friendshipID, currentUserID);

                // Live-notify the requester so their friend list updates without a reload.
                await NotifyFriendshipChanged(requesterID);

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
        /// <summary>
        /// Rejects a pending friend request addressed to the current user.
        /// </summary>
        /// <param name="friendshipID">The pending friendship to reject.</param>
        /// <returns>200 with <c>Success = true</c>; 400 if the ID is missing or the request
        /// cannot be rejected.</returns>
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
                var requesterID = await _friendshipService.RejectFriendRequest(friendshipID, currentUserID);

                // Live-notify the requester so their outgoing request disappears without a reload.
                await NotifyFriendshipChanged(requesterID);

                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reject friend request {FriendshipID} for user {CurrentUserID}", friendshipID, currentUserID);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Removes an existing friendship between the current user and another user.
        /// </summary>
        /// <param name="friendID">The friend to remove.</param>
        /// <returns>200 with <c>Success = true</c>; 400 if the ID is missing or the friendship
        /// cannot be removed.</returns>
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

                // Live-notify the removed friend so their friend list updates without a reload.
                await NotifyFriendshipChanged(friendID);

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
        /// <summary>
        /// Returns the pending friend requests addressed to the current user, newest first,
        /// including the requester's username, display name and avatar.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of incoming requests; 401 if no user is logged in.</returns>
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

        /// <summary>
        /// Returns the pending friend requests sent by the current user, newest first,
        /// including the addressee's username, display name and avatar.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of outgoing requests; 401 if no user is logged in.</returns>
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

        /// <summary>
        /// Returns the current user's accepted friends, each enriched with its live presence
        /// status (online/away/offline) from the presence service.
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of friends; 401 if no user is logged in.</returns>
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

        /// <summary>
        /// Pushes a <c>FriendshipChanged</c> event over the chat hub to all active connections
        /// of the given user, so the client can re-fetch its friendship data live. Targeting
        /// via <c>Clients.User</c> relies on the custom user-ID provider (see Program.cs).
        /// </summary>
        /// <param name="userID">The user whose clients should be notified. No-op if empty.</param>
        private async Task NotifyFriendshipChanged(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID))
            {
                return;
            }

            await _chatHub.Clients.User(userID).SendAsync("FriendshipChanged");
        }

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
    }
}