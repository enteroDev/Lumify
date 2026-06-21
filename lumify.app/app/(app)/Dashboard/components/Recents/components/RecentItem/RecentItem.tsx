"use client"

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Models
import { WorkspaceDTO } from "@/models/Space";
import { TodoEntryDTO } from "@/models/Todos";
import { CalendarEventDTO } from "@/models/Events";
import { Note } from "@/models/Notes";
// Styles
import styles from "./RecentItem.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    iconArea:           styles["iconArea"],
    icon:               styles["icon"],
    timeline:           styles["timeline"],

    infoArea:           styles["infoArea"],

    date:               styles["date"],

    message:            styles["message"],
    actionText:         styles["actionText"],
    itemName:           styles["itemName"],

    workspace:          styles["workspace"],
    workspaceLabel:     styles["workspaceLabel"],
    workspaceValue:     styles["workspaceValue"],
} as const;

export type RecentItemProps = {
    todo?: TodoEntryDTO;
    event?: CalendarEventDTO;
    note?: Note;

getSpaceInfoOfTodoEntry?: (todoEntryID: string) => Promise<WorkspaceDTO>;
getSpaceInfoOfEvent?: (eventID: string) => Promise<WorkspaceDTO>;
getSpaceInfoOfNote?: (noteID: string) => Promise<WorkspaceDTO>;
};

export type RecentItemType = "todo" | "event" | "note";
export type ModifierType = "create" | "edited";



// ----------------- //
// --- Component --- //
// ----------------- //
export default function RecentItem({
    todo,
    event,
    note,

    getSpaceInfoOfTodoEntry,
    getSpaceInfoOfEvent,
    getSpaceInfoOfNote,
}: RecentItemProps) {


    const [workspace, setWorkspace] = useState<WorkspaceDTO | null>(null);



    // -------------- //
    // --- Helper --- //
    // -------------- //
    function determineModifierType(item?: { createdAt: string; updatedAt: string }): ModifierType {
        if (!item) {
            return "create";
        }

        if (item.createdAt === item.updatedAt) {
            return "create";
        }

        return "edited";
    }

    function formatDate(value?: string) {
        if (!value) {
            return "";
        }

        const date = new Date(value);

        // Format: dd.MM.yyyy
        const day = String(date.getDate()).padStart(2, "0");
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const year = date.getFullYear();

        return `${day}.${month}.${year}`;
    }



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    function renderDate() {
        if (todo) {
            return <>{formatDate(todo.createdAt)}</>;
        }

        if (event) {
            return <>{formatDate(event.startTime)}</>;
        }

        if (note) {
            return <>{formatDate(note.createdAt)}</>;
        }

        return null;
    }

    function renderModifierType() {
        if (modifierType === "create") {
            return "Created:";
        }

        return "Updated:";
    }

    function renderEntryName() {
        if (todo) {
            return <>{todo.name}</>;
        }

        if (event) {
            return <>{event.name}</>;
        }

        if (note) {
            return <>{note.name}</>;
        }

        return null;
    }

    function renderSpaceName() {
        if (!workspace) {
            return <>Private</>;
        }

        return <>{workspace.name}</>;
    }



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        let cancelled = false;

        async function loadWorkspace() {
            if (todo && getSpaceInfoOfTodoEntry) {
                const result = await getSpaceInfoOfTodoEntry(todo.id);

                if (!cancelled) {
                    setWorkspace(result);
                }

                return;
            }

            if (event && getSpaceInfoOfEvent) {
                const result = await getSpaceInfoOfEvent(event.id);

                if (!cancelled) {
                    setWorkspace(result);
                }

                return;
            }

            if (note && getSpaceInfoOfNote) {
                const result = await getSpaceInfoOfNote(note.id);

                if (!cancelled) {
                    setWorkspace(result);
                }

                return;
            }

            setWorkspace(null);
        }

        loadWorkspace();

        return () => {
            cancelled = true;
        };
    }, [
        todo,
        event,
        note,
        getSpaceInfoOfTodoEntry,
        getSpaceInfoOfEvent,
        getSpaceInfoOfNote
    ]);




    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // ItemType
    const item = todo ?? event ?? note;

    // ModifierType
    const modifierType = determineModifierType(item);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>


            {/* LEFT: TIMELINE */}
            <div className={c.iconArea}>
                <div className={c.icon} />
                <div className={c.timeline} />
            </div>


            {/* INFO-AREA */}
            <div className={c.infoArea}>

                {/* DATE */}
                <div className={c.date}>
                    {renderDate()}
                </div>

                {/* ACTION + NAME */}
                <div className={c.message}>
                    <div className={c.actionText}>
                        {renderModifierType()}
                    </div>
                    <div className={c.itemName}>{renderEntryName()}</div>
                </div>

                {/* WORKSPACE (overlay/tag) */}
                <div className={c.workspace}>
                    <div className={c.workspaceLabel}>Space:</div>
                    <div className={c.workspaceValue}>{renderSpaceName()}</div>
                </div>

            </div>

        </div>
    );
}