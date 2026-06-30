
// --- DTOs --- //

/** A workspace as delivered to the client. Mirrors the backend `WorkspaceResponse`. */
export type WorkspaceDTO = {
    /** Workspace ID. */
    id: string;
    /** The owner's user ID. */
    ownerID: string;
    /** Workspace name. */
    name: string;
    /** Creation timestamp (ISO-8601 string). */
    createdAt?: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt?: string;
};

/** A workspace member with profile and role. Mirrors the backend `WorkspaceMemberResponse`. */
export type WorkspaceMembersDTO = {
    /** The member's user ID. */
    userID: string;
    /** Avatar URL, or `null` if none. */
    avatarUrl: string | null;
    /** Display name, or `null` if none. */
    displayName: string | null;
    /** Username. */
    username: string;
    /** E-mail address, or `null` if hidden. */
    email: string | null;
    /** Membership role: 1 = Owner, 2 = Admin, 3 = User. */
    role: number;
}



// --- VIEWMODELS --- //

/** A workspace enriched for display (owner name and member count). */
export type WorkspaceVM = {
    /** Workspace ID. */
    id: string;
    /** Workspace name. */
    name: string;
    /** The owner's user ID. */
    ownerID: string;
    /** The owner's display name. */
    ownerName: string;
    /** Number of members. */
    memberCount: number;
};



// --- UNIONS --- //

/** The active space: either the user's private space or a specific workspace. */
export type SpaceType =
    | { type: "private" }
    | { type: "workspace"; workspaceID: string; name: string };
