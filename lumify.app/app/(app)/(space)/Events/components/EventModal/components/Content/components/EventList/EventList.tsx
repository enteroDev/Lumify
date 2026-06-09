"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// Components
import EventItem from "./components/EventItem.tsx/EventItem";
// Models
import { CalendarEventDTO } from "@/models/Events";
import { EventEntryType, EventEntryVariant } from "@/models/Events";
// Icons
import TimedEventIcon from "@/app/src/svg/clock.svg";
import MultiDayEventIcon from "@/app/src/svg/layers_2.svg";
import FullDayEventIcon from "@/app/src/svg/calendar_day.svg";
// Styles
import styles from "./EventList.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],

    group:              styles["group"],
    groupHeader:        styles["groupHeader"],
    groupIcon:          styles["groupIcon"],
    groupTitle:         styles["groupTitle"],
    groupContent:       styles["groupContent"],
} as const;

export type EventListProps = {
    // All events of the selected date (fullDay, timed, multiDay combined).
    multiDayEvents: CalendarEventDTO[];
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];

    selectedEvent: CalendarEventDTO | null;
    onSelectEvent: (event: CalendarEventDTO) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function EventList({
    multiDayEvents,
    fullDayEvents,
    timedEvents,

    selectedEvent,
    onSelectEvent,
}: EventListProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Group: MultiDay */}
            <div className={c.group}>
                <div className={c.groupHeader}>
                    <div className={c.groupIcon}>
                        <MultiDayEventIcon />
                    </div>
                    <div className={c.groupTitle}>Mehrere Tage</div>
                </div>

                <div className={c.groupContent}>
                    {multiDayEvents.map((event) => (
                        <EventItem
                            key={event.id}
                            event={event}
                            type="multiDay"
                            isSelected={selectedEvent?.id === event.id}
                            onClick={() => onSelectEvent(event)}
                        />
                    ))}
                </div>
            </div>

            {/* Group: Full-day */}
            <div className={c.group}>
                <div className={c.groupHeader}>
                    <div className={c.groupIcon}>
                        <FullDayEventIcon />
                    </div>
                    <div className={c.groupTitle}>Ganztag</div>
                </div>

                <div className={c.groupContent}>
                    {fullDayEvents.map((event) => (
                        <EventItem
                            key={event.id}
                            event={event}
                            type="fullDay"
                            isSelected={selectedEvent?.id === event.id}
                            onClick={() => onSelectEvent(event)}
                        />
                    ))}
                </div>
            </div>

            {/* Group: Timed */}
            <div className={c.group}>
                <div className={c.groupHeader}>
                    <div className={c.groupIcon}>
                        <TimedEventIcon />
                    </div>
                    <div className={c.groupTitle}>Zeitlich</div>
                </div>

                <div className={c.groupContent}>
                    {timedEvents.map((event) => (
                        <EventItem
                            key={event.id}
                            event={event}
                            type="timed"
                            isSelected={selectedEvent?.id === event.id}
                            onClick={() => onSelectEvent(event)}
                        />
                    ))}
                </div>
            </div>
        </div>
    );
}