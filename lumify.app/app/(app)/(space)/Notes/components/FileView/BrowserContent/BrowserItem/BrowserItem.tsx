// --------------- //
// --- Imports --- //
// --------------- //

// React
import type { KeyboardEvent, MouseEvent } from "react";
// Models
import type { TreeNode } from "@/models/notes";
// Styles & Icons
import styles from "./BrowserItem.module.css";
import FolderIcon from "../../../../../../../src/svg/folder.svg";
import DocumentIcon from "../../../../../../../src/svg/document.svg";
import EditIcon from "../../../../../../../src/svg/rename_2.svg";
// Provider
import { useTooltip } from "../../../../../../../../components/Tooltip/TooltipProvider";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container: styles["container"],
    item: styles["item"],
    itemSelected: styles["itemSelected"],
    itemIcon: styles["itemIcon"],
    itemName: styles["itemName"],
    nameArea: styles["nameArea"],
    renameInput: styles["renameInput"],
    itemOverlay: styles["itemOverlay"],
} as const;

export type BrowserItemProps = {
    item: TreeNode;
    selectedID: string | null;
    onClick: (item: TreeNode) => void;
    onDoubleClick: (item: TreeNode) => void;
    editingID: string | null;

    onCommitFolder: (folderID: string, name: string) => void;
    onCommitNote: (noteID: string, name: string) => void;

    onEditFolder: (folderID: string) => void;
    onEditNote: (noteID: string) => void;

    onCancelEditFolder: (folderID: string) => void;
    onCancelEditNote: (noteID: string) => void;

    onSaveFolder: (folderID: string, name: string) => Promise<void>;
    onSaveNote: (noteID: string, name: string) => Promise<void>;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function BrowserItem({
    item,
    selectedID,
    onClick,
    onDoubleClick,
    editingID,
    onCommitFolder,
    onCommitNote,
    onEditFolder,
    onEditNote,
    onCancelEditFolder,
    onCancelEditNote,
    onSaveFolder,
    onSaveNote,
}: BrowserItemProps) {
    const isEditing = item.id === editingID;
    const isSelected = item.id === selectedID;

    const { showTooltip, hideTooltip } = useTooltip();



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Prevent browser/context/double-click behavior from bubbling up
    const swallowMouse = (e: MouseEvent<HTMLElement>) => {
        e.preventDefault();
        e.stopPropagation();
    };

    // Stop bubbling, but keep default input behavior intact
    const stopMousePropagation = (e: MouseEvent<HTMLElement>) => {
        e.stopPropagation();
    };

    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    // Handle Enter to commit and Escape to cancel
    const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
        const value = e.currentTarget.value;
        const isDraft = item.id.startsWith("draft_");

        if (e.key === "Enter") {
            if (item.type === "folder") {
                if (isDraft) {
                    onCommitFolder(item.id, value);
                    return;
                }

                void onSaveFolder(item.id, value);
                return;
            }

            if (isDraft) {
                onCommitNote(item.id, value);
                return;
            }

            void onSaveNote(item.id, value);
            return;
        }

        if (e.key === "Escape") {
            if (item.type === "folder") {
                onCancelEditFolder(item.id);
                return;
            }

            onCancelEditNote(item.id);
        }
    };

    // Cancel edit mode when the input loses focus
    const handleBlur = () => {
        if (item.type === "folder") {
            onCancelEditFolder(item.id);
            return;
        }

        onCancelEditNote(item.id);
    };

    // Start rename mode from the name area
    const handleRenameClick = (e: MouseEvent<HTMLDivElement>) => {
        e.stopPropagation();

        if (item.type === "folder") {
            onEditFolder(item.id);
            return;
        }

        onEditNote(item.id);
    };



    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // Fallback label for empty draft names
    const displayName = item.name.trim().length > 0
        ? item.name
        : item.type === "folder"
            ? "New folder"
            : "New note";

    const nameElement = isEditing ? (
        <input
            className={c.renameInput}
            autoFocus
            defaultValue={item.name}
            onKeyDown={handleKeyDown}
            onBlur={handleBlur}
            onClick={stopMousePropagation}
            onMouseDown={stopMousePropagation}
            onDoubleClick={swallowMouse}
            onContextMenu={swallowMouse}
        />
    ) : (
        displayName
    );



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div
                className={`${c.item} ${isSelected ? c.itemSelected : ""}`}
                onClick={() => onClick(item)}
                onDoubleClick={(e) => {
                    e.preventDefault();
                    onDoubleClick(item);
                }}
            >
                <div className={c.itemIcon}>
                    {item.type === "folder" ? <FolderIcon /> : <DocumentIcon />}
                </div>

                <div
                    className={c.nameArea}
                    onClick={handleRenameClick}
                    onMouseDown={stopMousePropagation}
                    onDoubleClick={swallowMouse}
                    onContextMenu={swallowMouse}
                    onMouseEnter={(e) => handleTooltipMove(e, "Umbenennen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Umbenennen")}
                    onMouseLeave={hideTooltip}
                >
                    <div className={c.itemName}>
                        {nameElement}
                        {!isEditing && <EditIcon className={styles.overlayIcon} />}
                    </div>
                </div>
            </div>
        </div>
    );
}