// --- Unions --- //
export type NoteBlockType = 0 | 1; // 0=text, 1=code



// --- DTOs --- //
export type Folder = {
    id: string;
    name: string;
    parentFolderID: string | null;
    description?: string;
};

export type Note = {
    id: string;
    ownerID: string;
    ownerName?: string | null;
    workspaceID: string | null;
    folderID: string | null;
    name: string;
    createdAt: string;
    updatedAt: string;
};

export type Note_LinkItem = {
    id: string;
    noteID: string;
    label?: string;
    url: string;
    notePos: number;
    createdAt: string;
    updatedAt: string;
};

export type Note_TextBlock = {
    id: string;
    noteID: string;
    type: NoteBlockType; // 0=text, 1=code
    name?: string;
    content?: string;
    codeLanguage?: string;
    notePos: number;
    isCollapsed: boolean;
    createdAt: string;
    updatedAt: string;
};




// --- ViewModels --- //

// Represents a folder or note in the file tree
export type TreeNode = {
    id: string;
    name: string;
    type: "folder" | "note";
    children?: TreeNode[];
};



