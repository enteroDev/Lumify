using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using lumify.api.Models.Context;
using lumify.api.Models.EF;
using lumify.api.Models.Enum;




namespace lumify.api.Services
{
    /// <summary>
    /// Encapsulates the friendship domain logic used by the
    /// <see cref="Controllers.FriendshipController"/>: sending, accepting, rejecting and
    /// removing friendships, including the validation and state transitions involved.
    /// </summary>
    /// <remarks>
    /// Each friendship is stored once per user pair using ordered <c>UserLowID</c>/<c>UserHighID</c>
    /// keys (a plain unique index, since MariaDB has no filtered indexes), so a soft-deleted row
    /// is resurrected rather than duplicated. Invalid operations throw, and the controller maps
    /// those to error responses.
    /// </remarks>
    public class FriendshipService
    {
        private readonly LumifyDbContext _db;

        /// <summary>
        /// Creates the service with its injected database context.
        /// </summary>
        public FriendshipService(LumifyDbContext db)
        {
            _db = db;
        }


        // ------------------------- //
        // --- Public Functions ---- //
        // ------------------------- //
        /// <summary>
        /// Creates a pending friend request from one user to another, or resurrects a previously
        /// removed relationship as a fresh request.
        /// </summary>
        /// <param name="requesterID">The user sending the request.</param>
        /// <param name="addresseeID">The user receiving the request.</param>
        /// <exception cref="Exception">Thrown if an ID is missing, the user adds themselves, or an
        /// active friendship already exists.</exception>
        public async Task SendFriendRequest(string requesterID, string addresseeID)
        {
            if (string.IsNullOrWhiteSpace(requesterID))
            {
                throw new Exception("RequesterID is required.");
            }

            if (string.IsNullOrWhiteSpace(addresseeID))
            {
                throw new Exception("AddresseeID is required.");
            }

            if (requesterID == addresseeID)
            {
                throw new Exception("Cannot add yourself.");
            }

            string userLowID = string.Compare(requesterID, addresseeID, StringComparison.Ordinal) < 0 ? requesterID : addresseeID;
            string userHighID = string.Compare(requesterID, addresseeID, StringComparison.Ordinal) < 0 ? addresseeID : requesterID;

            // Look up ANY row for this pair, including soft-deleted ones. The unique index
            // on (UserLowID, UserHighID) is plain (MariaDB has no filtered indexes), so a
            // leftover soft-deleted row must be resurrected rather than inserting a duplicate.
            Friendship? existingFriendship = await _db.Friendships
                .FirstOrDefaultAsync(x =>
                    x.UserLowID == userLowID &&
                    x.UserHighID == userHighID);

            // An active (not soft-deleted) relationship already exists -> nothing to do.
            if (existingFriendship != null && existingFriendship.DeletedAt == null)
            {
                throw new Exception("Friendship already exists.");
            }

            string now = DateTime.UtcNow.ToString("o");

            if (existingFriendship != null)
            {
                // Resurrect the previously removed row as a fresh pending request.
                existingFriendship.RequesterID = requesterID;
                existingFriendship.AddresseeID = addresseeID;
                existingFriendship.Status = (int)FriendshipStatus.Pending;

                existingFriendship.DeletedAt = null;
                existingFriendship.AcceptedAt = null;
                existingFriendship.RejectedAt = null;
                existingFriendship.BlockedAt = null;

                existingFriendship.CreatedAt = now;
                existingFriendship.UpdatedAt = now;
            }
            else
            {
                Friendship friendship = new Friendship
                {
                    ID = Guid.NewGuid().ToString(),

                    UserLowID = userLowID,
                    UserHighID = userHighID,

                    RequesterID = requesterID,
                    AddresseeID = addresseeID,

                    Status = (int)FriendshipStatus.Pending,

                    CreatedAt = now,
                    UpdatedAt = now,
                };

                _db.Friendships.Add(friendship);
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Accepts a pending friend request. Only the addressee may accept, and only pending
        /// requests can be accepted.
        /// </summary>
        /// <param name="friendshipID">The pending friendship to accept.</param>
        /// <param name="currentUserID">The user performing the action (must be the addressee).</param>
        /// <returns>The requester's ID, so the caller can notify the counterpart of the change.</returns>
        /// <exception cref="Exception">Thrown if an ID is missing, the friendship is not found,
        /// the caller is not the addressee, or the request is not pending.</exception>
        public async Task<string> AcceptFriendRequest(string friendshipID, string currentUserID)
        {
            if (string.IsNullOrWhiteSpace(friendshipID))
            {
                throw new Exception("FriendshipID is required.");
            }

            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                throw new Exception("CurrentUserID is required.");
            }

            Friendship? friendship = await _db.Friendships
                .FirstOrDefaultAsync(x =>
                    x.ID == friendshipID &&
                    x.DeletedAt == null);

            if (friendship == null)
            {
                throw new Exception("Friendship not found.");
            }

            if (friendship.AddresseeID != currentUserID)
            {
                throw new Exception("Not allowed.");
            }

            if (friendship.Status != (int)FriendshipStatus.Pending)
            {
                throw new Exception("Only pending friend requests can be accepted.");
            }

            string now = DateTime.UtcNow.ToString("o");

            friendship.Status = (int)FriendshipStatus.Accepted;
            friendship.AcceptedAt = now;
            friendship.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return friendship.RequesterID;
        }

        /// <summary>
        /// Rejects a pending friend request. Only the addressee may reject, and only pending
        /// requests can be rejected.
        /// </summary>
        /// <param name="friendshipID">The pending friendship to reject.</param>
        /// <param name="currentUserID">The user performing the action (must be the addressee).</param>
        /// <returns>The requester's ID, so the caller can notify the counterpart of the change.</returns>
        /// <exception cref="Exception">Thrown if an ID is missing, the friendship is not found,
        /// the caller is not the addressee, or the request is not pending.</exception>
        public async Task<string> RejectFriendRequest(string friendshipID, string currentUserID)
        {
            if (string.IsNullOrWhiteSpace(friendshipID))
            {
                throw new Exception("FriendshipID is required.");
            }

            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                throw new Exception("CurrentUserID is required.");
            }

            Friendship? friendship = await _db.Friendships
                .FirstOrDefaultAsync(x =>
                    x.ID == friendshipID &&
                    x.DeletedAt == null);

            if (friendship == null)
            {
                throw new Exception("Friendship not found.");
            }

            if (friendship.AddresseeID != currentUserID)
            {
                throw new Exception("Not allowed.");
            }

            if (friendship.Status != (int)FriendshipStatus.Pending)
            {
                throw new Exception("Only pending friend requests can be rejected.");
            }

            string now = DateTime.UtcNow.ToString("o");

            friendship.Status = (int)FriendshipStatus.Rejected;
            friendship.RejectedAt = now;
            friendship.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return friendship.RequesterID;
        }




