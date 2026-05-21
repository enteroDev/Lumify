using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using lumify.api.Interfaces;
using lumify.api.Models.Context;
using lumify.api.Models.EF;
using lumify.api.Models.Enum;




namespace lumify.api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IPresenceService _presenceService;
        private readonly LumifyDbContext _db;

        public ChatHub(IPresenceService presenceService, LumifyDbContext db)
        {
            _presenceService = presenceService;
            _db = db;
        }




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





        public async Task JoinRoom(string roomID)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomID);
        }

        public async Task LeaveRoom(string roomID)
        {
            if (string.IsNullOrWhiteSpace(roomID))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomID);
        }




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