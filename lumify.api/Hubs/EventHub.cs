using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lumify.api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time calendar event updates within a workspace. Clients join the
    /// group of a workspace to receive its live events (<c>EventCreated</c>, <c>EventUpdated</c>,
    /// <c>EventDeleted</c>) broadcast by the <see cref="Controllers.EventsController"/>. Requires
    /// an authenticated connection.
    /// </summary>
    [Authorize]
    public class EventHub : Hub
    {
        /// <summary>
        /// Adds the caller's connection to a workspace group so it receives that workspace's
        /// event updates. No-op if <paramref name="workspaceID"/> is empty.
        /// </summary>
        /// <param name="workspaceID">The workspace to join.</param>
        public async Task JoinWorkspace(string workspaceID)
        {
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, workspaceID);
        }

        /// <summary>
        /// Removes the caller's connection from a workspace group. No-op if
        /// <paramref name="workspaceID"/> is empty.
        /// </summary>
        /// <param name="workspaceID">The workspace to leave.</param>
        public async Task LeaveWorkspace(string workspaceID)
        {
            if (string.IsNullOrWhiteSpace(workspaceID))
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, workspaceID);
        }
    }
}