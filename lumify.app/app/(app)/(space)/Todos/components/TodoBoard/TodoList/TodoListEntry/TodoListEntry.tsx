"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Icons
import EditIcon from "@/app/src/svg/rename_2.svg";
import SaveIcon from "@/app/src/svg/save.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
import DeleteIcon from "@/app/src/svg/trash.svg";
// Styles
import styles from "./TodoListEntry.module.css";
// Models
import type { TodoEntryDTO } from "@/models/todo";
import { TODO_STATUS } from "@/models/todo";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    title:          styles["title"],
    editWrap:       styles["editWrap"],
    titleInput:     styles["titleInput"],

    checkBoxWrap:   styles["checkBoxWrap"],
    checkbox:       styles["checkbox"],
    checkboxLabel:  styles["checkboxLabel"],
    checkboxInput:  styles["checkboxInput"],
    checkboxBox:    styles["checkboxBox"],
    checkIcon:      styles["checkIcon"],

    actions:        styles["actions"],
    editButton:     styles["editButton"],
    saveButton:     styles["saveButton"],
    abortButton:    styles["abortButton"],
    deleteButton:   styles["deleteButton"],
} as const;

export type TodoListEntryProps = {
    entry: TodoEntryDTO;
    isEditing: boolean;

    onStartEdit?: (todoEntryID: string) => void;
    onCancelEdit: (todoEntryID: string) => void;
    onAddTodoEntry: (todoEntryID: string, todoListID: string, name: string) => void;
    onSaveTodoEntry: (todoEntry: TodoEntryDTO) => Promise<void> | void;

    onDeleteTodoEntry: (todoEntryID: string) => void;
};




// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoListEntry({
    entry,
    isEditing,

    onStartEdit,
    onCancelEdit,
    onAddTodoEntry,
    onSaveTodoEntry,

    onDeleteTodoEntry,
}: TodoListEntryProps) {

    const [draftName, setDraftName] = useState(entry.name);
    const [isSaving, setIsSaving] = useState(false);



    // --------------- //
    // --- Handler --- //
    // --------------- //
    const handleCancelEdit = () => {
        setIsSaving(false);
        onCancelEdit(entry.id);
    };

    const handleRenameTodoEntry = async () => {
        if (isSaving) { return; }

        try {
            setIsSaving(true);

            if (entry.id.startsWith("draft_")) {
                await onAddTodoEntry(entry.id, entry.todoListID, draftName);
                return;
            }

            const updatedTodoEntry: TodoEntryDTO = {
                ...entry,
                name: draftName,
            };

            await onSaveTodoEntry(updatedTodoEntry);

        } finally {
            setIsSaving(false);
        }
    };

    const handleToggleTodoEntry = async () => {
        const updatedTodoEntry: TodoEntryDTO = {
            ...entry,
            status: entry.status === TODO_STATUS.DONE
                ? TODO_STATUS.PENDING
                : TODO_STATUS.DONE,
        };

        await onSaveTodoEntry(updatedTodoEntry);
    };



    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //
    const renderEditMode = () => {
        return (
            <div className={c.container}>
                <div className={c.editWrap}>
                    <input
                        autoFocus
                        type="text"
                        className={c.titleInput}
                        value={draftName}
                        placeholder="Titel eingeben..."
                        onChange={(e) => setDraftName(e.currentTarget.value)}
                        onKeyDown={(e) => {

                            if (e.key === "Enter") {
                                void handleRenameTodoEntry();
                            }

                            if (e.key === "Escape") {
                                handleCancelEdit();
                            }
                        }}
                        onBlur={() => {
                            void handleRenameTodoEntry();
                        }}
                        spellCheck={false}
                    />

                    {/* Actions */}
                    <div className={c.actions}>

                        {/* Button: Save */}
                        <button
                            type="button"
                            disabled={isSaving}
                            className={c.saveButton}
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={() => { void handleRenameTodoEntry(); }}
                        >
                            <SaveIcon />
                        </button>

                        {/* Button: Abort */}
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
            </div>
        );
    };

    const renderDisplayMode = () => {
        return (
            <div className={c.container}>
                <div className={c.title}>{entry.name}</div>

                {/* ActionArea */}
                <div className={c.actions}>

                    {/* EditButton */}
                    {!entry.id.startsWith("draft_") && onStartEdit && (
                        <button
                            type="button"
                            className={c.editButton}
                            onClick={() => onStartEdit(entry.id)}
                        >
                            <EditIcon />
                        </button>
                    )}

                    {/* DeleteButton */}
                    <button
                        type="button"
                        className={c.deleteButton}
                        onClick={() => onDeleteTodoEntry(entry.id)}
                    >
                        <DeleteIcon />
                    </button>

                    {/* CheckBox */}
                    <div className={c.checkBoxWrap}>
                        <label className={c.checkboxLabel}>
                            <input
                                type="checkbox"
                                className={c.checkboxInput}
                                checked={entry.status === TODO_STATUS.DONE}
                                onChange={() => { void handleToggleTodoEntry(); }}
                            />

                            <span className={c.checkboxBox}>
                                {entry.status === TODO_STATUS.DONE && <span className={c.checkIcon}>✓</span>}
                            </span>
                        </label>
                    </div>

                </div>
            </div>
        );
    };



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        setDraftName(entry.name);
    }, [entry.name, isEditing]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return isEditing ? renderEditMode() : renderDisplayMode();
}