"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useMemo, useState } from "react";
// Components
import SidePanel from "../../../../components/SidePanel/SidePanel";
import TreeView from "./components/TreeView/TreeView";
import FileView from "./components/FileView/FileView";
import Overlay from "@/components/OverlayContainer/OverlayContainer";
// Models
import type { Folder, Note, Note_TextBlock, Note_LinkItem, TreeNode } from "@/models/notes";
// Utils
import { buildTreeNodes } from "./utils/buildTreeNodes";
// Provider
import { useSpace } from "../../../../components/_Space/SpaceProvider";
import { useToast } from "../../../../components/Toast/ToastProvider";
import { useAlert } from "../../../../components/AlertModal/AlertProvider";
import { useModal } from "@/components/Modal/ModalProvider";
// Services
import { FolderService } from "@/services/api/folderServices";
import { NoteService } from "@/services/api/noteService";
import { UserService } from "@/services/api/userService";
// Hooks
import { useNoteHub } from "@/hooks/useNoteHub";



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Notes() {

    const { isPrivate, workspaceID } = useSpace();
    const { showAlert } = useAlert();
    const { openNoteModal } = useModal();
    const toast = useToast();



    // -------------- //
    // --- States --- //
    // -------------- //

    const [treeViewSelectedID, setTreeViewSelectedID] = useState<string | null>(null);
    const [treeViewSelectedType, setTreeViewSelectedType] = useState<"folder" | "note" | null>(null);

    const [fileViewSelectedID, setFileViewSelectedID] = useState<string | null>(null);
    const [fileViewSelectedType, setFileViewSelectedType] = useState<"folder" | "note" | null>(null);

    const [openedFolderID, setOpenedFolderID] = useState<string | null>(null);
    const [editingID, setEditingID] = useState<string | null>(null);
    const [expandedIds, setExpandedIDs] = useState<Set<string>>(new Set());

    const [folders, setFolders] = useState<Folder[]>([]);
    const [notes, setNotes] = useState<Note[]>([]);



    // --------------------- //
    // --- Build UI-Tree --- //
    // --------------------- //
    const nodes = useMemo(() => buildTreeNodes(folders, notes), [folders, notes]);

    // Safety net: if the currently opened item no longer exists in the tree (e.g. it or one of
    // its ancestors was deleted - including deletions coming in via the hub from other users),
    // fall back to the root so the FileView never gets stuck on a "not found" message.
    useEffect(() => {
        if (openedFolderID === null) { return; }

        const exists = (items: TreeNode[]): boolean =>
            items.some(x => x.id === openedFolderID || (x.children ? exists(x.children) : false));

        if (!exists(nodes)) {
            setOpenedFolderID(null);
        }
    }, [nodes, openedFolderID]);



    // ------------------ //
    // --- UI Helpers --- //
    // ------------------ //
    const getItemType = (id: string): "folder" | "note" | null => {
        if (folders.some(x => x.id === id)) { return "folder"; }
        if (notes.some(x => x.id === id)) { return "note"; }
        return null;
    };

    const clearFileSelection = () => {
        setFileViewSelectedID(null);
        setFileViewSelectedType(null);
    };

    const clearTreeSelection = () => {
        setTreeViewSelectedID(null);
        setTreeViewSelectedType(null);
    };

    const unselectAll = () => {
        clearFileSelection();
        clearTreeSelection();
    };



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    const onToggle = (id: string) => {
        setExpandedIDs(prev => {
            const next = new Set(prev);
            if (next.has(id)) { next.delete(id); } else { next.add(id); }
            return next;
        });
    };

    const onTreeViewSelect = (id: string | null) => {
        setTreeViewSelectedID(id);

        if (id === null) {
            setTreeViewSelectedType(null);
            return;
        }

        setTreeViewSelectedType(getItemType(id));
    };

    const onFileViewSelect = (id: string) => {
        setFileViewSelectedID(id);
        setFileViewSelectedType(getItemType(id));
        setTreeViewSelectedID(id);
        setTreeViewSelectedType(getItemType(id));
    };

    const onUnselect = () => {
        unselectAll();
    };

    // OPEN folder
    const onOpenFolder = (folderID: string | null) => {
        setOpenedFolderID(folderID);

        // Clear selected value and type upon opening folder
        setFileViewSelectedID(null);
        setFileViewSelectedType(null);

        if (folderID === null) {
            return;
        }

        // Keep track of selected Folder in Fileview, by setting the selectedID and type in the TreeView
        setTreeViewSelectedID(folderID);
        setTreeViewSelectedType("folder");
    };



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {

        let cancelled = false;

        const load = async () => {
            try {

                let folders: Folder[] = [];
                let notes: Note[] = [];

                if (isPrivate) {
                    folders = await FolderService.getAllFoldersOfUser();
                    notes = await NoteService.getAllNotesOfUser();

                } else if (workspaceID) {
                    folders = await FolderService.getAllFoldersOfWorkspace(workspaceID);
                    notes = await NoteService.getAllNotesOfWorkspace(workspaceID);
                }

                if (cancelled) { return; }
                setFolders(folders);
                setNotes(notes);

            } catch (error) {

                if (cancelled) { return; }
                console.error("Failed to load notes page data", error);

                setFolders([]);
                setNotes([]);
                toast.error("Fehler beim Laden der Daten.");
            }
        };

        void load();

        return () => { cancelled = true; };

    }, [isPrivate, workspaceID, toast]);

    // HUB-HOOK
    useNoteHub({
        isPrivate,
        workspaceID,

        onFolderCreated: (folder) => {
            setFolders(prev => {
                const existingRealFolder = prev.some(x => x.id === folder.id);
                if (existingRealFolder) { return prev; }

                const matchingDraft = prev.find(x =>
                    x.id.startsWith("draft_") &&
                    x.name === folder.name &&
                    (x.parentFolderID ?? null) === (folder.parentFolderID ?? null)
                );

                if (matchingDraft) {
                    return prev.map(x => {
                        if (x.id !== matchingDraft.id) { return x; }

                        return {
                            ...x,
                            ...folder,
                        };
                    });
                }

                return [...prev, folder];
            });
        },

        onFolderUpdated: (folder) => {
            setFolders(prev => prev.map(x => {
                if (x.id !== folder.id) { return x; }

                return {
                    ...x,
                    ...folder,
                };
            }));
        },

        onFolderDeleted: ({ folderID }) => {
            setFolders(prev => prev.filter(x => x.id !== folderID));
            setNotes(prev => prev.filter(x => x.folderID !== folderID));

            if (fileViewSelectedID === folderID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === folderID) {
                clearTreeSelection();
            }

            if (openedFolderID === folderID) {
                setOpenedFolderID(null);
            }

            if (editingID === folderID) {
                setEditingID(null);
            }

            setExpandedIDs(new Set());
        },

        onNoteCreated: (note) => {
            setNotes(prev => {
                const existingRealNote = prev.some(x => x.id === note.id);
                if (existingRealNote) { return prev; }

                const matchingDraft = prev.find(x =>
                    x.id.startsWith("draft_") &&
                    x.name === note.name &&
                    (x.folderID ?? null) === (note.folderID ?? null)
                );

                if (matchingDraft) {
                    return prev.map(x => {
                        if (x.id !== matchingDraft.id) { return x; }
                        return note;
                    });
                }

                return [...prev, note];
            });
        },

        onNoteUpdated: (note) => {
            setNotes(prev => prev.map(x => {
                if (x.id !== note.id) { return x; }

                return {
                    ...x,
                    ...note,
                };
            }));
        },

        onNoteDeleted: ({ noteID }) => {
            setNotes(prev => prev.filter(x => x.id !== noteID));

            if (fileViewSelectedID === noteID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === noteID) {
                clearTreeSelection();
            }

            if (editingID === noteID) {
                setEditingID(null);
            }
        },
    });




    // ------------- //
    // --- Logic --- //
    // ------------- //

    /* --- */
    /* ADD */
    /* --- */
    const createDraftFolder = (parentFolderID: string | null) => {
        // Generate random id for the draft-folder & assign it
        const tempID = Math.random().toString(36).substring(2, 11);
        const draftID = `draft_${tempID}`;

        const draft: Folder = {
            id: draftID,
            name: "",
            parentFolderID: parentFolderID,
        };

        setFolders(prev => [...prev, draft]);       // Create DraftFolder in the FileView visually
        setEditingID(draftID);                      // Set DraftFolder into editingMode
        setFileViewSelectedID(draftID);             // Set DraftFolder as selectedItem
        setFileViewSelectedType("folder");          // Set SelectedType of item to "folder"
    };


    const addFolder = async (folderID: string, name: string) => {

        // 0.) PREPARE INPUT
        const trimmedName = name.trim();

        console.log("### START ###");
        console.log("Folder to add: '" + trimmedName + "'");

        // 1.) HADNLE EMPTY NAMEFIELD - If trimmedName is empty, Clear GUI -> Unselect Item, end editing Mode, and delete draftFolder
        if (!trimmedName) {
            console.warn("Folder name is empty. Reverting changes...");
            setFolders(prev => prev.filter(x => x.id !== folderID));
            setEditingID(null);

            if (fileViewSelectedID === folderID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === folderID) {
                clearTreeSelection();
            }

            return;
        }

        // 3.) GET DRAFTFOLDER OBJECT -> used to get parentID for the actual folder
        const draftFolder = folders.find(x => x.id === folderID);
        if (!draftFolder) { return; }

        setFolders(prev => prev.map(x => {
            if (x.id !== folderID) { return x; }

            return {
                ...x,
                name: trimmedName,
            };
        }));

        const parentFolderName = draftFolder.name ? draftFolder.name : "root"
        console.log("Trying to add folder: '" + trimmedName + "' to parentFolder: '" + parentFolderName + "'");

        // 4.) TRY FETCH
        try {

            // Create folderObject for database and fetch it
            const createdFolder = await FolderService.addFolder(
                trimmedName,
                draftFolder.parentFolderID ?? null,
                isPrivate ? null : workspaceID
            );

            console.log("Successfully added folder to database:", createdFolder);

            // Refreshs folders in UI
            setFolders(prev => prev.map(x => {
                if (x.id !== folderID) { return x; }

                return {
                    ...x,
                    id: createdFolder.id,
                    name: createdFolder.name,
                    parentFolderID: createdFolder.parentFolderID ?? null,
                    description: createdFolder.description ?? "",
                    ownerID: createdFolder.ownerID,
                    workspaceID: createdFolder.workspaceID ?? null,
                    createdAt: createdFolder.createdAt,
                    updatedAt: createdFolder.updatedAt,
                } as Folder;
            }));

            // End editing mode and set new folder as selected
            setEditingID(null);
            setFileViewSelectedID(createdFolder.id);
            setFileViewSelectedType("folder");

            toast.success("Ordner wurde erstellt.");
        } catch (error: unknown) {
            console.error("### ERROR CAUGHT ###");

            if (error instanceof Error) {
                console.error("Message:", error.message);
            }

            if (typeof error === "object" && error !== null && "response" in error) {
                const err = error as { response?: { data?: unknown; status?: number }; request?: unknown };

                if (err.response) {
                    // Server responded with status code
                    console.error("API Data:", err.response.data);
                    console.error("API Status:", err.response.status);
                } else if (err.request) {
                    // Request sent but no response received
                    console.error("No response received from API. Is the .NET server running on port 5331?");
                }
            }

            toast.error("Fehler beim Erstellen des Ordners");
        }

        console.log("### END ###");
        console.log("");
    };


    const createDraftNote = (folderID: string | null) => {
        // Generate random id for the draft-note & assign it
        const tempID = Math.random().toString(36).substring(2, 11);
        const draftID = `draft_${tempID}`;

        const draft: Note = {
            id: draftID,
            ownerID: "",
            workspaceID: isPrivate ? null : workspaceID,
            folderID,
            name: "",
            createdAt: "",
            updatedAt: "",
        };

        setNotes(prev => [...prev, draft as Note]);
        setEditingID(draftID);
        setFileViewSelectedID(draftID);
        setFileViewSelectedType("note");
    };


    const addNote = async (noteID: string, name: string) => {
        const trimmedName = name.trim();

        if (!trimmedName) {
            setNotes(prev => prev.filter(x => x.id !== noteID));
            setEditingID(null);

            if (fileViewSelectedID === noteID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === noteID) {
                clearTreeSelection();
            }

            return;
        }

        const draftNote = notes.find(x => x.id === noteID);
        if (!draftNote) { return; }

        setNotes(prev => prev.map(x => {
            if (x.id !== noteID) { return x; }

            return {
                ...x,
                name: trimmedName,
            };
        }));

        try {
            const createdNote = await NoteService.addNote({
                name: trimmedName,
                folderID: draftNote.folderID ?? null,
                workspaceID: isPrivate ? null : workspaceID,
            });

            setNotes(prev =>
                prev.map(x => (x.id !== noteID ? x : createdNote)),
            );

            setEditingID(null);
            setFileViewSelectedID(createdNote.id);
            setFileViewSelectedType("note");

            toast.success("Note wurde erstellt.");

        } catch (error) {
            console.error("Failed to create note", error);
            toast.error("Fehler beim Erstellen der Note");
        }
    };


    /* ----------- */
    /* EDIT / SAVE */
    /* ----------- */
    const startEditFolder = (folderID: string) => {
        setEditingID(folderID);
        setFileViewSelectedID(folderID);
        setFileViewSelectedType("folder");
    };

    const cancelEditFolder = (folderID: string) => {
        const isDraft = folderID.startsWith("draft_");

        if (isDraft) {
            setFolders(prev => prev.filter(x => x.id !== folderID));

            if (fileViewSelectedID === folderID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === folderID) {
                clearTreeSelection();
            }
        }
        setEditingID(null);
    };


    const saveFolder = async (folderID: string, name: string) => {
        const trimmedName = name.trim();
        if (!trimmedName) { return; }

        try {
            const savedFolder = await FolderService.saveFolder({
                id: folderID,
                name: trimmedName,
            });

            setFolders(prev => prev.map(x => {
                if (x.id !== folderID) { return x; }

                return {
                    ...x,
                    name: savedFolder.name,
                    description: savedFolder.description ?? x.description,
                    parentFolderID: savedFolder.parentFolderID ?? null,
                    updatedAt: savedFolder.updatedAt,
                };
            }));

            setEditingID(null);
            setFileViewSelectedID(folderID);
            setFileViewSelectedType("folder");
            toast.success("Ordner wurde umbenannt.");

        } catch (error) {
            console.error("Failed to save folder", error);
            toast.error("Fehler beim Speichern des Ordners");
        }
    };

    const startEditNote = (noteID: string) => {
        setEditingID(noteID);
        setFileViewSelectedID(noteID);
        setFileViewSelectedType("note");
    };


    const cancelEditNote = (noteID: string) => {
        const isDraft = noteID.startsWith("draft_");

        if (isDraft) {
            setNotes(prev => prev.filter(x => x.id !== noteID));

            if (fileViewSelectedID === noteID) {
                clearFileSelection();
            }

            if (treeViewSelectedID === noteID) {
                clearTreeSelection();
            }
        }

        setEditingID(null);
    };


    const saveNote = async (noteID: string, name: string) => {
        const trimmedName = name.trim();
        if (!trimmedName) { return; }

        try {
            const savedNote = await NoteService.saveNote({
                id: noteID,
                name: trimmedName,
            });

            setNotes(prev => prev.map(x => {
                if (x.id !== noteID) { return x; }

                return {
                    ...x,
                    name: savedNote.name,
                    folderID: savedNote.folderID ?? null,
                    updatedAt: savedNote.updatedAt,
                };
            }));

            setEditingID(null);
            setFileViewSelectedID(noteID);
            setFileViewSelectedType("note");

            toast.success("Note wurde umbenannt.");

        } catch (error) {
            console.error("Failed to save note", error);
            toast.error("Fehler beim Speichern der Note");
        }
    };


    /* ------ */
    /* DELETE */
    /* ------ */
    const deleteFolder = (folderID: string) => {
        const folder = folders.find(x => x.id === folderID);

        showAlert({
            title: "Ordner löschen",
            message: `Ordner "${folder?.name ?? folderID}" löschen?

            Damit werden auch alle Ordner und Notizen innerhalb gelöscht!`,
            status: "delete",
            confirmText: "Ja",
            cancelText: "Nein",
            onConfirm: async () => {
                try {
                    await FolderService.deleteFolder(folderID);

                    if (isPrivate) {
                        const folders = await FolderService.getAllFoldersOfUser();
                        const notes = await NoteService.getAllNotesOfUser();

                        setFolders(folders);
                        setNotes(notes);

                    } else if (workspaceID) {
                        const folders = await FolderService.getAllFoldersOfWorkspace(workspaceID);
                        const notes = await NoteService.getAllNotesOfWorkspace(workspaceID);

                        setFolders(folders);
                        setNotes(notes);
                    }

                    if (fileViewSelectedID === folderID) {
                        clearFileSelection();
                    }

                    if (treeViewSelectedID === folderID) {
                        clearTreeSelection();
                    }

                    // If the deleted folder was the "opened" one, fall back to its parent folder
                    // (or root) instead of leaving the FileView pointed at a now-missing node.
                    if (openedFolderID === folderID) {
                        setOpenedFolderID(folder?.parentFolderID ?? null);
                    }

                    setExpandedIDs(new Set());

                    toast.success("Ordner wurde gelöscht.");
                } catch (error) {
                    console.error("Failed to delete folder", error);

                    toast.error("Fehler beim Löschen des Ordners");
                }
            }
        });
    };

    const deleteNote = (noteID: string) => {
        const note = notes.find(x => x.id === noteID);

        showAlert({
            title: "Notiz löschen",
            message: `Notiz "${note?.name ?? noteID}" löschen?`,
            status: "delete",
            confirmText: "Ja",
            cancelText: "Nein",
            onConfirm: async () => {
                try {
                    await NoteService.deleteNote(noteID);

                    setNotes(prev => prev.filter(x => x.id !== noteID));

                    if (fileViewSelectedID === noteID) {
                        clearFileSelection();
                    }

                    if (treeViewSelectedID === noteID) {
                        clearTreeSelection();
                    }

                    // If the deleted note was the "opened" item (e.g. it was selected via the
                    // TreeView, which opens it in the FileView), stay in its parent folder instead
                    // of leaving the FileView pointed at a now-missing node ("not found").
                    if (openedFolderID === noteID) {
                        setOpenedFolderID(note?.folderID ?? null);
                    }

                    toast.success("Note wurde gelöscht.");

                } catch (error) {
                    console.error("Failed to delete note", error);

                    toast.error("Fehler beim Löschen der Note");
                }
            }
        });
    };


    /* ---- */
    /* OPEN */
    /* ---- */
    const openNote = async (noteID: string) => {
        const note = notes.find(x => x.id === noteID);
        if (!note) { return; }

        try {
            // Get neccessary information of the note
            const [textBlocks, linkItems, user] = await Promise.all([
                NoteService.getTextBlocksOfNote(noteID),
                NoteService.getLinkItemsOfNote(noteID),
                UserService.getUserAccountInfoWithID(note.ownerID),
            ]);

            // Add additional NoteInfos to the NoteObject - In this case we add the ownerName to display the name in the noteModal
            const fullNote: Note = {
                ...note,
                ownerName: `${user.firstName ?? "N/A"} ${user.lastName ?? ""}`.trim(),
            };

            setFileViewSelectedID(noteID);
            setFileViewSelectedType("note");

            openNoteModal({
                note: fullNote,
                textBlocks: textBlocks,
                linkItems: linkItems,
            });

        } catch (error) {

            console.error("Failed to open note", error);
            toast.error("Fehler beim Laden der Notiz.");
        }
    };



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className="scrollView">

            {/* Overlay */}
            <Overlay />

            {/* Content */}
            <div className="content">

                {/* SidePanel */}
                <SidePanel title="Baumansicht">

                    {/* TreeView */}
                    <TreeView
                        nodes={nodes}
                        selectedId={treeViewSelectedID}
                        expandedIds={expandedIds}
                        onToggle={onToggle}
                        onSelect={onTreeViewSelect}
                        onOpen={onOpenFolder}
                    />
                </SidePanel>

                {/* FileView */}
                <FileView
                    nodes={nodes}
                    selectedID={fileViewSelectedID}
                    selectedType={fileViewSelectedType}
                    editingID={editingID}
                    openedFolderID={openedFolderID}

                    onSelect={onFileViewSelect}
                    onUnselect={onUnselect}

                    onOpenFolder={onOpenFolder}
                    onOpenNote={openNote}

                    onCreateFolder={createDraftFolder}
                    onCreateNote={createDraftNote}

                    onCommitFolder={addFolder}
                    onCommitNote={addNote}

                    onCancelEditFolder={cancelEditFolder}
                    onCancelEditNote={cancelEditNote}

                    onEditFolder={startEditFolder}
                    onEditNote={startEditNote}

                    onSaveFolder={saveFolder}
                    onSaveNote={saveNote}

                    onDeleteFolder={deleteFolder}
                    onDeleteNote={deleteNote}
                />
            </div>
        </div>
    );
}