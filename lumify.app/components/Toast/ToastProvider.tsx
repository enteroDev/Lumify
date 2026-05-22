"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
// Models
import type { ToastItem, ToastVariant, ToastAction } from "../../models/Toast";
// Components
import ToastViewport from "./ToastViewport";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
type ToastCreateOptions = {
    id?: string;
    title?: string;
    durationMs?: number;
    action?: ToastAction;
};

type ToastApi = {
    show: (variant: ToastVariant, message: string, options?: ToastCreateOptions) => string;
    success: (message: string, options?: ToastCreateOptions) => string;
    info: (message: string, options?: ToastCreateOptions) => string;
    error: (message: string, options?: ToastCreateOptions) => string;

    dismiss: (id: string) => void;
    update: (id: string, patch: Partial<Omit<ToastItem, "id">>) => void;
    clear: () => void;
};

const ToastContext = createContext<ToastApi | null>(null);




// -------------- //
// --- Helper --- //
// -------------- //
function createId(): string {
    return Math.random().toString(16).slice(2) + Date.now().toString(16);
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ToastProvider({ children }: { children: React.ReactNode }) {
    const [items, setItems] = useState<ToastItem[]>([]);
    const timers = useRef<Map<string, number>>(new Map());

    const dismiss = useCallback((id: string) => {
        setItems(prev => prev.filter(x => x.id !== id));

        const t = timers.current.get(id);
        if (t != null) { window.clearTimeout(t); }
        timers.current.delete(id);
    }, []);

    const scheduleAutoDismiss = useCallback((id: string, durationMs: number) => {
        const old = timers.current.get(id);
        if (old != null) { window.clearTimeout(old); }

        const t = window.setTimeout(() => dismiss(id), durationMs);
        timers.current.set(id, t);
    }, [dismiss]);

    const update = useCallback((id: string, patch: Partial<Omit<ToastItem, "id">>) => {
        setItems(prev => prev.map(x => x.id === id ? { ...x, ...patch } : x));

        if (patch.durationMs != null) {
            scheduleAutoDismiss(id, patch.durationMs);
        }
    }, [scheduleAutoDismiss]);

    const show = useCallback((variant: ToastVariant, message: string, options?: ToastCreateOptions) => {
        const id = options?.id ?? createId();
        const durationMs = options?.durationMs ?? 3200;

        const next: ToastItem = {
            id,
            title: options?.title,
            message,
            variant,
            durationMs,
            createdAt: Date.now(),
            action: options?.action,
        };

        setItems(prev => {
            const exists = prev.some(x => x.id === id);

            // If same id is reused -> update instead of adding a second toast
            if (exists) {
                return prev.map(x => x.id === id ? { ...x, ...next } : x);
            }

            const stacked = [next, ...prev];
            return stacked.slice(0, 3); // keep max 3 visible
        });

        scheduleAutoDismiss(id, durationMs);
        return id;
    }, [scheduleAutoDismiss]);

    const clear = useCallback(() => {
        setItems([]);
        timers.current.forEach(t => window.clearTimeout(t));
        timers.current.clear();
    }, []);

    const api: ToastApi = useMemo(() => ({
        show,
        success: (message, options) => show("success", message, options),
        info: (message, options) => show("info", message, options),
        error: (message, options) => show("error", message, options),
        dismiss,
        update,
        clear,
    }), [show, dismiss, update, clear]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <ToastContext.Provider value={api}>
            {children}
            <ToastViewport
                items={items}
                onDismiss={dismiss}
                onPause={(id) => {
                    const t = timers.current.get(id);
                    if (t != null) { window.clearTimeout(t); }
                    timers.current.delete(id);
                }}
                onResume={(id, durationMs) => scheduleAutoDismiss(id, durationMs)}
            />
        </ToastContext.Provider>
    );
}


// ------------ //
// --- Hook --- //
// ------------ //
export function useToast(): ToastApi {
    const ctx = useContext(ToastContext);
    if (!ctx) { throw new Error("useToast must be used within ToastProvider"); }
    return ctx;
}