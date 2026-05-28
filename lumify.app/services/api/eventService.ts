
import { CONFIG } from "@/app/(app)/config/config";
import { saveFetch } from "@/services/utils/auth";

import type { CalendarEventDTO, SaveEventDTO } from "@/models/Events";
import type { WorkspaceDTO } from "@/models/Space";


const API_BASE = CONFIG.API.API_BASE;

export const EventService = {

    // --- ADD --- //
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


    // --- SAVE --- //
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


    // --- DELETE --- //
    async deleteEvent(eventID: string) {

        const res = await saveFetch(`${API_BASE}/events/deleteEvent?eventID=${encodeURIComponent(eventID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Event: " + eventID);
        }

        return await res.json();
    },


    // --- GET --- //
    async getEventCountOfUser(): Promise<number> {
        const response = await saveFetch(`${API_BASE}/events/getEventCountOfUser`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch event count of user.");
        }

        return await response.json();
    },

    async getEventCountOfWorkspace(workspaceID: string): Promise<number> {
        const response = await saveFetch(`${API_BASE}/events/getEventCountOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch event count of workspace.");
        }

        return await response.json();
    },

    async getAllEventsOfUser(): Promise<CalendarEventDTO[]> {
        const res = await saveFetch(`${API_BASE}/events/getAllEventsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch events of user");
        }

        return await res.json();
    },

    async getAllEventsOfWorkspace(workspaceID: string): Promise<CalendarEventDTO[]> {
        const res = await saveFetch(`${API_BASE}/events/getAllEventsOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch events of workspace");
        }

        return await res.json();
    },

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