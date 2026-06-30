// --- UNIONS --- //

/** Text block kind: 0 = plain text, 1 = code. */
export type NoteBlockType = 0 | 1;



// --- DTOs --- //

/** A note folder. Mirrors the relevant fields of the backend `FolderResponse`. */
export type Folder = {
    /** Folder ID. */
    id: string;
    /** Folder name. */
    name: string;
    /** Parent folder, or `null` if at the root. */
    parentFolderID: string | null;
    /** Optional description. */
    description?: string;
};

/** A note (metadata only; its modules are fetched separately). Mirrors the backend `NoteResponse`. */
export type Note = {
    /** Note ID. */
    id: string;
    /** The owner's user ID. */
    ownerID: string;
    /** The creator's display name (privacy-safe). */
    ownerName?: string | null;
    /** The owning workspace, or `null` for a personal note. */
    workspaceID: string | null;
    /** The containing folder, or `null` if at the root. */
    folderID: string | null;
    /** Note title. */
    name: string;
    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};

/** A link item module of a note (a labelled URL). Mirrors the backend `LinkItemResponse`. */
export type Note_LinkItem = {
    /** Link item ID. */
    id: string;
    /** The parent note. */
    noteID: string;
    /** Optional display label. */
    label?: string;
    /** The target URL. */
    url: string;
    /** Position within the note. */
    notePos: number;
    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};

/** A text block module of a note. Mirrors the backend `TextblockResponse`. */
export type Note_TextBlock = {
    /** Text block ID. */
    id: string;
    /** The parent note. */
    noteID: string;
    /** Block kind (see {@link NoteBlockType}). */
    type: NoteBlockType;
    /** Optional block title/heading. */
    name?: string;
    /** The block's text content. */
    content?: string;
    /** For code blocks, the language for syntax highlighting. */
    codeLanguage?: string;
    /** Position within the note. */
    notePos: number;
    /** Whether the block is collapsed in the UI. */
    isCollapsed: boolean;
    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};



// --- ViewModels --- //

/** A node in the folder/note file tree — either a folder (with children) or a note. */
export type TreeNode = {
    /** The folder or note ID. */
    id: string;
    /** Display name. */
    name: string;
    /** Whether this node is a folder or a note. */
    type: "folder" | "note";
    /** Child nodes (folders only). */
    children?: TreeNode[];
};
