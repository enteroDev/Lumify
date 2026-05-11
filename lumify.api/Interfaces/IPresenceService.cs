/* Interface for PresenceService
 * Interface currently only contains: Online / Offline
 */


using lumify.api.Models.Enum;


namespace lumify.api.Interfaces
{
    public interface IPresenceService
    {
        void AddConnection(string userID, string connectionID);
        void RemoveConnection(string connectionID);
        PresenceStatus GetPresenceStatus(string userID);
    }
}