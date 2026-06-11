// --------------- //
// --- Imports --- //
// --------------- //

// Models
import type { TreeNode } from "@/models/notes";
// Components
import BrowserItem from "./BrowserItem/BrowserItem";
// Styles
import styles from "./BrowserContent.module.css";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
    empty: styles["empty"],
} as const;

export type BrowserContentProps = {
    selectedID: string | null;
    editingID: string | null;
    notFound: boolean;
    isEmpty: boolean;

    items: TreeNode[];
    onItemClick: (item: TreeNode) => void;
    onItemDoubleClick: (item: TreeNode) => void;

    onCommitFolder: (folderID: string, name: string) => void;
    onCommitNote: (noteID: string, name: string) => void;

    onEditFolder: (folderID: string) => void;
    onEditNote: (noteID: string) => void;

    onCancelEditFolder: (folderID: string) => void;
    onCancelEditNote: (noteID: string) => void;

    onSaveFolder: (folderID: string,name: string) => Promise<void>;
    onSaveNote: (noteID: string, name: string) => Promise<void>;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function BrowserContent({
    selectedID,
    editingID,
    notFound,
    isEmpty,
    items,
    onItemClick,
    onItemDoubleClick,
    onCommitFolder,
    onCommitNote,
    onEditFolder,
    onEditNote,
    onCancelEditFolder,
    onCancelEditNote,
    onSaveFolder,
    onSaveNote,
}: BrowserContentProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    if (notFound) {
        return <div className={c.empty}>Ausgewähltes Element nicht gefunden</div>;
    }

    if (isEmpty) {
        return <div className={c.empty}>Ordner ist leer</div>;
    }

    return (
        <div className={c.container}>

            {/* BrowserItem */}
            {items.map((item) => (
                <BrowserItem
                    key={item.id}
                    item={item}
                    selectedID={selectedID}
                    editingID={editingID}
                    onClick={onItemClick}
                    onDoubleClick={onItemDoubleClick}

                    onCommitFolder={onCommitFolder}
                    onCommitNote={onCommitNote}

                    onEditFolder={onEditFolder}
                    onEditNote={onEditNote}

                    onCancelEditFolder={onCancelEditFolder}
                    onCancelEditNote={onCancelEditNote}

                    onSaveFolder={onSaveFolder}
                    onSaveNote={onSaveNote}
                />
            ))}
        </div>
    );
}