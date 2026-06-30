
// --- DTO --- //

/** Public profile of a user (display name, avatar, bio). Mirrors the backend `UserProfileResponse`. */
export type UserProfile = {
    /** The user's ID. */
    id: string;
    /** Avatar URL, or `null` if none. */
    avatarUrl: string | null;
    /** Public display name. */
    displayName: string;
    /** Profile bio, or `null` if none. */
    bio: string | null;
};

/** Account information of a user (name, e-mail, timestamps). Mirrors the backend `UserAccountInfoResponse`. */
export type UserAccountInfo = {
    /** The user's ID. */
    id: string;
    /** First name. */
    firstName: string;
    /** Last name. */
    lastName: string;
    /** E-mail address. */
    email: string;
    /** Username. */
    username: string;
    /** Registration date (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};

/** Compact user preview used in lists/search, with live presence. Mirrors the backend `UserPreviewResponse`. */
export type UserPreviewDTO = {
    /** The user's ID. */
    id: string;

    /** Username. */
    username: string;
    /** E-mail address. */
    email: string;

    /** Display name, if set. */
    displayName?: string | null;
    /** Avatar URL, if set. */
    avatarUrl?: string | null;

    /** Current presence status. */
    presenceStatus: PresenceStatus;
};



/** A user that can be added to a workspace, with optional live presence. Mirrors the backend `RelatedUserResponse`. */
export type RelatedUserDTO = {
    /** The user's ID. */
    userID: string;
    /** Avatar URL, or `null` if none. */
    avatarUrl: string | null;
    /** Display name. */
    displayName: string;
    /** Username. */
    username: string;
    /** E-mail address. */
    email: string;
    /** Current presence status, if known. */
    presenceStatus?: PresenceStatus;
};



/** Request payload for updating the current user's profile (only provided fields are changed). */
export type SaveUserProfileRequest = {
    /** New display name, if changing. */
    displayName?: string;
    /** New avatar URL, if changing. */
    avatarUrl?: string;
    /** New bio, if changing. */
    bio?: string;
};

/** Request payload for updating the current user's account info (only provided fields are changed). */
export type SaveAccountInfoRequest = {
    /** New first name, if changing. */
    firstName?: string;
    /** New last name, if changing. */
    lastName?: string;
    /** New e-mail address, if changing. */
    email?: string;
};





// --- ENUMS --- //

/** A user's real-time presence state. Mirrors the backend `PresenceStatus`. */
export enum PresenceStatus {
    /** No active connection. */
    Offline = 0,
    /** At least one active connection. */
    Online = 1,
    /** Connected but marked away. */
    Away = 2,
    /** Connected but not to be disturbed. */
    DoNotDisturb = 3,
}
