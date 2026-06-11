"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef, useState, type KeyboardEvent } from "react";
// Styles & Icons
import styles from "./Header.module.css";
import CloseIcon from "@/app/src/svg/close.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
import RenameIcon from "@/app/src/svg/rename_2.svg";
import SaveIcon from "@/app/src/svg/save.svg";
// Models
import type { Note } from "@/models/notes";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type HeaderProps = {
    note: Note;
    onClose: () => void;
    onSaveNote?: (noteID: string, name: string) => Promise<void> | void;
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
    note,
    onClose,
    onSaveNote
}: HeaderProps) {

    const [isEditing, setIsEditing] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [value, setValue] = useState(note.name);
    const inputRef = useRef<HTMLInputElement | null>(null);


    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //

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
                {note.name}
            </div>

            {/* Button: EditTitle */}
            <button
                type="button"
                className={c.editButton}
                onClick={startEdit}
            >
                <RenameIcon />
            </button>
        </>
    );

    const startEdit = () => {
        setValue(note.name);
        setIsEditing(true);
    };

    const abortEdit = () => {
        setValue(note.name);
        setIsEditing(false);
    };



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    // Handle Save TitleName
    const onSave = async () => {
        const trimmed = value.trim();

        if (!trimmed || trimmed === note.name) {
            abortEdit();
            return;
        }

        setIsSaving(true);

        try {
            await onSaveNote?.(note.id, trimmed);
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
        setValue(note.name);
    }, [note.name]);

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