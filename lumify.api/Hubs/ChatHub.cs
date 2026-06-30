using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using lumify.api.Interfaces;
using lumify.api.Models.Context;
using lumify.api.Models.EF;
using lumify.api.Models.Enum;




namespace lumify.api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat and user presence. Tracks each user's active connections
    /// via the presence service, broadcasts presence changes to all clients, and handles joining
    /// rooms, leaving rooms and sending messages. Requires an authenticated connection.
    /// </summary>
    /// <remarks>
    /// Presence is connection-counted: a user is online while at least one connection is open.
    /// Chat rooms are SignalR groups keyed by room ID; messages are persisted before being
    /// broadcast to the room.
    /// </remarks>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IPresenceService _presenceService;
        private readonly LumifyDbContext _db;

        /// <summary>
        /// Creates the hub with its injected presence service and database context.
        /// </summary>
        public ChatHub(IPresenceService presenceService, LumifyDbContext db)
        {
            _presenceService = presenceService;
            _db = db;
        }




        /// <summary>
        /// Called when a client connects: registers the connection with the presence service and
        /// broadcasts a <c>PresenceChanged</c> event marking the user online.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            string? userID = Context.User?.FindFirst("UserID")?.Value;

            if (!string.IsNullOrWhiteSpace(userID))
            {
                _presenceService.AddConnection(userID, Context.ConnectionId);

                await Clients.All.SendAsync("PresenceChanged", userID, PresenceStatus.Online);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects: removes the connection from the presence service
        /// and broadcasts a <c>PresenceChanged</c> event with the user's recomputed status
        /// (offline once their last connection closes).
        /// </summary>
        /// <param name="exception">The error that caused the disconnect, if any.</param>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string? userID = Context.User?.FindFirst("UserID")?.Value;

            _presenceService.RemoveConnection(Context.ConnectionId);

            if (!string.IsNullOrWhiteSpace(userID))
            {
                PresenceStatus newStatus = _presenceService.GetPresenceStatus(userID);

                await Clients.All.SendAsync("PresenceChanged", userID, newStatus);
            }

            await base.OnDisconnectedAsync(exception);
        }





        /// <summary>
        /// Adds the caller's connection to a chat room (SignalR group) so it receives that
        /// room's messages. No-op if <paramref name="roomID"/> is empty.
        /// </summary>
        /// <param name="roomID">The room to join.</param>
        public async Task JoinRoom(string roomID)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomID);
        }

        /// <summary>
        /// Removes the caller's connection from a chat room (SignalR group). No-op if
        /// <paramref name="roomID"/> is empty.
        /// </summary>
        /// <param name="roomID">The room to leave.</param>
        public async Task LeaveRoom(string roomID)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomID);
        }




        /// <summary>
        /// Persists a chat message from the current user and broadcasts it to everyone in the
        /// room as a <c>MessageReceived</c> event. Silently ignores empty room/content or an
        /// unauthenticated caller.
        /// </summary>
        /// <param name="roomID">The room to post to.</param>
        /// <param name="content">The message text.</param>
        public async Task SendMessage(string roomID, string content)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            string? userID = Context.User?.FindFirst("UserID")?.Value;

            if (string.IsNullOrWhiteSpace(userID))
            {
                return;
            }

            var now = DateTime.UtcNow.ToString("o");

            var chatMessage = new ChatMessage
            {
                ID = Guid.NewGuid().ToString(),
                RoomID = roomID.Trim(),
                SenderID = userID,
                Content = content.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                DeletedAt = null
            };

            _db.ChatMessages.Add(chatMessage);
            await _db.SaveChangesAsync();

            var senderQuery = _db.Users
                .Where(user => user.ID == chatMessage.SenderID && user.DeletedAt == null)
                .Select(user => new
                {
                    SenderName = user.DisplayName
                        ?? user.Username
                        ?? (user.FirstName != null && user.LastName != null
                            ? user.FirstName + " " + user.LastName
                            : user.FirstName ?? user.LastName ?? user.ID)
                });

            var sender = await senderQuery.FirstOrDefaultAsync();

            await Clients.Group(chatMessage.RoomID).SendAsync("MessageReceived", new
            {
                id = chatMessage.ID,
                roomID = chatMessage.RoomID,
                senderID = chatMessage.SenderID,
                senderName = sender?.SenderName ?? chatMessage.SenderID,
                content = chatMessage.Content,
                createdAt = chatMessage.CreatedAt,
                updatedAt = chatMessage.UpdatedAt
            });
        }
    }
}