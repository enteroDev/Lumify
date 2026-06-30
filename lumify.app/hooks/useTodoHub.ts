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
import type { TodoListDTO, TodoEntryDTO } from "@/models/Todos";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type TodoListDeletedPayload = {
    todoListID: string;
};

type TodoEntryDeletedPayload = {
    todoEntryID: string;
};

type UseTodoHubProps = {
    isPrivate: boolean;
    workspaceID: string | null;

    onTodoListCreated?: (todoList: TodoListDTO) => void;
    onTodoListUpdated?: (todoList: TodoListDTO) => void;
    onTodoListDeleted?: (data: TodoListDeletedPayload) => void;

    onTodoEntryCreated?: (todoEntry: TodoEntryDTO) => void;
    onTodoEntryUpdated?: (todoEntry: TodoEntryDTO) => void;
    onTodoEntryDeleted?: (data: TodoEntryDeletedPayload) => void;
};



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * React hook that subscribes to live todo-list and todo-entry updates for a workspace via the todo
 * hub. It connects and joins the workspace group (only for non-private spaces) and forwards each
 * create/update/delete event to the matching callback. The connection is torn down on unmount or
 * when the workspace changes.
 * @param props Whether the space is private, the workspace ID, and the list/entry callbacks.
 */
export function useTodoHub({
    isPrivate,
    workspaceID,

    onTodoListCreated,
    onTodoListUpdated,
    onTodoListDeleted,

    onTodoEntryCreated,
    onTodoEntryUpdated,
    onTodoEntryDeleted,
}: UseTodoHubProps) {


    // ------------ //
    // --- Refs --- //
    // ------------ //
    const onTodoListCreatedRef = useRef(onTodoListCreated);
    const onTodoListUpdatedRef = useRef(onTodoListUpdated);
    const onTodoListDeletedRef = useRef(onTodoListDeleted);

    const onTodoEntryCreatedRef = useRef(onTodoEntryCreated);
    const onTodoEntryUpdatedRef = useRef(onTodoEntryUpdated);
    const onTodoEntryDeletedRef = useRef(onTodoEntryDeleted);


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        onTodoListCreatedRef.current = onTodoListCreated;
    }, [onTodoListCreated]);

    useEffect(() => {
        onTodoListUpdatedRef.current = onTodoListUpdated;
    }, [onTodoListUpdated]);

    useEffect(() => {
        onTodoListDeletedRef.current = onTodoListDeleted;
    }, [onTodoListDeleted]);

    useEffect(() => {
        onTodoEntryCreatedRef.current = onTodoEntryCreated;
    }, [onTodoEntryCreated]);

    useEffect(() => {
        onTodoEntryUpdatedRef.current = onTodoEntryUpdated;
    }, [onTodoEntryUpdated]);

    useEffect(() => {
        onTodoEntryDeletedRef.current = onTodoEntryDeleted;
    }, [onTodoEntryDeleted]);

    // Lifecycle
    useEffect(() => {
        if (isPrivate || !workspaceID) {
            return;
        }

        let cancelled = false;
        let joinedWorkspace = false;

        const connection = new HubConnectionBuilder()
            .withUrl(`${CONFIG.API.API_BASE}/hubs/todos`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        // Listeners/Triggers for changes in the Hub. -> If change, then update FE
        connection.on("TodoListCreated", (todoList: TodoListDTO) => {
            onTodoListCreatedRef.current?.(todoList);
        });

        connection.on("TodoListUpdated", (todoList: TodoListDTO) => {
            onTodoListUpdatedRef.current?.(todoList);
        });

        connection.on("TodoListDeleted", (data: TodoListDeletedPayload) => {
            onTodoListDeletedRef.current?.(data);
        });

        connection.on("TodoEntryCreated", (todoEntry: TodoEntryDTO) => {
            onTodoEntryCreatedRef.current?.(todoEntry);
        });

        connection.on("TodoEntryUpdated", (todoEntry: TodoEntryDTO) => {
            onTodoEntryUpdatedRef.current?.(todoEntry);
        });

        connection.on("TodoEntryDeleted", (data: TodoEntryDeletedPayload) => {
            onTodoEntryDeletedRef.current?.(data);
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

                console.error("Failed to connect TodoHub", error);
            }
        };

        void startConnection();

        return () => {
            cancelled = true;

            const cleanup = async () => {
                connection.off("TodoListCreated");
                connection.off("TodoListUpdated");
                connection.off("TodoListDeleted");

                connection.off("TodoEntryCreated");
                connection.off("TodoEntryUpdated");
                connection.off("TodoEntryDeleted");

                try {
                    if (joinedWorkspace && connection.state === HubConnectionState.Connected) {
                        await connection.invoke("LeaveWorkspace", workspaceID);
                    }
                } catch (error) {
                    console.warn("Failed to leave TodoHub workspace", error);
                }

                try {
                    if (connection.state !== HubConnectionState.Disconnected) {
                        await connection.stop();
                    }
                } catch (error) {
                    console.warn("Failed to stop TodoHub", error);
                }
            };

            void cleanup();
        };
    }, [isPrivate, workspaceID]);
}