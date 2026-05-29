
// --- DTO --- //
export type UserProfile = {
    id: string;
    avatarUrl: string | null;
    displayName: string;
    bio: string | null;
};

export type UserAccountInfo = {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    username: string;
    createdAt: string;
    updatedAt: string;
};

export type UserPreviewDTO = {
    id: string;

    username: string;
    email: string;

    displayName?: string | null;
    avatarUrl?: string | null;

    presenceStatus: PresenceStatus;
};

export type RelatedUserDTO = {
    userID: string;
    avatarUrl: string | null;
    displayName: string;
    username: string;
    email: string;
    presenceStatus?: PresenceStatus;
};



export type SaveUserProfileRequest = {
    displayName?: string;
    avatarUrl?: string;
    bio?: string;
};

export type SaveAccountInfoRequest = {
    firstName?: string;
    lastName?: string;
    email?: string;
};






// --- ENUMS --- //
export enum PresenceStatus {
    Offline = 0,
    Online = 1,
    Away = 2,
    DoNotDisturb = 3,
}