
// DTO's
export type WorkspaceDTO = {
    id: string;
    ownerID: string;
    name: string;
    createdAt?: string;
    updatedAt?: string;
};

export type WorkspaceMembersDTO = {
    userID: string;
    avatarUrl: string | null;
    displayName: string | null;
    username: string;
    email: string | null;
    role: number;
}


// Viemodels
export type WorkspaceVM = {
    id: string;
    name: string;
    ownerID: string;
    ownerName: string;
    memberCount: number;
};



// Space-Types
export type SpaceType =
    | { type: "private" }
    | { type: "workspace"; workspaceID: string; name: string };


