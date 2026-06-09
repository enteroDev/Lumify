"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Components
import MetaPanel from "./components/MetaPanel/MetaPanel";
import EventList from "./components/EventList/EventList";
// Models
import type { SaveEventDTO, CalendarEventDTO } from "@/models/Events";
// Styles
import styles from "./Content.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type ContentProps = {
    // All events of the selected date (fullDay, timed, multiDay combined).
    multiDayEvents: CalendarEventDTO[];
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];

    onSaveEvent: (data: SaveEventDTO) => Promise<CalendarEventDTO | null>;
    onDeleteEvent: (eventID: string) => void,
};

const c = {
    container:      styles["container"],

    eventList:      styles["eventList"],
    metaPanel:      styles["metaPanel"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Content({
    multiDayEvents,
    fullDayEvents,
    timedEvents,

    onSaveEvent,
    onDeleteEvent,
}: ContentProps) {


    const [selectedEvent, setSelectedEvent] = useState<CalendarEventDTO | null>(null);


    function handleDeleteEvent(eventID: string) {
        onDeleteEvent(eventID);

        if (selectedEvent?.id === eventID) {
            setSelectedEvent(null);
        }
    }


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* EVENT LIST */}
            <div className={c.eventList}>
                <EventList
                    fullDayEvents={fullDayEvents}
                    timedEvents={timedEvents}
                    multiDayEvents={multiDayEvents}

                    selectedEvent={selectedEvent}
                    onSelectEvent={setSelectedEvent}
                />
            </div>

            {/* META PANEL */}
            <div className={c.metaPanel}>
                <MetaPanel
                    selectedEvent={selectedEvent}

                    onSaveEvent={onSaveEvent}
                    onDeleteEvent={handleDeleteEvent}
                />
            </div>
        </div>
    );

}