        /// <summary>
        /// Removes an accepted friendship by soft-deleting it. Only accepted friendships can be
        /// removed.
        /// </summary>
        /// <param name="currentUserID">The user performing the removal.</param>
        /// <param name="friendID">The friend to remove.</param>
        /// <exception cref="Exception">Thrown if an ID is missing, the user removes themselves,
        /// the friendship is not found, or it is not in the accepted state.</exception>
        public async Task RemoveFriend(string currentUserID, string friendID)
        {
            if (string.IsNullOrWhiteSpace(currentUserID))
            {
                throw new Exception("CurrentUserID is required.");
            }

            if (string.IsNullOrWhiteSpace(friendID))
            {
                throw new Exception("FriendID is required.");
            }

            if (currentUserID == friendID)
            {
                throw new Exception("You cannot remove yourself as a friend.");
            }

            var userLowID = string.CompareOrdinal(currentUserID, friendID) < 0 ? currentUserID : friendID;
            var userHighID = string.CompareOrdinal(currentUserID, friendID) < 0 ? friendID : currentUserID;

            var friendship = await _db.Friendships
                .FirstOrDefaultAsync(x =>
                    x.UserLowID == userLowID &&
                    x.UserHighID == userHighID &&
                    x.DeletedAt == null);

            if (friendship == null)
            {
                throw new Exception("Friendship not found.");
            }

            if (friendship.Status != (int)FriendshipStatus.Accepted)
            {
                throw new Exception("Only accepted friendships can be removed.");
            }

            string now = DateTime.UtcNow.ToString("o");

            friendship.DeletedAt = now;
            friendship.UpdatedAt = now;

            await _db.SaveChangesAsync();
        }

        
    }
}