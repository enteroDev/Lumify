using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using lumify.api.Models.Context;
using lumify.api.Models.EF;
using lumify.api.Models.Enum;




namespace lumify.api.Services
{
    public class FriendshipService
    {
        private readonly LumifyDbContext _db;

        public FriendshipService(LumifyDbContext db)
        {
            _db = db;
        }


        // ------------------------- //
        // --- Public Functions ---- //
        // ------------------------- //
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

            Friendship? existingFriendship = await _db.Friendships
                .FirstOrDefaultAsync(x =>
                    x.UserLowID == userLowID &&
                    x.UserHighID == userHighID &&
                    x.DeletedAt == null);

            if (existingFriendship != null)
            {
                throw new Exception("Friendship already exists.");
            }

            string now = DateTime.UtcNow.ToString("o");

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
            await _db.SaveChangesAsync();
        }

        // Returns the RequesterID so the caller can notify the counterpart about the change.
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

        // Returns the RequesterID so the caller can notify the counterpart about the change.
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