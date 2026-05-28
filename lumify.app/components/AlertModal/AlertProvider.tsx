"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState } from "react";

// Components
import AlertModal, { type AlertModalStatus } from "./AlertModal";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
type AlertOptions = {
    title: string;
    message: string;
    status?: AlertModalStatus;
    confirmText?: string;
    cancelText?: string;
    onConfirm?: () => void | Promise<void>;
    onCancel?: () => void;
};

type AlertContextValue = {
    showAlert: (options: AlertOptions) => void;
    closeAlert: () => void;
};

type AlertState = AlertOptions & {
    isOpen: boolean;
};

type AlertProviderProps = {
    children: React.ReactNode;
};

const AlertContext = createContext<AlertContextValue | null>(null);


// ----------------- //
// --- Component --- //
// ----------------- //
export default function AlertProvider({ children }: AlertProviderProps) {
    const [alertState, setAlertState] = useState<AlertState | null>(null);

    // -------------- //
    // --- States --- //
    // -------------- //
    const closeAlert = useCallback(() => {
        setAlertState(null);
    }, []);

    const showAlert = useCallback((options: AlertOptions) => {
        setAlertState({
            ...options,
            isOpen: true,
        });
    }, []);



    // --------------- //
    // --- Handler --- //
    // --------------- //
    const handleConfirm = useCallback(async () => {
        if (!alertState) { return; }

        await alertState.onConfirm?.();
        setAlertState(null);
    }, [alertState]);

    const handleCancel = useCallback(() => {
        if (alertState?.onCancel) {
            alertState.onCancel();
        }

        setAlertState(null);
    }, [alertState]);



    // ------------ //
    // --- Memo --- //
    // ------------ //   
    const value = useMemo(() => ({
        showAlert,
        closeAlert,
    }), [showAlert, closeAlert]);



    // ----------- //
    // --- JSX --- //
    // ----------- //  
    return (
        <AlertContext.Provider value={value}>
            {children}

            <AlertModal
                isOpen={alertState?.isOpen ?? false}
                title={alertState?.title ?? ""}
                message={alertState?.message ?? ""}
                status={alertState?.status ?? "info"}
                confirmText={alertState?.confirmText ?? "Ja"}
                cancelText={alertState?.cancelText ?? "Nein"}
                onConfirm={handleConfirm}
                onCancel={handleCancel}
            />
        </AlertContext.Provider>
    );
}


// ------------ //
// --- Hook --- //
// ------------ //
export function useAlert() {
    const context = useContext(AlertContext);
    if (!context) {
        throw new Error("useAlert must be used within AlertProvider.");
    }

    return context;
}