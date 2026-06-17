// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useRef, useState } from "react";
import { createPortal } from "react-dom";
// Components
import EventEntry from "./components/EventEntry/EventEntry";
// Models
import type { CalendarEventDTO } from "@/models/Events";
// Styles
import styles from "./CalendarDay.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    compact:            styles["compact"],
    detail:             styles["detail"],

    dayLabelShort:      styles["dayLabelShort"],
    dayLabelFull:       styles["dayLabelFull"],
    eventsPreview:      styles["eventsPreview"],
    eventsDetailed:     styles["eventsDetailed"],

    notCurrentMonth:    styles["notCurrentMonth"],
    currentDay:         styles["currentDay"],

    additionalInfo:     styles["additionalInfo"],
    button:             styles["button"],
} as const;

export type CalenderDayProps = {
    date: string;
    isCurrentMonth: boolean;

    // Events of the displayed month, categorized by type (fullDay, timed, multiDay).
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];
    multiDayEvents: CalendarEventDTO[];

    onOpenEventModal: (
        selectedDate: string,
        multiDayEvents: CalendarEventDTO[],
        fullDayEvents: CalendarEventDTO[],
        timedEvents: CalendarEventDTO[],

    ) => void;

    onAddEvent: (date: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function CalendarDay({
    date,
    isCurrentMonth,

    multiDayEvents,
    fullDayEvents,
    timedEvents,

    onOpenEventModal,
    onAddEvent,
}: CalenderDayProps) {


    const containerRef = useRef<HTMLDivElement | null>(null);
    const [pos, setPos] = useState({ left: 0, top: 0 });

    const [showDetail, setShowDetail] = useState(false);



    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleEnter() {
        if (!containerRef.current) { return; }

        const rect = containerRef.current.getBoundingClientRect();

        setPos({
            left: rect.left + rect.width / 2,
            top: rect.top + rect.height / 2,
        });

        setShowDetail(true);
    }

    function handleLeave() {
        setShowDetail(false);
    }

    function handleOpenEventModal() {
        // Hide the hover detail popup before opening the modal, otherwise it stays visible
        // on top of (or behind) the modal since the cursor is still over this day cell.
        setShowDetail(false);

        onOpenEventModal(
            date,
            dayMultiDayEvents,
            dayFullDayEvents,
            dayTimedEvents
        )
    }

    function handleAddEvent() {
        onAddEvent(date);
    }



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    function renderPreviewEvents() {
        let renderedCount = 0;

        return (
            <>
                {dayFullDayEvents.map(e => {
                    if (renderedCount >= 3) { return null; }
                    renderedCount++;

                    return (
                        <EventEntry key={e.id} event={e} variant="compact" type="fullDay" />
                    );
                })}

                {dayTimedEvents.map(e => {
                    if (renderedCount >= 3) { return null; }
                    renderedCount++;

                    return (
                        <EventEntry key={e.id} event={e} variant="compact" type="timed" />
                    );
                })}

                {dayMultiDayEvents.map(e => {
                    if (renderedCount >= 3) { return null; }
                    renderedCount++;

                    return (
                        <EventEntry key={e.id} event={e} variant="compact" type="multiDay" />
                    );
                })}

                {hiddenEventsCount > 0 && (
                    <div className={c.additionalInfo}>
                        + {hiddenEventsCount} mehr
                    </div>
                )}
            </>
        );
    }

    function renderDetailedEvents() {
        return (
            <>
                {dayFullDayEvents.map(e => (
                    <EventEntry key={e.id} event={e} variant="detailed" type="fullDay" />
                ))}

                {dayTimedEvents.map(e => (
                    <EventEntry key={e.id} event={e} variant="detailed" type="timed" />
                ))}

                {dayMultiDayEvents.map(e => (
                    <EventEntry key={e.id} event={e} variant="detailed" type="multiDay" />
                ))}
            </>
        );
    }




    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // Filter events for this day.
    const dayFullDayEvents = fullDayEvents.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return date >= startDate && date <= endDate;
    });

    const dayTimedEvents = timedEvents.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return date >= startDate && date <= endDate;
    });

    const dayMultiDayEvents = multiDayEvents.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return date >= startDate && date <= endDate;
    });


    // Get year, month and day of dayCell (extract from date string and build real date object).
    const [year, month, day] = date.split("-").map(Number);
    const dateValue = new Date(year, month - 1, day);

    // get current date
    const today = new Date().toISOString().slice(0, 10);
    const isToday = date === today;

    // Generate day labels
    const dayLabelShort = day.toString().padStart(2, "0");
    const dayLabelFull = dateValue.toLocaleDateString("de-AT", {
        day: "numeric",
        month: "long",
    });

    // Count hidden events (for preview).
    const dayEventsCount = dayFullDayEvents.length + dayTimedEvents.length + dayMultiDayEvents.length;
    const hiddenEventsCount = dayEventsCount - 3;

    // Build container style (Current Month / Not Current Month / Today).
    let containerStyle = c.container;
    if (!isCurrentMonth) {
        containerStyle += " " + c.notCurrentMonth;
    }
    if (isToday) {
        containerStyle += " " + c.currentDay;
    }





    // ----------- //
    // --- JSX --- //
    // ----------- //

    return (
        <div
            className={containerStyle}
            ref={containerRef}
            onMouseEnter={handleEnter}
            onMouseLeave={handleLeave}
        >

            {/* Preview */}
            <div className={c.compact}>
                <div className={c.dayLabelShort}>{dayLabelShort}</div>

                <div className={c.eventsPreview}>
                    {renderPreviewEvents()}
                </div>
            </div>

            {/* Detailed */}
            {showDetail && createPortal(
                <div
                    className={c.detail}
                    style={pos}
                >
                    <div className={c.dayLabelFull}>{dayLabelFull}</div>

                    <div className={c.eventsDetailed}>
                        {renderDetailedEvents()}
                    </div>

                    <button className={c.button} onClick={handleOpenEventModal}>
                        Tag öffnen
                    </button>
                </div>,
                document.body
            )}
        </div>
    );
}