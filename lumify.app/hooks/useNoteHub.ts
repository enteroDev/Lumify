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
import type { Folder, Note, Note_TextBlock, Note_LinkItem } from "@/models/notes";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type FolderDeletedPayload = {
    folderID: string;
};

type NoteDeletedPayload = {
    noteID: string;
};

type TextBlockDeletedPayload = {
    textBlockID: string;
};

type LinkItemDeletedPayload = {
    linkItemID: string;
};

type UseNoteHubProps = {
    isPrivate: boolean;
    workspaceID: string | null;

    onFolderCreated?: (folder: Folder) => void;
    onFolderUpdated?: (folder: Folder) => void;
    onFolderDeleted?: (data: FolderDeletedPayload) => void;

    onNoteCreated?: (note: Note) => void;
    onNoteUpdated?: (note: Note) => void;
    onNoteDeleted?: (data: NoteDeletedPayload) => void;

    onTextBlockCreated?: (textBlock: Note_TextBlock) => void;
    onTextBlockUpdated?: (textBlock: Note_TextBlock) => void;
    onTextBlockDeleted?: (data: TextBlockDeletedPayload) => void;

    onLinkItemCreated?: (linkItem: Note_LinkItem) => void;
    onLinkItemDeleted?: (data: LinkItemDeletedPayload) => void;
};



// ------------ //
// --- Hook --- //
// ------------ //
export function useNoteHub({
    isPrivate,
    workspaceID,

    onFolderCreated,
    onFolderUpdated,
    onFolderDeleted,

    onNoteCreated,
    onNoteUpdated,
    onNoteDeleted,

    onTextBlockCreated,
    onTextBlockUpdated,
    onTextBlockDeleted,

    onLinkItemCreated,
    onLinkItemDeleted,
}: UseNoteHubProps) {


    // ------------ //
    // --- Refs --- //
    // ------------ //
    const onFolderCreatedRef = useRef(onFolderCreated);
    const onFolderUpdatedRef = useRef(onFolderUpdated);
    const onFolderDeletedRef = useRef(onFolderDeleted);

    const onNoteCreatedRef = useRef(onNoteCreated);
    const onNoteUpdatedRef = useRef(onNoteUpdated);
    const onNoteDeletedRef = useRef(onNoteDeleted);

    const onTextBlockCreatedRef = useRef(onTextBlockCreated);
    const onTextBlockUpdatedRef = useRef(onTextBlockUpdated);
    const onTextBlockDeletedRef = useRef(onTextBlockDeleted);

    const onLinkItemCreatedRef = useRef(onLinkItemCreated);
    const onLinkItemDeletedRef = useRef(onLinkItemDeleted);


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        onFolderCreatedRef.current = onFolderCreated;
    }, [onFolderCreated]);

    useEffect(() => {
        onFolderUpdatedRef.current = onFolderUpdated;
    }, [onFolderUpdated]);

    useEffect(() => {
        onFolderDeletedRef.current = onFolderDeleted;
    }, [onFolderDeleted]);

    useEffect(() => {
        onNoteCreatedRef.current = onNoteCreated;
    }, [onNoteCreated]);

    useEffect(() => {
        onNoteUpdatedRef.current = onNoteUpdated;
    }, [onNoteUpdated]);

    useEffect(() => {
        onNoteDeletedRef.current = onNoteDeleted;
    }, [onNoteDeleted]);

    useEffect(() => {
        onTextBlockCreatedRef.current = onTextBlockCreated;
    }, [onTextBlockCreated]);

    useEffect(() => {
        onTextBlockUpdatedRef.current = onTextBlockUpdated;
    }, [onTextBlockUpdated]);

    useEffect(() => {
        onTextBlockDeletedRef.current = onTextBlockDeleted;
    }, [onTextBlockDeleted]);

    useEffect(() => {
        onLinkItemCreatedRef.current = onLinkItemCreated;
    }, [onLinkItemCreated]);

    useEffect(() => {
        onLinkItemDeletedRef.current = onLinkItemDeleted;
    }, [onLinkItemDeleted]);

    // Lifecycle
    useEffect(() => {
        if (isPrivate || !workspaceID) {
            return;
        }

        let cancelled = false;
        let joinedWorkspace = false;

        const connection = new HubConnectionBuilder()
            .withUrl(`${CONFIG.API.API_BASE}/hubs/notes`, {
                withCredentials: true,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        // Listeners/Triggers for changes in the Hub. -> If change, then update FE
        connection.on("FolderCreated", (folder: Folder) => {
            window.setTimeout(() => {
                onFolderCreatedRef.current?.(folder);
            }, 0);
        });

        connection.on("FolderUpdated", (folder: Folder) => {
            onFolderUpdatedRef.current?.(folder);
        });

        connection.on("FolderDeleted", (data: FolderDeletedPayload) => {
            onFolderDeletedRef.current?.(data);
        });

        connection.on("NoteCreated", (note: Note) => {
            window.setTimeout(() => {
                onNoteCreatedRef.current?.(note);
            }, 0);
        });

        connection.on("NoteUpdated", (note: Note) => {
            onNoteUpdatedRef.current?.(note);
        });

        connection.on("NoteDeleted", (data: NoteDeletedPayload) => {
            onNoteDeletedRef.current?.(data);
        });

        connection.on("TextBlockCreated", (textBlock: Note_TextBlock) => {
            onTextBlockCreatedRef.current?.(textBlock);
        });

        connection.on("TextBlockUpdated", (textBlock: Note_TextBlock) => {
            onTextBlockUpdatedRef.current?.(textBlock);
        });

        connection.on("TextBlockDeleted", (data: TextBlockDeletedPayload) => {
            onTextBlockDeletedRef.current?.(data);
        });

        connection.on("LinkItemCreated", (linkItem: Note_LinkItem) => {
            onLinkItemCreatedRef.current?.(linkItem);
        });

        connection.on("LinkItemDeleted", (data: LinkItemDeletedPayload) => {
            onLinkItemDeletedRef.current?.(data);
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

                console.error("Failed to connect NoteHub", error);
            }
        };

        void startConnection();

        return () => {
            cancelled = true;

            const cleanup = async () => {
                connection.off("FolderCreated");
                connection.off("FolderUpdated");
                connection.off("FolderDeleted");

                connection.off("NoteCreated");
                connection.off("NoteUpdated");
                connection.off("NoteDeleted");

                connection.off("TextBlockCreated");
                connection.off("TextBlockUpdated");
                connection.off("TextBlockDeleted");

                connection.off("LinkItemCreated");
                connection.off("LinkItemDeleted");

                try {
                    if (joinedWorkspace && connection.state === HubConnectionState.Connected) {
                        await connection.invoke("LeaveWorkspace", workspaceID);
                    }
                } catch (error) {
                    console.warn("Failed to leave NoteHub workspace", error);
                }

                try {
                    if (connection.state !== HubConnectionState.Disconnected) {
                        await connection.stop();
                    }
                } catch (error) {
                    console.warn("Failed to stop NoteHub", error);
                }
            };

            void cleanup();
        };
    }, [isPrivate, workspaceID]);
}