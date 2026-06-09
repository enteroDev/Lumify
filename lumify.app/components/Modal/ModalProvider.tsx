"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState } from "react";

// Components
import WorkspaceModal from "@/app/(app)/Dashboard/components/Workspaces/components/WorkspaceModal/WorkspaceModal";
import EventModal from "@/app/(app)/(space)/Events/components/EventModal/EventModal";

// Models
import type { WorkspaceVM } from "@/models/Space";
import type { CalendarEventDTO, SaveEventDTO } from "@/models/Events";




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

type ModalState =
    | { type: null }
    | { type: "workspace"; options: WorkspaceModalOptions }
    | { type: "event"; options: EventModalOptions };



type ModalContextValue = {
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
        openWorkspaceModal,
        openEventModal,
        closeModal,
    }), [openWorkspaceModal, openEventModal, closeModal]);


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <ModalContext.Provider value={value}>
            {children}

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