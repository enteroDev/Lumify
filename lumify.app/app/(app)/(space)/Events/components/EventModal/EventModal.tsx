"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Components
import Header from "./components/Header/Header";
import InfoLine from "./components/InfoLine/InfoLine";
import Content from "./components/Content/Content";
// Models
import type { SaveEventDTO, CalendarEventDTO } from "@/models/Events";
// Styles
import styles from "./EventModal.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:            styles["overlay"],
    modal:              styles["modal"],

    header:             styles["header"],
    actionLine:         styles["actionLine"],
    infoLine:           styles["infoLine"],
    body:               styles["body"],

    button:             styles["button"],
} as const;


export type EventModalProps = {
    isOpen: boolean;
    selectedDate: string;

    initialMultiDayEvents: CalendarEventDTO[];
    initialFullDayEvents: CalendarEventDTO[];
    initialTimedEvents: CalendarEventDTO[];

    onClose: () => void;

    saveEvent: (data: SaveEventDTO) => Promise<CalendarEventDTO | null>;
    deleteEvent: (eventID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function EventModal({
    isOpen,
    selectedDate,

    initialMultiDayEvents,
    initialFullDayEvents,
    initialTimedEvents,

    onClose,
    saveEvent,
    deleteEvent,
}: EventModalProps) {

    // -------------- //
    // --- States --- //
    // -------------- //
    const [multiDayEvents, setMultiDayEvents] = useState<CalendarEventDTO[]>(initialMultiDayEvents);
    const [fullDayEvents, setFullDayEvents] = useState<CalendarEventDTO[]>(initialFullDayEvents);
    const [timedEvents, setTimedEvents] = useState<CalendarEventDTO[]>(initialTimedEvents);



    // --------------- //
    // --- Helper --- //
    // --------------- //

    function eventBelongsToSelectedDate(event: CalendarEventDTO) {
        const startDate = event.startTime.slice(0, 10);
        const endDate = event.endTime.slice(0, 10);

        if (startDate === selectedDate) { return true; }
        if (endDate === selectedDate) { return true; }

        return startDate < selectedDate && endDate > selectedDate;
    }

    function replaceOrRemove(prev: CalendarEventDTO[], savedEvent: CalendarEventDTO) {
        const belongs = eventBelongsToSelectedDate(savedEvent);

        // If doesnt belong to current displayed day, it gets removed from the list.
        if (!belongs) {
            return prev.filter(x => x.id !== savedEvent.id); 
        }

        // Find event by ID and replace it with the saved version, keep the rest as-is
        return prev.map(x => {
            if (x.id !== savedEvent.id) { return x; }
            return savedEvent;
        });
    }



    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleDeleteEvent(eventID: string) {
        deleteEvent(eventID);

        setMultiDayEvents(prev => prev.filter(x => x.id !== eventID));
        setFullDayEvents(prev => prev.filter(x => x.id !== eventID));
        setTimedEvents(prev => prev.filter(x => x.id !== eventID));
    }

    async function handleSaveEvent(data: SaveEventDTO) {
        const savedEvent = await saveEvent(data);

        if (!savedEvent) { return null; }

        setMultiDayEvents(prev => replaceOrRemove(prev, savedEvent));
        setFullDayEvents(prev => replaceOrRemove(prev, savedEvent));
        setTimedEvents(prev => replaceOrRemove(prev, savedEvent));

        return savedEvent;
    }


    if (!isOpen) { return null; }



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={onClose}>

            {/* Modal */}
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                {/* HEADER */}
                <div className={c.header}>
                    <Header
                        selectedDate={selectedDate}
                        onClose={onClose}
                    />
                </div>

                {/* INFOLINE */}
                <div className={c.infoLine}>
                    <InfoLine
                        fullDayEvents={fullDayEvents}
                        timedEvents={timedEvents}
                        multiDayEvents={multiDayEvents}
                    />
                </div>

                {/* ACTIONLINE */}
                <div className={c.actionLine}>
                    <button type="button" className={c.button}>+ Neuer Termin</button>
                </div>

                {/* BODY */}
                <div className={c.body}>
                    <Content
                        fullDayEvents={fullDayEvents}
                        timedEvents={timedEvents}
                        multiDayEvents={multiDayEvents}

                        onSaveEvent={handleSaveEvent}
                        onDeleteEvent={handleDeleteEvent}
                    />
                </div>

            </div>
        </div>
    );
}