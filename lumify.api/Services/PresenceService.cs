/* PresenceService
 * Inherits interface IPresenceService
 */


using System.Collections.Concurrent;
using lumify.api.Interfaces;
using lumify.api.Models.Enum;



namespace lumify.api.Services
{
    /// <summary>
    /// In-memory implementation of <see cref="IPresenceService"/>. Maintains the mapping between
    /// users and their open SignalR connections in thread-safe dictionaries; a user counts as
    /// online while at least one connection is registered.
    /// </summary>
    /// <remarks>
    /// State is per-process and not persisted, so it resets on restart and is not shared across
    /// multiple server instances.
    /// </remarks>
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
        private readonly ConcurrentDictionary<string, string> _connectionUsers = new();


        /// <summary>
        /// Registers a new connection for a user, creating the user's connection set on first use.
        /// </summary>
        /// <param name="userID">The connecting user.</param>
        /// <param name="connectionID">The new SignalR connection ID.</param>
        public void AddConnection(string userID, string connectionID)
        {
            _connectionUsers[connectionID] = userID;

            _userConnections.AddOrUpdate(
                userID,
                _ => new HashSet<string> { connectionID },
                (_, existingConnections) =>
                {
                    lock (existingConnections)
                    {
                        existingConnections.Add(connectionID);
                        return existingConnections;
                    }
                });
        }


        /// <summary>
        /// Removes a connection and, if it was the user's last one, drops the user from the
        /// online set.
        /// </summary>
        /// <param name="connectionID">The SignalR connection ID that closed.</param>
        public void RemoveConnection(string connectionID)
        {
            if (!_connectionUsers.TryRemove(connectionID, out var userID))
            {
                return;
            }

            if (!_userConnections.TryGetValue(userID, out var existingConnections))
            {
                return;
            }

            lock (existingConnections)
            {
                existingConnections.Remove(connectionID);

                if (existingConnections.Count == 0)
                {
                    _userConnections.TryRemove(userID, out _);
                }
            }
        }


        /// <summary>
        /// Returns whether the user currently has any open connection.
        /// </summary>
        /// <param name="userID">The user to check.</param>
        /// <returns><c>Online</c> if at least one connection is open, otherwise <c>Offline</c>.</returns>
        public PresenceStatus GetPresenceStatus(string userID)
        {
            if (_userConnections.TryGetValue(userID, out var existingConnections))
            {
                lock (existingConnections)
                {
                    if (existingConnections.Count > 0)
                    {
                        return PresenceStatus.Online;
                    }
                }
            }

            return PresenceStatus.Offline;
        }
    }
}