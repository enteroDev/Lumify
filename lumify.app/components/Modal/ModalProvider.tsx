"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState } from "react";

// Components
import WorkspaceModal from "@/app/(app)/Dashboard/components/Workspaces/components/WorkspaceModal/WorkspaceModal";
import EventModal from "@/app/(app)/(space)/Events/components/EventModal/EventModal";
import NoteModal from "@/app/(app)/(space)/Notes/components/NoteModal/NoteModal";

// Models
import type { WorkspaceVM } from "@/models/Space";
import type { CalendarEventDTO, SaveEventDTO } from "@/models/Events";
import type { Note, Note_TextBlock, Note_LinkItem } from "@/models/notes";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
type WorkspaceModalOptions = {
    workspace: WorkspaceVM;
    onWorkspaceSaved?: (updatedWorkspace: WorkspaceVM) => void;
    onMemberCountChanged?: (workspaceID: string, memberCount: number) => void;
};

type EventModalOptions = {
    selectedDate: string;

    initialMultiDayEvents: CalendarEventDTO[];
    initialFullDayEvents: CalendarEventDTO[];
    initialTimedEvents: CalendarEventDTO[];

    saveEvent: (data: SaveEventDTO) => Promise<CalendarEventDTO | null>;
    deleteEvent: (eventID: string) => void;
};

type NoteModalOptions = {
    note: Note;
    textBlocks: Note_TextBlock[];
    linkItems: Note_LinkItem[];
};




type ModalState =
    | { type: null }
    | { type: "workspace"; options: WorkspaceModalOptions }
    | { type: "event"; options: EventModalOptions }
    | { type: "note"; options: NoteModalOptions };



type ModalContextValue = {
    openNoteModal: (options: NoteModalOptions) => void;
    openWorkspaceModal: (options: WorkspaceModalOptions) => void;
    openEventModal: (options: EventModalOptions) => void;
    closeModal: () => void;
};


type ModalProviderProps = {
    children: React.ReactNode;
};

const ModalContext = createContext<ModalContextValue | null>(null);



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ModalProvider({ children }: ModalProviderProps) {

    // -------------- //
    // --- States --- //
    // -------------- //
    const [modalState, setModalState] = useState<ModalState>({
        type: null,
    });

    const closeModal = useCallback(() => {
        setModalState({
            type: null,
        });
    }, []);

    const openNoteModal = useCallback((options: NoteModalOptions) => {
        setModalState({
            type: "note",
            options,
        });
    }, []);

    const openWorkspaceModal = useCallback((options: WorkspaceModalOptions) => {
        setModalState({
            type: "workspace",
            options,
        });
    }, []);

    const openEventModal = useCallback((options: EventModalOptions) => {
        setModalState({
            type: "event",
            options,
        });
    }, []);


    const value = useMemo(() => ({
        openNoteModal,
        openWorkspaceModal,
        openEventModal,
        closeModal,
    }), [openNoteModal, openWorkspaceModal, openEventModal, closeModal]);


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <ModalContext.Provider value={value}>
            {children}

            {/* NoteModal */}
            {modalState.type === "note" && (
                <NoteModal
                    key={modalState.options.note.id}

                    initialNote={modalState.options.note}
                    initialTextBlocks={modalState.options.textBlocks}
                    initialLinkItems={modalState.options.linkItems}

                    isOpen={true}
                    onClose={closeModal}
                />
            )}

            {/* WorkspaceModal */}
            {modalState.type === "workspace" && (
                <WorkspaceModal
                    key={modalState.options.workspace.id}

                    workspace={modalState.options.workspace}

                    onClose={closeModal}
                    onWorkspaceSaved={modalState.options.onWorkspaceSaved}
                    onMemberCountChanged={modalState.options.onMemberCountChanged}
                />
            )}

            {/* EventModal */}
            {modalState.type === "event" && (
                <EventModal
                    key={modalState.options.selectedDate}

                    selectedDate={modalState.options.selectedDate}

                    initialMultiDayEvents={modalState.options.initialMultiDayEvents}
                    initialFullDayEvents={modalState.options.initialFullDayEvents}
                    initialTimedEvents={modalState.options.initialTimedEvents}

                    isOpen={true}
                    onClose={closeModal}

                    saveEvent={modalState.options.saveEvent}
                    deleteEvent={modalState.options.deleteEvent}
                />
            )}
        </ModalContext.Provider>
    );
}


// ------------ //
// --- Hook --- //
// ------------ //
export function useModal() {
    const context = useContext(ModalContext);
    if (!context) {
        throw new Error("useModal must be used within ModalProvider.");
    }

    return context;
}