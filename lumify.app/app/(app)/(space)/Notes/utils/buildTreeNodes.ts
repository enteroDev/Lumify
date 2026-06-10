import type { Folder, Note, TreeNode } from "../../../../../models/notes";

type FolderEntry = {
    folder: Folder;
    childFolderIds: string[];
    childNoteIds: string[];
};

export function buildTreeNodes(folders: Folder[], notes: Note[]): TreeNode[] {

    // Index folders
    const folderById = new Map<string, Folder>();
    const folderEntryById = new Map<string, FolderEntry>();

    for (const folder of folders) {
        folderById.set(folder.id, folder);
        folderEntryById.set(folder.id, { folder, childFolderIds: [], childNoteIds: [] });
    }

    // Build folder -> child folder relations
    const rootFolderIds: string[] = [];
    for (const folder of folders) {
        if (!folder.parentFolderID) {
            rootFolderIds.push(folder.id);
            continue;
        }

        const parent = folderEntryById.get(folder.parentFolderID);
        if (!parent) {
            rootFolderIds.push(folder.id);
            continue;
        } // Orphan -> treat as root

        parent.childFolderIds.push(folder.id);
    }

    // Build folder -> note relations
    const rootNoteIds: string[] = [];
    for (const note of notes) {
        if (!note.folderID) {
            rootNoteIds.push(note.id);
            continue;
        }

        const parent = folderEntryById.get(note.folderID);
        if (!parent) {
            rootNoteIds.push(note.id);
            continue;
        } // Orphan -> treat as root

        parent.childNoteIds.push(note.id);
    }

    // Index notes for fast lookup
    const noteById = new Map<string, Note>();
    for (const note of notes) {
        noteById.set(note.id, note);
    }

    // Deterministic order (optional but usually desired)
    const sortIdsByName = <T extends { id: string; name: string }>(ids: string[], map: Map<string, T>) => {
        ids.sort((a, b) => (map.get(a)?.name ?? "").localeCompare(map.get(b)?.name ?? ""));
    };

    sortIdsByName(rootFolderIds, folderById);
    sortIdsByName(rootNoteIds, noteById);

    for (const entry of folderEntryById.values()) {
        sortIdsByName(entry.childFolderIds, folderById);
        sortIdsByName(entry.childNoteIds, noteById);
    }

    // Recursive builder
    const buildFolderNode = (folderID: string): TreeNode => {
        const entry = folderEntryById.get(folderID);
        const folder = entry?.folder ?? folderById.get(folderID);

        if (!folder) {
            return { id: folderID, name: "Unknown Folder", type: "folder", children: [] };
        }

        const children: TreeNode[] = [];

        if (entry) {
            for (const childFolderID of entry.childFolderIds) {
                children.push(buildFolderNode(childFolderID));
            }

            for (const childNoteID of entry.childNoteIds) {
                const note = noteById.get(childNoteID);
                if (!note) { continue; }

                children.push({
                    id: note.id,
                    name: note.name,
                    type: "note",
                });
            }
        }

        return {
            id: folder.id,
            name: folder.name,
            type: "folder",
            children,
        };
    };

    // Root nodes = root folders + root notes
    const roots: TreeNode[] = [];

    for (const folderID of rootFolderIds) {
        roots.push(buildFolderNode(folderID));
    }

    for (const noteID of rootNoteIds) {
        const note = noteById.get(noteID);
        if (!note) { continue; }

        roots.push({
            id: note.id,
            name: note.name,
            type: "note",
        });
    }

    return roots;
}