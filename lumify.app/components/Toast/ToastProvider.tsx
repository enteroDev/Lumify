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

/** Options when creating a toast. */
export type ToastCreateOptions = {
    /** Reuse an existing toast ID to update it in place instead of stacking a new one. */
    id?: string;
    /** Optional bold title shown above the message. */
    title?: string;
    /** Auto-dismiss delay in milliseconds (defaults to 3200). */
    durationMs?: number;
    /** Optional action button (label + handler). */
    action?: ToastAction;
};

/** The toast API exposed via {@link useToast}. */
export type ToastApi = {
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
/**
 * Provides the toast notification system: holds the toast queue (max 3 visible), schedules
 * auto-dismiss timers, renders the `ToastViewport`, and exposes the {@link ToastApi} via
 * context. Wrap the app once; consumers call {@link useToast}.
 * @param props Standard React children.
 */
export default function ToastProvider({
    children,
}: { children: React.ReactNode }) {


    // -------------- //
    // --- States --- //
    // -------------- //
    const [items, setItems] = useState<ToastItem[]>([]);


    // ------------ //
    // --- Refs --- //
    // ------------ //
    const timers = useRef<Map<string, number>>(new Map());


    // ----------------- //
    // --- Callbacks --- //
    // ----------------- //
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


    // ------------ //
    // --- Memo --- //
    // ------------ //
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
/**
 * Accesses the toast {@link ToastApi} (show/success/info/error, dismiss, update, clear).
 * @returns The toast API.
 * @throws Error if used outside a {@link ToastProvider}.
 */
export function useToast(): ToastApi {
    const ctx = useContext(ToastContext);
    if (!ctx) { throw new Error("useToast must be used within ToastProvider"); }
    return ctx;
}