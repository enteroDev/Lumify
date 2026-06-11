"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles & Icons
import styles from "./ActionBar.module.css";
import FolderAddIcon from "../../../../../../src/svg/folder_add.svg";
import DocumentAddIcon from "../../../../../../src/svg/document_add.svg";
import DeleteIcon from "../../../../../../src/svg/trash.svg";
import EditIcon from "../../../../../../src/svg/rename.svg";
// Provider
import { useTooltip } from "../../../../../../../components/Tooltip/TooltipProvider";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    actionButton:       styles["action-button"],
    deleteButton:       styles["delete-button"],
    divider:            styles["divider"],
} as const;

export type ActionBarProps = {
    selectedID: string | null;
    selectedType: "folder" | "note" | null;

    onCreateFolder?: () => void;
    canCreateFolder?: boolean;

    onCreateNote?: () => void;
    canCreateNote?: boolean;

    onEditFolder: (folderID: string) => void;
    onEditNote: (noteID: string) => void;

    onDeleteFolder: (folderID: string) => void;
    onDeleteNote: (noteID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ActionBar({
    selectedID,
    selectedType,
    onCreateFolder,
    canCreateFolder,
    onCreateNote,
    canCreateNote,
    onEditFolder,
    onEditNote,
    onDeleteFolder,
    onDeleteNote,
}: ActionBarProps) {

    const { showTooltip, hideTooltip } = useTooltip();


    // ------------------ //
    // --- UI Helpers --- //
    // ------------------ //
    const handleTooltipMove = (e: React.MouseEvent<HTMLButtonElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };


    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    const onEdit = () => {
        if (!selectedID || !selectedType) {
            return;
        }

        if (selectedType === "folder") {
            onEditFolder(selectedID);
            return;
        }

        onEditNote(selectedID);
    };

    const onDelete = () => {
        console.log("DELETE CLICK", { selectedID, selectedType });

        if (!selectedID || !selectedType) {
            return;
        }

        if (selectedType === "folder") {
            onDeleteFolder(selectedID);
            return;
        }

        onDeleteNote(selectedID);
    };


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const folderDisabled = canCreateFolder === false || !onCreateFolder;
    const noteDisabled = canCreateNote === false || !onCreateNote;
    const editDisabled = !selectedID || !selectedType;
    const deleteDisabled = !selectedID || !selectedType;


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {/* CREATE */}
            {/* Button: AddFolder */}
            <button
                type="button"
                className={c.actionButton}
                onClick={onCreateFolder}
                disabled={folderDisabled}
                onMouseEnter={(e) => handleTooltipMove(e, "Ordner erstellen")}
                onMouseMove={(e) => handleTooltipMove(e, "Ordner erstellen")}
                onMouseLeave={hideTooltip}
            >
                <FolderAddIcon />
            </button>

            {/* Button: AddNote */}
            <button
                type="button"
                className={c.actionButton}
                onClick={onCreateNote}
                disabled={noteDisabled}
                onMouseEnter={(e) => handleTooltipMove(e, "Notiz erstellen")}
                onMouseMove={(e) => handleTooltipMove(e, "Notiz erstellen")}
                onMouseLeave={hideTooltip}
            >
                <DocumentAddIcon />
            </button>


            {/* MODIFY */}
            {/* Divider */}
            <div className={c.divider}></div>

            {/* Button: RenameItem */}
            <button
                type="button"
                className={c.actionButton}
                onClick={onEdit}
                disabled={editDisabled}
                onMouseEnter={(e) => handleTooltipMove(e, "Umbenennen")}
                onMouseMove={(e) => handleTooltipMove(e, "Umbenennen")}
                onMouseLeave={hideTooltip}
            >
                <EditIcon />
            </button>


            {/* DANGER */}
            {/* Divider */}
            <div className={c.divider}></div>

            {/* Button: DeleteItem */}
            <button
                type="button"
                className={c.deleteButton}
                onClick={onDelete}
                disabled={deleteDisabled}
                onMouseEnter={(e) => handleTooltipMove(e, "Löschen")}
                onMouseMove={(e) => handleTooltipMove(e, "Löschen")}
                onMouseLeave={hideTooltip}
            >
                <DeleteIcon />
            </button>
        </div>
    );
}