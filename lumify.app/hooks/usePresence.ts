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
import { CONFIG } from "@/app/config/config";
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
/**
 * React hook that keeps a SignalR connection to the chat hub open to receive live presence and
 * friendship updates for the current user. It invokes `onPresenceChanged` whenever a user's
 * presence flips and the optional `onFriendshipChanged` when a friendship changes (both ride on the
 * same chat-hub connection). The connection is torn down on unmount or when the user changes.
 * @param userID The current user, or `null` to stay disconnected.
 * @param onPresenceChanged Called with the affected user ID and their new {@link PresenceStatus}.
 * @param onFriendshipChanged Optional; called when a friendship of the current user changes.
 */
export function usePresence(
    userID: string | null,
    onPresenceChanged: PresenceChangedHandler,
    onFriendshipChanged?: () => void
) {


    const connectionRef = useRef<HubConnection | null>(null);
    const onPresenceChangedRef = useRef(onPresenceChanged);
    const onFriendshipChangedRef = useRef(onFriendshipChanged);


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        onPresenceChangedRef.current = onPresenceChanged;
    }, [onPresenceChanged]);

    useEffect(() => {
        onFriendshipChangedRef.current = onFriendshipChanged;
    }, [onFriendshipChanged]);


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

        // Friendship live-sync rides on the same ChatHub connection (see FriendshipController).
        connection.on("FriendshipChanged", () => {
            onFriendshipChangedRef.current?.();
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
            connection.off("FriendshipChanged");
            void connection.stop();
        };
    }, [userID]);
}