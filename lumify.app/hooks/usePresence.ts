// Hook for presence. Currently shared with ChatHub. -> ChatHub
// ChatHub additionally provides presence-related functionality.


"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React & Microsoft
import { useEffect, useRef } from "react";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
// Utils
import { CONFIG } from "@/app/(app)/config/config";
// Models
import { PresenceStatus } from "@/models/User";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type PresenceChangedHandler = (
    changedUserID: string,
    presenceStatus: PresenceStatus
) => void;



// ------------ //
// --- Hook --- //
// ------------ //
export function usePresence(
    userID: string | null,
    onPresenceChanged:
    PresenceChangedHandler
) {


    const connectionRef = useRef<HubConnection | null>(null);
    const onPresenceChangedRef = useRef(onPresenceChanged);


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        onPresenceChangedRef.current = onPresenceChanged;
    }, [onPresenceChanged]);


    useEffect(() => {
        if (!userID) {
            return;
        }

        let cancelled = false;

        const connection = new HubConnectionBuilder()
            .withUrl(`${CONFIG.API.API_BASE}/hubs/chat`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .build();

        connection.on("PresenceChanged", (changedUserID: string, presenceStatus: PresenceStatus) => {
            onPresenceChangedRef.current(changedUserID, presenceStatus);
        });

        connectionRef.current = connection;

        const start = async () => {
            try {
                await connection.start();

                if (cancelled) {
                    await connection.stop();
                    return;
                }
            } catch (err) {
                if (cancelled) { return; }

                const message = err instanceof Error ? err.message : String(err);

                if (message.includes("stopped during negotiation")) {
                    return;
                }

                console.error("Presence connection failed", err);
            }
        };

        void start();

        return () => {
            cancelled = true;
            connection.off("PresenceChanged");
            void connection.stop();
        };
    }, [userID]);
}