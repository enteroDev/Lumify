using Microsoft.AspNetCore.SignalR;




namespace lumify.api.Hubs
{
    // Maps the custom "UserID" claim to the SignalR user identifier, so that
    // Clients.User(userID) targets all of a user's active hub connections.
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("UserID")?.Value;
        }
    }
}
