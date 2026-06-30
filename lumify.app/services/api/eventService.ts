
import { CONFIG } from "@/app/config/config";
import { saveFetch } from "@/services/utils/auth";

import type { CalendarEventDTO, SaveEventDTO } from "@/models/Events";
import type { WorkspaceDTO } from "@/models/Space";


const API_BASE = CONFIG.API.API_BASE;

/**
 * Client for the calendar event endpoints (create, update, delete and various queries).
 * Methods throw on a non-OK response.
 */
export const EventService = {

    /**
     * Creates a new event, optionally inside a workspace.
     * @param data Event fields (name, description, all-day, start/end time, optional workspace).
     * @returns The created event.
     */
    async addEvent(data: {
        name: string;
        description: string | null;
        isAllDay: boolean;
        startTime: string;
        endTime: string;
        workspaceID: string | null;
    }): Promise<CalendarEventDTO> {

        const res = await saveFetch(`${API_BASE}/events/addEvent`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                name: data.name,
                description: data.description,
                isAllDay: data.isAllDay,
                startTime: data.startTime,
                endTime: data.endTime,
                workspaceID: data.workspaceID,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to add event", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to add event (${res.status}): ${text}`);
        }

        return await res.json();
    },


    /**
     * Updates an event (only provided fields are changed).
     * @param data The event ID plus the fields to change.
     * @returns The updated event.
     */
    async saveEvent(data: SaveEventDTO): Promise<CalendarEventDTO> {

        const res = await saveFetch(`${API_BASE}/events/saveEvent`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                id: data.id,
                name: data.name,
                description: data.description,
                isAllDay: data.isAllDay,
                startTime: data.startTime,
                endTime: data.endTime,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to save event", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to save event (${res.status}): ${text}`);
        }

        return await res.json();
    },


    /**
     * Deletes an event.
     * @param eventID The event to delete.
     * @returns The server confirmation payload.
     */
    async deleteEvent(eventID: string) {

        const res = await saveFetch(`${API_BASE}/events/deleteEvent?eventID=${encodeURIComponent(eventID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Event: " + eventID);
        }

        return await res.json();
    },


    /**
     * Returns the number of the current user's personal events.
     * @returns The event count.
     */
    async getEventCountOfUser(): Promise<number> {
        const response = await saveFetch(`${API_BASE}/events/getEventCountOfUser`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch event count of user.");
        }

        return await response.json();
    },

    /**
     * Returns the number of events in a workspace.
     * @param workspaceID The workspace to count events for.
     * @returns The event count.
     */
    async getEventCountOfWorkspace(workspaceID: string): Promise<number> {
        const response = await saveFetch(`${API_BASE}/events/getEventCountOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch event count of workspace.");
        }

        return await response.json();
    },

    /**
     * Returns all of the current user's personal events.
     * @returns The list of events.
     */
    async getAllEventsOfUser(): Promise<CalendarEventDTO[]> {
        const res = await saveFetch(`${API_BASE}/events/getAllEventsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch events of user");
        }

        return await res.json();
    },

    /**
     * Returns all events of a workspace.
     * @param workspaceID The workspace whose events are requested.
     * @returns The list of events.
     */
    async getAllEventsOfWorkspace(workspaceID: string): Promise<CalendarEventDTO[]> {
        const res = await saveFetch(`${API_BASE}/events/getAllEventsOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch events of workspace");
        }

        return await res.json();
    },

    /**
     * Returns the workspace an event belongs to.
     * @param eventID The event whose workspace is requested.
     * @returns The workspace.
     */
    async getSpaceInfosOfEvent(eventID: string): Promise<WorkspaceDTO> {
        const res = await saveFetch(`${API_BASE}/events/getSpaceInfosOfEvent?eventID=${encodeURIComponent(eventID)}`, { 
            method: "GET"
        });

        if (!res.ok) {
            throw new Error("Failed to fetch space infos of event: " + eventID);
        }

        return await res.json();
    },


};