// --- CONSTS --- //
export const EVENT_STATUS = {
    PENDING: 1,
    DONE: 2,
} as const;

// Only allow what is given per constant
export type EventStatus = typeof EVENT_STATUS[keyof typeof EVENT_STATUS];



// --- Unions --- //
export type EventEntryVariant = "compact" | "detailed";
export type EventEntryType = "fullDay" | "timed" | "multiDay";



// --- DTOs --- //
export type CalendarEventDTO = {
    id: string;
    name: string;
    description?: string;

    status: EventStatus;
    isAllDay: boolean;

    startTime: string;
    endTime: string;

    createdAt: string;
    createdBy: string;
    updatedAt: string;
    updatedBy: string;
};

export type SaveEventDTO = {
    id: string;
    name?: string;
    description?: string | null;
    isAllDay?: boolean;
    startTime?: string;
    endTime?: string;
};

