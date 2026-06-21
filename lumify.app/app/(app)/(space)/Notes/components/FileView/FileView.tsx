/**
 * FileView.tsx
 * In this file, we implement the FileView component, which is responsible for displaying the file browser interface.
 * It includes a header for navigation and search, a body for displaying files and folders, and a footer for additional actions.
 */

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useMemo, useState } from "react";
// Models
import type { TreeNode } from "@/models/Notes";
// Components
import FileViewHeader from "./FileViewHeader/FileViewHeader";
import BrowserContent from "./BrowserContent/BrowserContent";
import ActionBar from "./ActionBar/ActionBar";
// Styles
import styles from "./FileView.module.css";
// Utils
import { buildFileViewVM, getParentFolderId } from "../../utils/fileViewUtils";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
    header: styles["header"],
    body: styles["body"],
    footer: styles["footer"],
} as const;

export type FileViewProps = {
    nodes: TreeNode[];
    selectedID: string | null;
    selectedType: "folder" | "note" | null;
    openedFolderID: string | null;
    editingID: string | null;
    onSelect: (id: string) => void;
    onUnselect: () => void;

    onOpenFolder: (folderID: string | null) => void;
    onOpenNote?: (noteID: string) => void;

    onCreateFolder: (parentFolderID: string | null) => void;                        // Start "draft create" (shows new folder immediately + starts rename in UI)
    onCreateNote: (folderID: string | null) => void;                                // Start "draft create" for note

    onCommitFolder: (folderID: string, name: string) => Promise<void> | void;       // Commit name (Enter) and save to database
    onCommitNote: (noteID: string, name: string) => Promise<void> | void;           // Commit note name (Enter) and save to database                              // Cancel note edit mode (Escape)

    onEditFolder: (folderID: string) => void;
    onEditNote: (noteID: string) => void;

    onCancelEditFolder: (folderID: string) => void;                                 // Cancel edit mode (Escape)
    onCancelEditNote: (noteID: string) => void;                                     // Cancel edit mode (Escape)

    onSaveFolder: (folderID: string,name: string) => Promise<void>;
    onSaveNote: (noteID: string, name: string) => Promise<void>;

    onDeleteFolder: (folderID: string) => void;
    onDeleteNote: (noteID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FileView({
    nodes,
    selectedID,
    selectedType,
    openedFolderID,
    editingID,
    onSelect,
    onUnselect,
    onOpenFolder,
    onOpenNote,
    onCreateFolder,
    onCreateNote,
    onCommitFolder,
    onCommitNote,
    onEditFolder,
    onEditNote,
    onCancelEditFolder,
    onCancelEditNote,
    onSaveFolder,
    onSaveNote,
    onDeleteFolder,
    onDeleteNote,
}: FileViewProps) {


    // -------------- //
    // --- States --- //
    // -------------- //

    // State for search query. Triggers a re-render of the FileView when the query changes.
    //  - Starts with empty string (No search-filter)
    //  - Query is the search term entered by the user
    //  - setQuery is the function to use the query and display items accordingly
    const [query, setQuery] = useState<string>("");


    // ------------- //
    // --- Memos --- //
    // ------------- //

    // Builds and caches the FileView ViewModel based on current state. (Controls what is displayed in the FileView)
    // useMemo caches the state, and only recompute when nodes, openedFolderID, or query change. (This optimizes performance by avoiding unnecessary recalculations.)
    const vm = useMemo(() => {
        return buildFileViewVM(nodes, openedFolderID, query);
    }, [nodes, openedFolderID, query]);



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    const handleItemClick = (item: TreeNode) => {
        onSelect(item.id);
    };

    // Open item separately from simple selection
    const handleItemDoubleClick = (item: TreeNode) => {
        if (item.type === "folder") {
            onOpenFolder(item.id);
            return;
        }

        if (onOpenNote) {
            onOpenNote(item.id);
        }
    };

    const handleCreateFolder = () => {
        onCreateFolder(vm.currentFolderID);
    };

    const handleCreateNote = () => {
        onCreateNote(vm.currentFolderID);
    };

    const handleBack = () => {
        const backId = getParentFolderId(nodes, vm.currentFolderID);
        onOpenFolder(backId);
    };


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    // Root shows just "/"; deeper folders show "/ A / B".
    const path = vm.breadcrumb ? "/ " + vm.breadcrumb : "/";


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>

                {/* FileViewHeader */}
                <FileViewHeader
                    path={path}
                    query={query}
                    setQuery={setQuery}
                    onBack={handleBack}
                    canGoBack={vm.currentFolderID !== null}
                />
            </div>


            {/* BODY */}
            <div
                className={c.body}
                onClick={(e) => {
                    if (e.target === e.currentTarget) {
                        onUnselect();
                    }
                }}
            >
                
                {/* BrowserContent */}
                <BrowserContent
                    selectedID={selectedID}
                    editingID={editingID}
                    notFound={vm.notFound}
                    isEmpty={vm.isEmpty}
                    items={vm.items}

                    onItemClick={handleItemClick}
                    onItemDoubleClick={handleItemDoubleClick}

                    onCommitFolder={onCommitFolder}
                    onCommitNote={onCommitNote}

                    onEditFolder={onEditFolder}
                    onEditNote={onEditNote}

                    onSaveFolder={onSaveFolder}
                    onSaveNote={onSaveNote}

                    onCancelEditFolder={onCancelEditFolder}
                    onCancelEditNote={onCancelEditNote}
                />
            </div>


            {/* FOOTER */}
            <div className={c.footer}>

                {/* ActionBar */}
                <ActionBar
                    selectedID={selectedID}
                    selectedType={selectedType}

                    onCreateFolder={handleCreateFolder}
                    canCreateFolder={!!onCreateFolder}
                    onCreateNote={handleCreateNote}
                    canCreateNote={!!onCreateNote}

                    onDeleteFolder={onDeleteFolder}
                    onDeleteNote={onDeleteNote}

                    onEditFolder={onEditFolder}
                    onEditNote={onEditNote}
                />
            </div>
        </div>
    );
}