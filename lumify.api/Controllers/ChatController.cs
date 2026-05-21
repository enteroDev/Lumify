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
    [Route("chat/[action]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly LumifyDbContext _db;

        public ChatController(ILogger<ChatController> logger, LumifyDbContext db)
        {
            _logger = logger;
            _db = db;
        }



        // ----------- //
        // --- GET --- //
        // ----------- //
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
