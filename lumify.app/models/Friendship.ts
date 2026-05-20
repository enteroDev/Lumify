
export type FriendshipDTO = {
    id: string;

    requesterID: string;
    addresseeID: string;

    status: number;
    createdAt: string;

    requesterUsername?: string | null;
    requesterDisplayName?: string | null;
    requesterAvatarUrl?: string | null;

    friendUserID?: string | null;
    friendUsername?: string | null;
    friendDisplayName?: string | null;
    friendAvatarUrl?: string | null;
};


export enum FriendshipStatus {
    None = 0,
    PendingIncoming = 1,
    PendingOutgoing = 2,
    Friend = 3,
    Blocked = 4,
}