/* Interface for PresenceService
 * Interface currently only contains: Online / Offline
 */


using lumify.api.Models.Enum;


namespace lumify.api.Interfaces
{
    /// <summary>
    /// Tracks which users are currently online by counting their active SignalR connections.
    /// Implemented by <see cref="Services.PresenceService"/> and driven by
    /// <see cref="Hubs.ChatHub"/> on connect/disconnect.
    /// </summary>
    public interface IPresenceService
    {
        /// <summary>
        /// Registers a new connection for a user (marks the user online).
        /// </summary>
        /// <param name="userID">The connecting user.</param>
        /// <param name="connectionID">The new SignalR connection ID.</param>
        void AddConnection(string userID, string connectionID);

        /// <summary>
        /// Removes a connection; the user goes offline once their last connection is gone.
        /// </summary>
        /// <param name="connectionID">The SignalR connection ID that closed.</param>
        void RemoveConnection(string connectionID);

        /// <summary>
        /// Returns the current presence status of a user.
        /// </summary>
        /// <param name="userID">The user to check.</param>
        /// <returns><c>Online</c> if at least one connection is open, otherwise <c>Offline</c>.</returns>
        PresenceStatus GetPresenceStatus(string userID);
    }
}