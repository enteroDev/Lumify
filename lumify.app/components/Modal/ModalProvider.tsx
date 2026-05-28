"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState } from "react";

// Components
import WorkspaceModal from "@/app/(app)/Dashboard/components/Workspaces/components/WorkspaceModal/WorkspaceModal";

// Models
import type { WorkspaceVM } from "@/models/Space";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
type WorkspaceModalOptions = {
    workspace: WorkspaceVM;
    onWorkspaceSaved?: (updatedWorkspace: WorkspaceVM) => void;
    onMemberCountChanged?: (workspaceID: string, memberCount: number) => void;
};

type ModalState =
    | { type: null }
    | { type: "workspace"; options: WorkspaceModalOptions };



type ModalContextValue = {
    openWorkspaceModal: (options: WorkspaceModalOptions) => void;
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


    const value = useMemo(() => ({
        openWorkspaceModal,
        closeModal,
    }), [openWorkspaceModal, closeModal]);


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