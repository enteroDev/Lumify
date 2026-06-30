// --- CONSTS --- //

/** Allowed event status values (1 = pending, 2 = done). */
export const EVENT_STATUS = {
    PENDING: 1,
    DONE: 2,
} as const;



// --- UNIONS --- //

/** Union of the valid {@link EVENT_STATUS} values. */
export type EventStatus = typeof EVENT_STATUS[keyof typeof EVENT_STATUS];

/** How an event entry is rendered: condensed or with full details. */
export type EventEntryVariant = "compact" | "detailed";
/** The visual kind of an event entry in the calendar. */
export type EventEntryType = "fullDay" | "timed" | "multiDay";



// --- DTOs --- //

/** A calendar event as delivered to the client. Mirrors the backend `EventResponse`. */
export type CalendarEventDTO = {
    /** Event ID. */
    id: string;
    /** Event title. */
    name: string;
    /** Optional description. */
    description?: string;

    /** Event status (see {@link EventStatus}). */
    status: EventStatus;
    /** Whether the event spans the whole day. */
    isAllDay: boolean;

    /** Start date/time (ISO-8601 string). */
    startTime: string;
    /** End date/time (ISO-8601 string). */
    endTime: string;

    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Display name of the creator. */
    createdBy: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
    /** Display name of the last editor. */
    updatedBy: string;
};

/** Request payload for updating an event (only provided fields are changed). */
export type SaveEventDTO = {
    /** The event to update. */
    id: string;
    /** New title, if changing. */
    name?: string;
    /** New description, if changing. */
    description?: string | null;
    /** New all-day flag, if changing. */
    isAllDay?: boolean;
    /** New start date/time, if changing. */
    startTime?: string;
    /** New end date/time, if changing. */
    endTime?: string;
};
