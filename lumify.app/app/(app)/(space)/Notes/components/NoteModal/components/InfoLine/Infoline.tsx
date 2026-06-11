"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./InfoLine.module.css";

// Models
import type { Note } from "../../../../../../../../models/notes";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type InfoLineProps = {
    note: Note;
    createdByName?: string;
};

const c = {
    container:  styles["container"],
    pill:       styles["pill"],
    item:       styles["item"],
    label:      styles["label"],
    value:      styles["value"],
} as const;



// ----------------- //
// --- Helpers --- //
// ----------------- //
const formatDate = (value?: string | null) => {
    if (!value) { return "-"; }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) { return "-"; }

    return date.toLocaleDateString("de-AT", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    });
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function InfoLine({
    note,
    createdByName
}: InfoLineProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Pill */}
            <div className={c.pill}>

                {/* Item: Created by */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Created by
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {createdByName ?? note.ownerName}
                    </div>
                </div>

                {/* Item: Created at */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Created at
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {formatDate(note.createdAt)}
                    </div>
                </div>

                {/* Item: Updated at */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Updated at
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {formatDate(note.updatedAt)}
                    </div>
                </div>
            </div>
        </div>
    );
}