using Microsoft.AspNetCore.SignalR;




namespace lumify.api.Hubs
{
    /// <summary>
    /// Maps the custom <c>UserID</c> claim to the SignalR user identifier, so that
    /// <c>Clients.User(userID)</c> targets all of a user's active hub connections.
    /// </summary>
    public class UserIdProvider : IUserIdProvider
    {
        /// <summary>
        /// Returns the user identifier for a connection — the value of its <c>UserID</c> claim.
        /// </summary>
        /// <param name="connection">The hub connection to resolve the user for.</param>
        /// <returns>The user's ID, or <c>null</c> if the claim is absent.</returns>
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("UserID")?.Value;
        }
    }
}
