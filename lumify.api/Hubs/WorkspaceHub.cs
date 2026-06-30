using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lumify.api.Hubs
{
    /// <summary>
    /// SignalR hub for real-time workspace and membership updates. Unlike the other workspace
    /// hubs, clients join a group keyed by their own <em>user ID</em> (a user can belong to many
    /// workspaces), so the <see cref="Controllers.WorkspaceController"/> can push events such as
    /// <c>WorkspaceCreated</c>, <c>WorkspaceUpdated</c>, <c>WorkspaceDeleted</c>,
    /// <c>WorkspaceMemberAdded</c> and <c>WorkspaceMemberRemoved</c> to every affected member.
    /// Requires an authenticated connection.
    /// </summary>
    [Authorize]
    public class WorkspaceHub : Hub
    {
        /// <summary>
        /// Adds the caller's connection to its own user group so it receives workspace/membership
        /// events that concern this user. No-op if <paramref name="userID"/> is empty.
        /// </summary>
        /// <param name="userID">The user (group) to join.</param>
        public async Task JoinUser(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, userID);
        }

        /// <summary>
        /// Removes the caller's connection from its user group. No-op if
        /// <paramref name="userID"/> is empty.
        /// </summary>
        /// <param name="userID">The user (group) to leave.</param>
        public async Task LeaveUser(string userID)
        {
            if (string.IsNullOrWhiteSpace(userID)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userID);
        }
    }
}