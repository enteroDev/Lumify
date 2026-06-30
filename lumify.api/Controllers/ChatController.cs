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
    /// Read access to chat history. All endpoints require an authenticated user.
    /// Real-time delivery of new messages is handled by <see cref="Hubs.ChatHub"/>;
    /// this controller only serves the persisted message history.
    /// </summary>
    [ApiController]
    [Route("chat/[action]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly LumifyDbContext _db;

        /// <summary>
        /// Creates the controller with its injected logger and database context.
        /// </summary>
        public ChatController(ILogger<ChatController> logger, LumifyDbContext db)
        {
            _logger = logger;
            _db = db;
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
        /// <summary>
        /// Returns the full message history of a chat room, oldest first.
        /// </summary>
        /// <remarks>
        /// Soft-deleted messages and messages from soft-deleted users are excluded. The
        /// sender name is resolved to the user's display name (falling back to username,
        /// full name, or ID).
        /// </remarks>
        /// <param name="roomID">The room whose messages are requested.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>200 with the list of messages; 400 if <paramref name="roomID"/> is missing.</returns>
        [HttpGet]
        [ActionName("getMessagesOfRoom")]
        public async Task<ActionResult<List<ChatMessageResponse>>> GetMessagesOfRoom(string roomID, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return BadRequest("roomID is required");
            }

            var trimmedRoomID = roomID.Trim();

            var messages = await (
                from chatMessage in _db.ChatMessages
                join user in _db.Users on chatMessage.SenderID equals user.ID
                where chatMessage.RoomID == trimmedRoomID
                    && chatMessage.DeletedAt == null
                    && user.DeletedAt == null
                orderby chatMessage.CreatedAt
                select new ChatMessageResponse
                {
                    ID = chatMessage.ID,
                    RoomID = chatMessage.RoomID,
                    SenderID = chatMessage.SenderID,
                    SenderName = user.DisplayName
                        ?? user.Username
                        ?? (user.FirstName != null && user.LastName != null
                            ? user.FirstName + " " + user.LastName
                            : user.FirstName ?? user.LastName ?? user.ID),
                    Content = chatMessage.Content,
                    CreatedAt = chatMessage.CreatedAt,
                    UpdatedAt = chatMessage.UpdatedAt
                }
            ).ToListAsync(ct);

            return Ok(messages);
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
    }
}
