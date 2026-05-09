using System.Collections.Concurrent;
using lumify.api.Interfaces;
using lumify.api.Models.Enum;



namespace lumify.api.Services
{
    public class PresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
        private readonly ConcurrentDictionary<string, string> _connectionUsers = new();

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