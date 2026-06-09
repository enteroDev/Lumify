"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Components
import CalendarNavigation from "./components/CalendarNavigation/CalenderNavigation";
import DayHeader from "./components/DayHeader/DayHeader";
import CalendarDay from "./components/CalendarDay/CalendarDay";
import AddEventModal from "./components/AddEventModal/AddEventModal";
import type { AddEventFormData } from "@/components/_Forms/AddEventForm/AddEventForm";
// Utils
import { buildMonthDays, needsSixthRow } from "./utils/calendarUtils";
// Models
import type { CalendarEventDTO } from "../../../../../../models/Events";
// Styles
import styles from "./Calendar.module.css";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:          styles["container"],
    navigation:         styles["navigation"],
    heading:            styles["heading"],
    body:               styles["body"],
} as const;

export type CalenderProps = {
    fullDayEvents: CalendarEventDTO[];
    timedEvents: CalendarEventDTO[];
    multiDayEvents: CalendarEventDTO[];

    onOpenEventModal: (
        selectedDate: string,

        multiDayEvents: CalendarEventDTO[],
        fullDayEvents: CalendarEventDTO[],
        timedEvents: CalendarEventDTO[],
    ) => void;

    onAddEvent: (data: AddEventFormData) => Promise<void>;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Calendar({
    multiDayEvents,
    fullDayEvents,
    timedEvents,

    onOpenEventModal,
    onAddEvent,
}: CalenderProps) {


    // Set date to current date (To always load current month on page load).
    const [view, setView] = useState({ year: 2026, month: 1 }); // 0=Jan, 1=Feb, ...

    const [showAddEventModal, setShowAddEventModal] = useState(false);
    const [selectedDate, setSelectedDate] = useState<string | undefined>(undefined);



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Move to the next month (handles year rollover aswell).
    function nextMonth() {
        setView(next => {
            if (next.month === 11) {
                return { year: next.year + 1, month: 0 };
            }
            return { year: next.year, month: next.month + 1 };
        });
    }

    // Move to the previous month (handles year rollover aswell).
    function previousMonth() {
        setView(prev => {
            if (prev.month === 0) {
                return { year: prev.year - 1, month: 11 };
            }
            return { year: prev.year, month: prev.month - 1 };
        });
    }


    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleAddEvent() {
        setSelectedDate(undefined);
        setShowAddEventModal(true);
    }

    async function handleSaveEvent(data: AddEventFormData) {
        await onAddEvent(data);
        setShowAddEventModal(false);
    }

    function handleAddEventOnDate(date: string) {
        setSelectedDate(date);
        setShowAddEventModal(true);
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const sixRows = needsSixthRow(view.year, view.month);
    const days = buildMonthDays(view.year, view.month, sixRows ? 6 : 5);

    const fullDayEventsOfMonth = fullDayEvents.filter(e => {
        const eventDate = new Date(e.startTime);

        return (
            eventDate.getFullYear() === view.year &&
            eventDate.getMonth() === view.month
        );
    });

    const timedEventsOfMonth = timedEvents.filter(e => {
        const eventDate = new Date(e.startTime);

        return (
            eventDate.getFullYear() === view.year &&
            eventDate.getMonth() === view.month
        );
    });

    const multiDayEventsOfMonth = multiDayEvents.filter(e => {
        const eventStartDate = new Date(e.startTime);
        const eventEndDate = new Date(e.endTime);

        return (
            (
                eventStartDate.getFullYear() === view.year &&
                eventStartDate.getMonth() === view.month
            ) ||
            (
                eventEndDate.getFullYear() === view.year &&
                eventEndDate.getMonth() === view.month
            )
        );
    });





    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* NAVIGATION */}
            <div className={c.navigation}>
                <CalendarNavigation
                    viewYear={view.year}
                    viewMonth={view.month}
                    previousMonth={previousMonth}
                    nextMonth={nextMonth}

                    onAddEvent={handleAddEvent}
                />
            </div>

            {/* DayHeaders */}
            <div className={c.heading}>
                <DayHeader />
            </div>

            {/* BODY - DAYS */}
            <div className={c.body}>
                {days.map((day) => (
                    <CalendarDay
                        key={day.date}
                        date={day.date}
                        isCurrentMonth={day.isCurrentMonth}

                        fullDayEvents={fullDayEventsOfMonth}
                        timedEvents={timedEventsOfMonth}
                        multiDayEvents={multiDayEventsOfMonth}

                        onOpenEventModal={onOpenEventModal}
                        onAddEvent={handleAddEventOnDate}
                    />
                ))}
            </div>


            {/* AddEventModal */}
            {showAddEventModal && (
                <AddEventModal
                    isOpen={showAddEventModal}
                    startDate={selectedDate}
                    onClose={() => setShowAddEventModal(false)}
                    onSave={handleSaveEvent}
                />
            )}

        </div>
    );
}