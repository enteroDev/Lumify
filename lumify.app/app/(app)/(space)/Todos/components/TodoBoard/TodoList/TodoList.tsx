"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Components
import ActionLine from "./ActionLine/ActionLine";
import TodoListEntry from "./TodoListEntry/TodoListEntry";
// Icons
import EditIcon from "@/app/src/svg/rename_2.svg";
import SaveIcon from "@/app/src/svg/save.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
// Styles
import styles from "./TodoList.module.css";
// Models
import type { TodoEntryDTO, TodoListDTO } from "@/models/Todos";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],

    header:         styles["header"],
    titleArea:      styles["titleArea"],
    actionArea:     styles["actionArea"],
    editButton:     styles["editButton"],

    editWrap:       styles["editWrap"],
    titleInput:     styles["titleInput"],
    saveButton:     styles["saveButton"],
    abortButton:    styles["abortButton"],

    body:           styles["body"],
    empty:          styles["empty"],

    footer:         styles["footer"],
} as const;

export type TodoListProps = {
    todoList: TodoListDTO;
    todoEntries: TodoEntryDTO[];

    isEditing: boolean;
    editingTodoEntryID: string | null;

    onStartEdit?: (todoListID: string) => void;
    onCancelEdit?: (todoListID: string) => void;
    onCancelEditTodoEntry: (todoEntryID: string) => void;

    onAddTodoList: (todoListID: string, name: string) => void;
    onSaveTodoList?: (todoListID: string, name: string) => Promise<void> | void;

    onCreateDraftTodoEntry: (todoListID: string) => void;
    onAddTodoEntry: (todoEntryID: string, todoListID: string, name: string, description?: string) => void;
    onSaveTodoEntry: (todoEntry: TodoEntryDTO) => Promise<void> | void;

    ondeleteTodoList: (todoListID: string) => void;
    onDeleteTodoEntry: (todoEntryID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoList({
    todoList,
    todoEntries,

    isEditing,
    editingTodoEntryID,

    onStartEdit,
    onCancelEdit,
    onCancelEditTodoEntry,

    onAddTodoList,
    onSaveTodoList,

    onCreateDraftTodoEntry,
    onAddTodoEntry,
    onSaveTodoEntry,

    ondeleteTodoList,
    onDeleteTodoEntry,
}: TodoListProps) {


    const [draftName, setDraftName] = useState(todoList.name);
    const [isSaving, setIsSaving] = useState(false);


    // --------------- //
    // --- Handler --- //
    // --------------- //
    const handleCancelEdit = () => {
        setIsSaving(false);
        onCancelEdit?.(todoList.id);
    };

    const handleSaveTodoList = async () => {
        if (isSaving) { return; }

        try {
            setIsSaving(true);

            if (todoList.id.startsWith("draft_")) {
                await onAddTodoList(todoList.id, draftName);
                return;
            }

            if (!onSaveTodoList) { return; }
            await onSaveTodoList(todoList.id, draftName);

        } finally {
            setIsSaving(false);
        }
    };

    const handleCreateDraftTodoEntry = () => {
        onCreateDraftTodoEntry(todoList.id);
    };




    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //
    const renderHeader = () => {
        if (isEditing) {
            return (
                <div className={c.editWrap}>
                    <input
                        autoFocus
                        value={draftName}
                        className={c.titleInput}
                        onMouseDown={(e) => e.stopPropagation()}
                        onClick={(e) => e.stopPropagation()}
                        onChange={(e) => setDraftName(e.currentTarget.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") {
                                void handleSaveTodoList();
                            }

                            if (e.key === "Escape") {
                                handleCancelEdit();
                            }
                        }}
                        onBlur={() => {
                            void handleSaveTodoList();
                        }}
                    />

                    <div className={c.actionArea}>
                        <button
                            type="button"
                            disabled={isSaving}
                            className={c.saveButton}
                            onMouseDown={(e) => e.preventDefault()}
                            onClick={() => { void handleSaveTodoList(); }}
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
            <>
                <div className={c.titleArea}>
                    <h2>{todoList.name}</h2>
                </div>

                <div className={c.actionArea}>
                    <div
                        className={c.editButton}
                        onClick={() => onStartEdit?.(todoList.id)}
                    >
                        <EditIcon />
                    </div>
                </div>
            </>
        );
    };

    const renderBodyContent = () => {
        if (todoEntries.length === 0) {
            return <div className={c.empty}>Todoliste ist leer</div>;
        }

        return todoEntries.map((entry) => (
            <TodoListEntry
                key={entry.id}
                entry={entry}

                isEditing={editingTodoEntryID === entry.id}
                onCancelEdit={onCancelEditTodoEntry}

                onAddTodoEntry={onAddTodoEntry}
                onSaveTodoEntry={onSaveTodoEntry}

                onDeleteTodoEntry={onDeleteTodoEntry}
            />
        ));
    };



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        setDraftName(todoList.name);
    }, [todoList.name, isEditing]);




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container} data-id={todoList.id}>

            {/* HEADER */}
            <div className={c.header}>
                {renderHeader()}
            </div>

            {/* BODY */}
            <div className={c.body}>

                {/* TodoListEntries OR EmptyMessage */}
                {renderBodyContent()}
            </div>

            {/* FOOTER */}
            <div className={c.footer}>
                <ActionLine 
                    onCreateDraftTodoEntry={handleCreateDraftTodoEntry}
                    ondeleteTodoList={() => ondeleteTodoList(todoList.id)}
            />
            </div>
        </div>
    );
}