"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef, useState, type KeyboardEvent } from "react";
// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Models
import { WorkspaceVM } from "@/models/Space";
// Styles & Icons
import styles from "./Header.module.css";
import CloseIcon from "@/app/src/svg/close.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
import RenameIcon from "@/app/src/svg/rename_2.svg";
import SaveIcon from "@/app/src/svg/save.svg";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type HeaderProps = {
    workspace: WorkspaceVM;
    onClose: () => void;
    onSaveWorkspace?: (workspaceID: string, name: string) => Promise<void> | void;
};

const c = {
    container:      styles["container"],
    titleWrap:      styles["titleWrap"],
    title:          styles["title"],
    editWrap:       styles["editWrap"],
    titleInput:     styles["titleInput"],
    actions:        styles["actions"],
    editButton:     styles["editButton"],
    abortButton:    styles["abortButton"],
    saveButton:     styles["saveButton"],
    closeButton:    styles["closeButton"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Header({
    workspace,
    onClose,
    onSaveWorkspace,
}: HeaderProps) {

    const { showTooltip, hideTooltip } = useTooltip();

    const [isEditing, setIsEditing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [value, setValue] = useState(workspace.name);
    const inputRef = useRef<HTMLInputElement | null>(null);


    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };

    // Render EditState
    const renderEditMode  = () => (
        // EditWrap
        <div className={c.editWrap}>
            <input
                ref={inputRef}
                type="text"
                disabled={isSaving}
                className={c.titleInput}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                onKeyDown={onKeyDown}
                spellCheck={false}
            />

            {/* Actions */}
            <div className={c.actions}>

                {/* Button: SaveTitle */}
                <button
                    type="button"
                    disabled={isSaving}
                    className={c.saveButton}
                    onMouseDown={(e) => e.preventDefault()}

                    onMouseEnter={(e) => handleTooltipMove(e, "Speichern")}
                    onMouseMove={(e) => handleTooltipMove(e, "Speichern")}
                    onMouseLeave={hideTooltip}

                    onClick={onSave}
                >
                    <SaveIcon />
                </button>

                {/* Button: AbortTitleEdit */}
                <button
                    type="button"
                    disabled={isSaving}
                    className={c.abortButton}
                    onMouseDown={(e) => e.preventDefault()}

                    onMouseEnter={(e) => handleTooltipMove(e, "Abbrechen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Abbrechen")}
                    onMouseLeave={hideTooltip}

                    onClick={abortEdit}
                >
                    <AbortIcon />
                </button>
            </div>
        </div>
    );

    const renderDisplayMode  = () => (
        <>
            {/* Title */}
            <div className={c.title}>
                {workspace.name}
            </div>

            {/* Button: EditTitle */}
            <button
                type="button"
                className={c.editButton}

                onMouseEnter={(e) => handleTooltipMove(e, "Space umbenennen")}
                onMouseMove={(e) => handleTooltipMove(e, "Space umbenennen")}
                onMouseLeave={hideTooltip}

                onClick={startEdit}
            >
                <RenameIcon />
            </button>
        </>
    );

    const startEdit = () => {
        setValue(workspace.name);
        setIsEditing(true);
    };

    const abortEdit = () => {
        setValue(workspace.name);
        setIsEditing(false);
    };



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    // Handle Save TitleName
    const onSave = async () => {
        const trimmed = value.trim();

        if (!trimmed || trimmed === workspace.name) {
            abortEdit();
            return;
        }

        setIsSaving(true);

        try {
            await onSaveWorkspace?.(workspace.id, trimmed);
            setIsEditing(false);
        } finally {
            setIsSaving(false);
        }
    };

    // Handle Keydowns
    const onKeyDown = async (e: KeyboardEvent<HTMLInputElement>) => {

        // Save TitleName on "Enter"
        if (e.key === "Enter") {
            e.preventDefault();
            await onSave();
            return;
        }

        // Abort renaming on "Escape"
        if (e.key === "Escape") {
            e.preventDefault();
            abortEdit();
        }
    };


    // --------------- //
    // --- Effects --- //
    // --------------- //

    // EFFEKT: Sets value noteName, TRIGGER: "note.Name"
    useEffect(() => {
        setValue(workspace.name);
    }, [workspace.name]);

    // EFFEKT: Sets focus on input and selects content, TRIGGER: isEditing changes.
    useEffect(() => {
        if (!isEditing) { return; }
        inputRef.current?.focus();
        inputRef.current?.select();
    }, [isEditing]);



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const titleContent = isEditing ? renderEditMode() : renderDisplayMode();



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* TitleWrap */}
            <div className={c.titleWrap}>
                {titleContent}
            </div>

            {/* Button: Close NoteModal */}
            <button
                type="button"
                className={c.closeButton}
                onClick={onClose}
            >
                <CloseIcon />
            </button>

        </div>
    );
}