
/** TODO-Collection-Entry
 * This file defines how the entries themselves are displayed.
 * Entries additionally get a style depending on their status. (Visual status info for the user - indicator and icon)
 **/

"use client";



// --------------- //
// --- Imports --- //
// --------------- //

// Models
import type { TodoStatus } from "@/models/todo";
import { TODO_STATUS } from "@/models/todo";
// Icons
import PendingIcon from "@/app/src/svg/clock.svg";
import DoneIcon from "@/app/src/svg/done.svg";
// Styles
import styles from "./CollectionEntry.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
    title: styles["title"],
    indicator: styles["indicator"],
    status: styles["status"],
    pending: styles["pending"],
    completed: styles["completed"],
} as const;

export type TodoCollectionEntryProps = {
    id: string;
    title: string;
    status: TodoStatus;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoCollectionEntry({ id, title, status }: TodoCollectionEntryProps) {
    const stateClass = status === TODO_STATUS.PENDING ? c.pending : c.completed;

    return (
        <div className={`${c.container} ${stateClass}`} data-id={id}>
            <div className={c.title}>{title}</div>

            <div className={c.status}>
                {status === TODO_STATUS.PENDING && <PendingIcon />}
                {status === TODO_STATUS.DONE && <DoneIcon />}
            </div>

            <div className={c.indicator}></div>
        </div>
    );
}
