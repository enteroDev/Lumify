// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Icons
import SuitcaseIcon from "@/app/src/svg/suitcase.svg";
import TodoIcon from "@/app/src/svg/todo.svg";
import NoteIcon from "@/app/src/svg/folder.svg";
import EventIcon from "@/app/src/svg/calendar.svg";
import GroupIcon from "@/app/src/svg/group_3.svg";
import SaveIcon from "@/app/src/svg/save.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
// Styles
import styles from "./WorkspaceCard.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    header:             styles["header"],
    title:              styles["title"],
    info:               styles["info"],
    infoLabel:          styles["infoLabel"],
    infoValue:          styles["infoValue"],
    memberTag:          styles["memberTag"],
    memberCount:        styles["memberCount"],
    memberIcon:         styles["memberIcon"],

    body:               styles["body"],

    featureBox:         styles["featureBox"],
    item:               styles["item"],
    itemLabel:          styles["itemLabel"],
    itemIcon:           styles["itemIcon"],

    contextButton:      styles["contextButton"],
    contextMenu:        styles["contextMenu"],
    contextMenuOpen:    styles["contextMenuOpen"],
    contextMenuItem:    styles["contextMenuItem"],

    icon:               styles["icon"],
    titleInput:         styles["titleInput"],
    editWrap:           styles["editWrap"],
    actions:            styles["actions"],
    abortButton:        styles["abortButton"],
    saveButton:         styles["saveButton"],
} as const;

