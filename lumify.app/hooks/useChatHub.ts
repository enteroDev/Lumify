"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef } from "react";
// SignalR
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
// Config
import { CONFIG } from "@/app/config/config";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type ChatMessageReceivedPayload = {
    id: string;
    roomID: string;
    senderID: string;
    senderName: string;
    content: string;
    createdAt: string;
    updatedAt: string;
};

type UseChatHubProps = {
    userID: string | null;
    roomID: string | null;

    onMessageReceived?: (data: ChatMessageReceivedPayload) => void;
};

/** The return value of {@link useChatHub}. */
export type UseChatHubResult = {
    /** Sends a trimmed message to the active room over the chat hub. */
    sendMessage: (content: string) => Promise<void>;
};



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * React hook that manages a SignalR connection to the chat hub for a given user and room: it
 * joins/leaves the room as `roomID` changes, invokes the `onMessageReceived` callback for incoming
 * messages, and returns a `sendMessage` function. The connection is torn down on unmount.
 * @param props The current user, the active room, and an optional incoming-message callback.
 * @returns An object exposing {@link UseChatHubResult.sendMessage}.
 */
export function useChatHub({
    userID,
    roomID,
    onMessageReceived,
}: UseChatHubProps): UseChatHubResult {


    // ------------ //
    // --- Refs --- //
    // ------------ //
    const connectionRef = useRef<HubConnection | null>(null);
    const joinedRoomIDRef = useRef<string | null>(null);
    const activeRoomIDRef = useRef<string | null>(roomID);

    const onMessageReceivedRef = useRef(onMessageReceived);


    // --------------- //
    // --- Helpers --- //
    // --------------- //

    // Ensures that both chat members are in the same chat room
    const syncRoomMembership = async () => {
        const connection = connectionRef.current;
        const activeRoomID = activeRoomIDRef.current;
        const joinedRoomID = joinedRoomIDRef.current;

        if (!connection) {
            return;
        }

        if (connection.state !== HubConnectionState.Connected) {
            return;
        }

        try {
            if (joinedRoomID === activeRoomID) {
                return;
            }

            if (joinedRoomID) {
                await connection.invoke("LeaveRoom", joinedRoomID);
                joinedRoomIDRef.current = null;
            }

            if (activeRoomID) {
                await connection.invoke("JoinRoom", activeRoomID);
                joinedRoomIDRef.current = activeRoomID;
            }
        } catch (error) {
            console.error("Failed to sync ChatHub room membership", error);
        }
    };

    // Validates message data and sends it to ChatHub
    const sendMessage = async (content: string) => {
        const connection = connectionRef.current;
        const activeRoomID = activeRoomIDRef.current;
        const trimmedContent = content.trim();

        if (!connection) {
            return;
        }

        if (!activeRoomID) {
            return;
        }

        if (!trimmedContent) {
            return;
        }

        if (connection.state !== HubConnectionState.Connected) {
            throw new Error("ChatHub is not connected.");
        }

        await connection.invoke("SendMessage", activeRoomID, trimmedContent);
    };


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        onMessageReceivedRef.current = onMessageReceived;
    }, [onMessageReceived]);

    useEffect(() => {
        activeRoomIDRef.current = roomID;
        void syncRoomMembership();
    }, [roomID]);

    // Lifecycle for Chat
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
            .configureLogging(LogLevel.Information)
            .build();

        connectionRef.current = connection;

        connection.on("MessageReceived", (data: ChatMessageReceivedPayload) => {
            onMessageReceivedRef.current?.(data);
        });

        connection.onreconnected(() => {
            void syncRoomMembership();
        });

        const startConnection = async () => {
            try {
                await connection.start();

                if (cancelled) {
                    await connection.stop();
                    return;
                }

                await syncRoomMembership();
            } catch (error) {
                if (cancelled) { return; }

                const message = error instanceof Error ? error.message : String(error);

                if (message.includes("stopped during negotiation")) {
                    return;
                }

                console.error("Failed to connect ChatHub", error);
            }
        };

        void startConnection();

        return () => {
            cancelled = true;

            const cleanup = async () => {
                try {
                    if (joinedRoomIDRef.current && connection.state === HubConnectionState.Connected) {
                        await connection.invoke("LeaveRoom", joinedRoomIDRef.current);
                        joinedRoomIDRef.current = null;
                    }
                } catch (error) {
                    console.warn("Failed to leave ChatHub room", error);
                }

                try {
                    connection.off("MessageReceived");

                    if (connection.state !== HubConnectionState.Disconnected) {
                        await connection.stop();
                    }
                } catch (error) {
                    console.warn("Failed to stop ChatHub", error);
                }

                connectionRef.current = null;
                joinedRoomIDRef.current = null;
            };

            void cleanup();
        };
    }, [userID]);


    return {
        sendMessage,
    };
}