"use client"

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Components
import SidePanel from "@/components/SidePanel/SidePanel";
import Calendar from "./components/Calendar/Calendar";
import TaskList from "./components/TaskList/TaskList";
// Provider
import { useSpace } from "@/components/_Space/SpaceProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Services
import { EventService } from "@/services/api/eventService";
// Hooks
import { useEventHub } from "@/hooks/useEventHub";
// Models
import type { CalendarEventDTO } from "@/models/Events";





// ----------------- //
// --- Component --- //
// ----------------- //
export default function Events() {

    const toast = useToast();
    const { isPrivate, workspaceID } = useSpace();

    const [events, setEvents] = useState<CalendarEventDTO[]>([]);




    // --------------- //
    // --- Effects --- //
    // --------------- //

    // LOAD Events
    useEffect(() => {
        let cancelled = false;

        const load = async () => {
            try {
                let loadedEvents: CalendarEventDTO[] = [];

                if (isPrivate) {
                    loadedEvents = await EventService.getAllEventsOfUser();
                } else {
                    if (!workspaceID) {
                        setEvents([]);
                        return;
                    }

                    loadedEvents = await EventService.getAllEventsOfWorkspace(workspaceID);
                }

                if (cancelled) { return; }
                setEvents(loadedEvents);

            } catch (error) {
                if (cancelled) { return; }
                console.error("Failed to load events", error);
                toast.error("Fehler beim Laden der Termine.");
            }
        };

        void load();

        return () => { cancelled = true; };

    }, [isPrivate, workspaceID, toast]);


    // HUB-HOOK
    useEventHub({
        isPrivate,
        workspaceID,

        onEventCreated: (event) => {
            setEvents(prev => {
                const exists = prev.some(x => x.id === event.id);
                if (exists) { return prev; }

                return [...prev, event];
            });
        },

        onEventUpdated: (event) => {
            setEvents(prev => prev.map(x => {
                if (x.id !== event.id) { return x; }

                return event;
            }));
        },

        onEventDeleted: ({ eventID }) => {
            setEvents(prev => prev.filter(x => x.id !== eventID));
        },
    });




    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const fullDayEvents = events.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return e.isAllDay && startDate === endDate;
    });

    const timedEvents = events.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return !e.isAllDay && startDate === endDate;
    });

    const multiDayEvents = events.filter(e => {
        const startDate = e.startTime.slice(0, 10);
        const endDate = e.endTime.slice(0, 10);

        return startDate !== endDate;
    });




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className="scrollView">
            {/* Content */}
            <div className="content">
                <SidePanel title="Events">
                    <TaskList
                        fullDayEvents={fullDayEvents}
                        timedEvents={timedEvents}
                        multiDayEvents={multiDayEvents}
                    />
                </SidePanel>

                <Calendar
                    fullDayEvents={fullDayEvents}
                    timedEvents={timedEvents}
                    multiDayEvents={multiDayEvents}
                />
            </div>
        </div>
    );
}