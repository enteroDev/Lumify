"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef } from "react";
// SignalR
import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
// Config
import { CONFIG } from "@/app/(app)/config/config";
// Models
import type { CalendarEventDTO } from "@/models/Events";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type EventDeletedPayload = {
    eventID: string;
};

type UseEventHubProps = {
    isPrivate: boolean;
    workspaceID: string | null;

    onEventCreated?: (event: CalendarEventDTO) => void;
    onEventUpdated?: (event: CalendarEventDTO) => void;
    onEventDeleted?: (data: EventDeletedPayload) => void;
};



// ------------ //
// --- Hook --- //
// ------------ //
export function useEventHub({
    isPrivate,
    workspaceID,

    onEventCreated,
    onEventUpdated,
    onEventDeleted,
}: UseEventHubProps) {

    // ------------ //
    // --- Refs --- //
    // ------------ //
    const onEventCreatedRef = useRef(onEventCreated);
    const onEventUpdatedRef = useRef(onEventUpdated);
    const onEventDeletedRef = useRef(onEventDeleted);



    // ------------------ //
    // --- UseEffects --- //
    // ------------------ //
    useEffect(() => {
        onEventCreatedRef.current = onEventCreated;
    }, [onEventCreated]);

    useEffect(() => {
        onEventUpdatedRef.current = onEventUpdated;
    }, [onEventUpdated]);

    useEffect(() => {
        onEventDeletedRef.current = onEventDeleted;
    }, [onEventDeleted]);

    // Lifecycle
    useEffect(() => {
        if (isPrivate || !workspaceID) {
            return;
        }

        let cancelled = false;
        let joinedWorkspace = false;

        const connection = new HubConnectionBuilder()
            .withUrl(`${CONFIG.API.API_BASE}/hubs/events`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        // Listeners/Triggers for changes in the Hub. -> If change, then update FE
        connection.on("EventCreated", (event: CalendarEventDTO) => {
            onEventCreatedRef.current?.(event);
        });

        connection.on("EventUpdated", (event: CalendarEventDTO) => {
            onEventUpdatedRef.current?.(event);
        });

        connection.on("EventDeleted", (data: EventDeletedPayload) => {
            onEventDeletedRef.current?.(data);
        });

        const startConnection = async () => {
            try {
                await connection.start();

                if (cancelled) {
                    await connection.stop();
                    return;
                }

                await connection.invoke("JoinWorkspace", workspaceID);
                joinedWorkspace = true;

            } catch (error) {
                if (cancelled) { return; }

                const message = error instanceof Error ? error.message : String(error);

                if (message.includes("stopped during negotiation")) {
                    return;
                }

                console.error("Failed to connect EventHub", error);
            }
        };

        void startConnection();

        return () => {
            cancelled = true;

            const cleanup = async () => {
                connection.off("EventCreated");
                connection.off("EventUpdated");
                connection.off("EventDeleted");

                try {
                    if (joinedWorkspace && connection.state === HubConnectionState.Connected) {
                        await connection.invoke("LeaveWorkspace", workspaceID);
                    }
                } catch (error) {
                    console.warn("Failed to leave EventHub workspace", error);
                }

                try {
                    if (connection.state !== HubConnectionState.Disconnected) {
                        await connection.stop();
                    }
                } catch (error) {
                    console.warn("Failed to stop EventHub", error);
                }
            };

            void cleanup();
        };
    }, [isPrivate, workspaceID]);
}