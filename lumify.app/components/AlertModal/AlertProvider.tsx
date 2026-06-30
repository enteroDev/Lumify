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

/** Configuration for a confirmation/alert dialog. */
export type AlertOptions = {
    /** Dialog heading. */
    title: string;
    /** Body text. */
    message: string;
    /** Visual style (e.g. info/warning/danger); defaults to `"info"`. */
    status?: AlertModalStatus;
    /** Confirm button label (defaults to "Ja"). */
    confirmText?: string;
    /** Cancel button label (defaults to "Nein"). */
    cancelText?: string;
    /** Invoked when the user confirms; may be async (the dialog closes afterwards). */
    onConfirm?: () => void | Promise<void>;
    /** Invoked when the user cancels. */
    onCancel?: () => void;
};

/** The alert API exposed via {@link useAlert}. */
export type AlertContextValue = {
    /** Opens an alert dialog with the given {@link AlertOptions}. */
    showAlert: (options: AlertOptions) => void;
    /** Closes the current alert dialog without firing any callback. */
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
/**
 * Provides a single app-wide confirmation/alert dialog. Renders one `AlertModal` and exposes
 * {@link AlertContextValue} via context so any component can open it imperatively. Wrap the app
 * once; consumers call {@link useAlert}.
 * @param props Standard React children.
 */
export default function AlertProvider({
    children,
}: AlertProviderProps) {


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
/**
 * Accesses the alert {@link AlertContextValue} (`showAlert`, `closeAlert`).
 * @returns The alert API.
 * @throws Error if used outside an {@link AlertProvider}.
 */
export function useAlert() {
    const context = useContext(AlertContext);
    if (!context) {
        throw new Error("useAlert must be used within AlertProvider.");
    }

    return context;
}