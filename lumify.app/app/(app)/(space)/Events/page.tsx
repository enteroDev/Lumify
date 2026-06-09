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
import Overlay from "@/components/OverlayContainer/OverlayContainer";
import type { AddEventFormData } from "@/components/_Forms/AddEventForm/AddEventForm";
// Provider
import { useSpace } from "@/components/_Space/SpaceProvider";
import { useModal } from "@/components/Modal/ModalProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Services
import { EventService } from "@/services/api/eventService";
// Hooks
import { useEventHub } from "@/hooks/useEventHub";
// Models
import type { CalendarEventDTO, SaveEventDTO } from "@/models/Events";







// ----------------- //
// --- Component --- //
// ----------------- //
export default function Events() {

    const toast = useToast();
    const { isPrivate, workspaceID } = useSpace();
    const { openEventModal } = useModal();

    const [events, setEvents] = useState<CalendarEventDTO[]>([]);



    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleOpenEventModal(
        selectedDate: string,
        multiDayEvents: CalendarEventDTO[],
        fullDayEvents: CalendarEventDTO[],
        timedEvents: CalendarEventDTO[],
    ) {
        openEventModal({
            selectedDate,

            initialMultiDayEvents: multiDayEvents,
            initialFullDayEvents: fullDayEvents,
            initialTimedEvents: timedEvents,

            deleteEvent,
            saveEvent,
        });
    }



    // ------------- //
    // --- Logic --- //
    // ------------- //

    /* --- */
    /* ADD */
    /* --- */
    async function addEvent(data: AddEventFormData) {
        try {
            const createdEvent = await EventService.addEvent({
                name: data.name.trim(),
                description: data.description.trim() || null,
                isAllDay: data.isAllDay,
                startTime: data.startTime,
                endTime: data.endTime,
                workspaceID: isPrivate ? null : workspaceID,
            });

            setEvents(prev => {
                const exists = prev.some(x => x.id === createdEvent.id);
                if (exists) { return prev; }

                return [...prev, createdEvent];
            });

            toast.success("Termin wurde erstellt.");

        } catch (error) {
            console.error("Failed to add event", error);

            toast.error("Fehler beim Erstellen des Termins.");
        }
    }


    /* ------ */
    /* DELETE */
    /* ------ */
    const deleteEvent = async (eventID: string) => {
        try {
            await EventService.deleteEvent(eventID);

            setEvents(prev => prev.filter(x => x.id !== eventID));

            toast.success("Event wurde gelöscht.");

        } catch (error) {
            console.error("Failed to delete event", error);
            toast.error("Fehler beim Löschen des Events");
        }
    };


    /* ---- */
    /* SAVE */
    /* ---- */
    async function saveEvent(data: SaveEventDTO): Promise<CalendarEventDTO | null> {
        try {
            const savedEvent = await EventService.saveEvent(data);

            setEvents(prev => prev.map(event => {
                if (event.id !== savedEvent.id) { return event; }

                return savedEvent;
            }));

            toast.success("Event wurde gespeichert.");

            return savedEvent;

        } catch (error) {
            console.error("Failed to save event", error);
            toast.error("Fehler beim Speichern des Events.");

            return null;
        }
    }



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
            {/* Overlay */}
            <Overlay />

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

                    onOpenEventModal={handleOpenEventModal}
                    onAddEvent={addEvent}
                />
            </div>
        </div>
    );
}