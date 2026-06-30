
/** A friendship/friend-request as delivered to the client. Mirrors the backend `FriendshipResponse`. */
export type FriendshipDTO = {
    /** Friendship ID. */
    id: string;

    /** The user who sent the request. */
    requesterID: string;
    /** The user who received the request. */
    addresseeID: string;

    /** Raw status code from the backend. */
    status: number;
    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;

    /** Requester's username (populated for incoming requests). */
    requesterUsername?: string | null;
    /** Requester's display name (populated for incoming requests). */
    requesterDisplayName?: string | null;
    /** Requester's avatar URL (populated for incoming requests). */
    requesterAvatarUrl?: string | null;

    /** The friend's user ID (populated for outgoing requests / accepted friends). */
    friendUserID?: string | null;
    /** The friend's username (populated for outgoing requests / accepted friends). */
    friendUsername?: string | null;
    /** The friend's display name (populated for outgoing requests / accepted friends). */
    friendDisplayName?: string | null;
    /** The friend's avatar URL (populated for outgoing requests / accepted friends). */
    friendAvatarUrl?: string | null;
};


/**
 * UI-oriented friendship status from the current user's perspective. Note this differs from the
 * backend's stored status — it distinguishes incoming vs outgoing pending requests so the UI can
 * render the right action.
 */
export enum FriendshipStatus {
    /** No relationship.  */
    None = 0,
    /** A request sent to the current user, awaiting their response. */
    PendingIncoming = 1,
    /** A request the current user sent, awaiting the other side. */
    PendingOutgoing = 2,
    /** An accepted friendship. */
    Friend = 3,
    /** The relationship is blocked. */
    Blocked = 4,
}
