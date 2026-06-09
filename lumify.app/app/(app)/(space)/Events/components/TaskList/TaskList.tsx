"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// Models
import { CalendarEventDTO } from "@/models/Events";
// Components
import TaskGroup from "./TaskGroup/TaskGroup";
// Styles
import styles from "./TaskList.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:          styles["container"],
} as const;

export type TaskListProps = {
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];
    multiDayEvents: CalendarEventDTO[];
};

export type Task = {
    id: string;
    date: string;   // "YYYY-MM-DD"
    label: string;
};




// --------------- //
// --- Helpers --- //
// --------------- //
// Groups Tasks into date-groups. Every date with tasks will get its own group.
function groupTasksByDate(tasks: Task[]) {
    const map = new Map<string, Task[]>();

    for (const task of tasks) {
        const arr = map.get(task.date) || [];
        arr.push(task);
        map.set(task.date, arr);
    }

    return map;
}




// ----------------- //
// --- Component --- //
// ----------------- //
export default function TaskList({
    fullDayEvents,
    timedEvents,
    multiDayEvents,
}: TaskListProps) {



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const tasks: Task[] = [
        ...multiDayEvents.map(event => ({
            id: event.id,
            date: event.startTime.slice(0, 10),
            label: event.name,
        })),

        ...fullDayEvents.map(event => ({
            id: event.id,
            date: event.startTime.slice(0, 10),
            label: event.name,
        })),

        ...timedEvents.map(event => ({
            id: event.id,
            date: event.startTime.slice(0, 10),
            label: event.name,
        })),
    ];

    // Sort by date ascending
    const sorted = [...tasks].sort((a, b) => a.date.localeCompare(b.date));
    const grouped = groupTasksByDate(sorted);
    const dates = Array.from(grouped.keys());



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {dates.map(date => (
                <TaskGroup key={date} date={date} tasks={grouped.get(date)!} />
            ))}
        </div>
    );
}