export type WorkspaceCardProps = {
    workspaceID?: string;
    name: string | null;

    ownerName: string | null;
    memberCount?: number;

    isEditing?: boolean;
    currentUserIsOwner?: boolean;

    onAddWorkspace?: (workspaceID: string, name: string) => Promise<void> | void;
    onCancelAddWorkspace?: (workspaceID: string) => void;
    onDeleteWorkspace?: (workspaceID: string) => void;
    onEditWorkspace?: (workspaceID: string) => void;

    onOpenSpace?: () => void;
    onOpenTodos?: () => void;
    onOpenEvents?: () => void;
    onOpenNotes?: () => void;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function WorkspaceCard({
    workspaceID,
    name,
    ownerName,
    memberCount,

    isEditing = false,
    currentUserIsOwner = false,

    onOpenSpace,
    onOpenTodos,
    onOpenEvents,
    onOpenNotes,

    onAddWorkspace,
    onCancelAddWorkspace,
    onDeleteWorkspace,
    onEditWorkspace,
}: WorkspaceCardProps) {

    const [contextMenuOpen, setContextMenuOpen] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [draftName, setDraftName] = useState(name ?? "");



    // --------------- //
    // --- Handler --- //
    // --------------- //
    const openContextMenu = (e: React.MouseEvent<HTMLDivElement>) => {
        e.stopPropagation();
        setContextMenuOpen(prev => !prev);
    };

    const closeContextMenu = () => {
        setContextMenuOpen(false);
    };

    const handleCancelEdit = () => {
        if (!workspaceID || !onCancelAddWorkspace) { return; }

        setIsSaving(false);
        onCancelAddWorkspace(workspaceID);
    };

    const handleAddWorkspace = async () => {
        if (isSaving) { return; }
        if (!workspaceID || !onAddWorkspace) { return; }

        try {
            setIsSaving(true);
            await onAddWorkspace(workspaceID, draftName);
        } finally {
            setIsSaving(false);
        }
    };

    const handleEditWorkspace = () => {
        if (!workspaceID || !onEditWorkspace) { return; }

        closeContextMenu();
        onEditWorkspace(workspaceID);
    };

    const handleDeleteWorkspace = () => {
        if (!workspaceID || !onDeleteWorkspace) { return; }

        closeContextMenu();
        onDeleteWorkspace(workspaceID);
    };




    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // RENDER: Render Context menu - depending if owner or not.
    const renderContextMenuItems = () => {

        // OWNER
        if (currentUserIsOwner) {
            return (
                <>
                    <div
                        className={c.contextMenuItem}
                        title="Space bearbeiten"
                        onClick={(e) => {
                            e.stopPropagation();
                            handleEditWorkspace();
                        }}
                    >
                        Bearbeiten
                    </div>

                    <div
                        className={c.contextMenuItem}
                        title="Space löschen"
                        onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteWorkspace();
                        }}
                    >
                        Löschen
                    </div>
                </>
            );
        }

        // Member
        return (
            <div
                className={c.contextMenuItem}
                title="Space verlassen"
                onClick={(e) => {
                    e.stopPropagation();
                    closeContextMenu();
                }}
            >
                Verlassen
            </div>
        );
    };

    // RENDER: Render Header Title - Label OR EditMode with Input and actions
    const renderHeaderTitle = () => {
        if (isEditing) {
            return (
                <div className={c.editWrap}>
                    {/* Input */}
                    <input
                        autoFocus
                        value={draftName}
                        className={c.titleInput}
                        onClick={(e) => e.stopPropagation()}
                        onChange={(e) => setDraftName(e.currentTarget.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") {
                                void handleAddWorkspace();
                            }

                            if (e.key === "Escape") {
                                handleCancelEdit();
                            }
                        }}
                        onBlur={() => {
                            void handleAddWorkspace();
                        }}
                    />

                    {/* Actions */}
                    <div className={c.actions}>
                        <button
                            type="button"
                            disabled={isSaving}
                            className={c.saveButton}
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={() => { void handleAddWorkspace(); }}
                        >
                            <SaveIcon />
                        </button>

                        <button
                            type="button"
                            disabled={isSaving}
                            className={c.abortButton}
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={handleCancelEdit}
                        >
                            <AbortIcon />
                        </button>
                    </div>
                </div>
            );
        }

        return (
            <div className={c.title}>{name}</div>
        );
    };




    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        setDraftName(name ?? "");
    }, [name, isEditing]);




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div
            className={c.container}
            onMouseLeave={closeContextMenu}
            onClick={() => {
                closeContextMenu();
                if (isEditing) { return; }
                onOpenSpace?.();
            }}
        >

            {/* HEADER */}
            <div className={c.header}>

                {/* Title/editInput */}
                {renderHeaderTitle()}

                {/* Infos */}
                <div className={c.info}>
                    <div className={c.infoLabel}>Owner:</div>
                    <div className={c.infoValue}>{ownerName}</div>
                </div>

                {/* MemberTag */}
                {!isEditing && (
                    <span className={c.memberTag}>
                        <div className={c.memberIcon}><GroupIcon /></div>
                        <div className={c.memberCount}>{memberCount ?? 0}</div>
                    </span>
                )}
            </div>

            {/* ContextButton: Hide in editMode */}
            {!isEditing && (
                <div className={c.contextButton} onClick={openContextMenu}>...</div>
            )}


            {/* BODY */}
            <div className={c.body}>
                <div className={c.icon}><SuitcaseIcon /></div>
            </div>


            {/* FEATURE-OVERLAY: Hide in editMode */}
            {!isEditing && (
                <div className={c.featureBox}>
                    <div
                        className={c.item}
                        onClick={(e) => {
                            e.stopPropagation();
                            onOpenTodos?.();
                        }}
                    >
                        <div className={c.itemLabel}>Todos</div>
                        <div className={c.itemIcon}><TodoIcon /></div>
                    </div>

                    <div
                        className={c.item}
                        onClick={(e) => {
                            e.stopPropagation();
                            onOpenEvents?.();
                        }}
                    >
                        <div className={c.itemLabel}>Events</div>
                        <div className={c.itemIcon}><EventIcon /></div>
                    </div>

                    <div
                        className={c.item}
                        onClick={(e) => {
                            e.stopPropagation();
                            onOpenNotes?.();
                        }}
                    >
                        <div className={c.itemLabel}>Notes</div>
                        <div className={c.itemIcon}><NoteIcon /></div>
                    </div>
                </div>
            )}


            {/* CONTEXT-MENU: Hide in editMode */}
            {contextMenuOpen && !isEditing && (
                <div
                    className={`${c.contextMenu} ${contextMenuOpen ? c.contextMenuOpen : ""}`}
                    onClick={(e) => e.stopPropagation()}
                >
                    {renderContextMenuItems()}
                </div>
            )}
        </div>
    );
}