using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lumify.api.Hubs
{
    [Authorize]
    public class NoteHub : Hub
    {
        public async Task JoinWorkspace(string workspaceID)
        {
            if (string.IsNullOrWhiteSpace(workspaceID)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, workspaceID);
        }

        public async Task LeaveWorkspace(string workspaceID)
        {
            if (string.IsNullOrWhiteSpace(workspaceID)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, workspaceID);
        }
    }
}