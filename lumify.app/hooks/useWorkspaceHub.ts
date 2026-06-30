"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef } from "react";
// SignalR
import { HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
// Config
import { CONFIG } from "@/app/config/config";
// Models
import type { WorkspaceDTO, WorkspaceMembersDTO } from "@/models/Space";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type WorkspaceDeletedPayload = {
    WorkspaceID: string;
};

type WorkspaceMemberAddedPayload = {
    WorkspaceID: string;
    UserID: string;
    Member: WorkspaceMembersDTO;
};

type WorkspaceMemberRemovedPayload = {
    WorkspaceID: string;
    UserID: string;
};

type UseWorkspaceHubProps = {
    userID: string | null;

    onWorkspaceCreated?: (workspace: WorkspaceDTO) => void;
    onWorkspaceUpdated?: (workspace: WorkspaceDTO) => void;
    onWorkspaceDeleted?: (data: WorkspaceDeletedPayload) => void;

    onWorkspaceMemberAdded?: (data: WorkspaceMemberAddedPayload) => void;
    onWorkspaceMemberRemoved?: (data: WorkspaceMemberRemovedPayload) => void;
};



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * React hook that subscribes to live workspace updates (create/update/delete and member
 * add/remove) for the current user via the workspace hub. It connects and joins the user's group,
 * then forwards each event to the matching callback. The connection is torn down on unmount or when
 * the user changes.
 * @param props The current user ID and the workspace/member callbacks.
 */
export function useWorkspaceHub({
    userID,

    onWorkspaceCreated,
    onWorkspaceUpdated,
    onWorkspaceDeleted,

    onWorkspaceMemberAdded,
    onWorkspaceMemberRemoved,
}: UseWorkspaceHubProps) {

    // ------------ //
    // --- Refs --- //
    // ------------ //
    const onWorkspaceCreatedRef = useRef(onWorkspaceCreated);
    const onWorkspaceUpdatedRef = useRef(onWorkspaceUpdated);
    const onWorkspaceDeletedRef = useRef(onWorkspaceDeleted);
    const onWorkspaceMemberAddedRef = useRef(onWorkspaceMemberAdded);
    const onWorkspaceMemberRemovedRef = useRef(onWorkspaceMemberRemoved);


    // ------------------ //
    // --- UseEffects --- //
    // ------------------ //
    useEffect(() => {
        onWorkspaceCreatedRef.current = onWorkspaceCreated;
    }, [onWorkspaceCreated]);

    useEffect(() => {
        onWorkspaceUpdatedRef.current = onWorkspaceUpdated;
    }, [onWorkspaceUpdated]);

    useEffect(() => {
        onWorkspaceDeletedRef.current = onWorkspaceDeleted;
    }, [onWorkspaceDeleted]);

    useEffect(() => {
        onWorkspaceMemberAddedRef.current = onWorkspaceMemberAdded;
    }, [onWorkspaceMemberAdded]);

    useEffect(() => {
        onWorkspaceMemberRemovedRef.current = onWorkspaceMemberRemoved;
    }, [onWorkspaceMemberRemoved]);

    // Lifecycle
    useEffect(() => {
        if (!userID) {
            return;
        }

        let cancelled = false;
        let joinedUser = false;

        const connection = new HubConnectionBuilder()
            .withUrl(`${CONFIG.API.API_BASE}/hubs/workspaces`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        // Listeners/Triggers for changes in the Hub. -> If change, then update FE
        connection.on("WorkspaceCreated", (workspace: WorkspaceDTO) => {
            onWorkspaceCreatedRef.current?.(workspace);
        });

        connection.on("WorkspaceUpdated", (workspace: WorkspaceDTO) => {
            onWorkspaceUpdatedRef.current?.(workspace);
        });

        connection.on("WorkspaceDeleted", (data: WorkspaceDeletedPayload) => {
            onWorkspaceDeletedRef.current?.(data);
        });

        connection.on("WorkspaceMemberAdded", (data: WorkspaceMemberAddedPayload) => {
            onWorkspaceMemberAddedRef.current?.(data);
        });

        connection.on("WorkspaceMemberRemoved", (data: WorkspaceMemberRemovedPayload) => {
            onWorkspaceMemberRemovedRef.current?.(data);
        });

        const startConnection = async () => {
            try {
                await connection.start();

                if (cancelled) {
                    await connection.stop();
                    return;
                }

                await connection.invoke("JoinUser", userID);
                joinedUser = true;
            } catch (error) {
                if (cancelled) { return; }

                const message = error instanceof Error ? error.message : String(error);

                if (message.includes("stopped during negotiation")) {
                    return;
                }

                console.error("Failed to connect WorkspaceHub", error);
            }
        };

        void startConnection();

        return () => {
            cancelled = true;

            const cleanup = async () => {
                connection.off("WorkspaceCreated");
                connection.off("WorkspaceUpdated");
                connection.off("WorkspaceDeleted");

                connection.off("WorkspaceMemberAdded");
                connection.off("WorkspaceMemberRemoved");

                try {
                    if (joinedUser && connection.state === HubConnectionState.Connected) {
                        await connection.invoke("LeaveUser", userID);
                    }
                } catch (error) {
                    console.warn("Failed to leave WorkspaceHub user-group", error);
                }

                try {
                    if (connection.state !== HubConnectionState.Disconnected) {
                        await connection.stop();
                    }
                } catch (error) {
                    console.warn("Failed to stop WorkspaceHub", error);
                }
            };

            void cleanup();
        };
    }, [userID]);
}