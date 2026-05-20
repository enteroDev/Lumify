using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lumify.api.Hubs
{
    [Authorize]
    public class WorkspaceHub : Hub
    {
        public async Task JoinUser(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, userID);
        }

        public async Task LeaveUser(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userID);
        }
    }
}