"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./InfoLine.module.css";

// Models
import type { CalendarEventDTO } from "@/models/Events";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:  styles["container"],
    pill:       styles["pill"],
    item:       styles["item"],
    label:      styles["label"],
    value:      styles["value"],
} as const;

export type InfoLineProps = {
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];
    multiDayEvents: CalendarEventDTO[];
};



// --------------- //
// --- Helpers --- //
// --------------- //
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
    fullDayEvents,
    timedEvents,
    multiDayEvents,
}: InfoLineProps) {


    // -------------- //
    // --- Helper --- //
    // -------------- //
    function getLastUpdatedEvent() {
        const allEvents = [
            ...fullDayEvents,
            ...timedEvents,
            ...multiDayEvents,
        ];

        if (allEvents.length === 0) {
            return null;
        }

        let lastUpdatedEvent = allEvents[0];

        for (const event of allEvents) {
            const currentUpdatedAt = new Date(event.updatedAt).getTime();
            const lastUpdatedAt = new Date(lastUpdatedEvent.updatedAt).getTime();

            if (currentUpdatedAt > lastUpdatedAt) {
                lastUpdatedEvent = event;
            }
        }

        return lastUpdatedEvent;
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const totalEvents = fullDayEvents.length + timedEvents.length + multiDayEvents.length;

    // Get meta for dayCell
    const lastUpdatedEvent = getLastUpdatedEvent();
    const lastUpdatedAt = lastUpdatedEvent?.updatedAt ?? null;
    const lastUpdatedBy = lastUpdatedEvent?.updatedBy ?? "-";



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Pill */}
            <div className={c.pill}>

                {/* Item: Updated at */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Letzte Änderung
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {formatDate(lastUpdatedAt)}
                    </div>
                </div>

                {/* Item: Updated by */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Bearbeitet von
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {lastUpdatedBy}
                    </div>
                </div>

                {/* Item: Updated at */}
                <div className={c.item}>
                    {/* Label */}
                    <div className={c.label}>
                        Events
                    </div>
                    {/* Value */}
                    <div className={c.value}>
                        {totalEvents}
                    </div>
                </div>
            </div>
        </div>
    );
}