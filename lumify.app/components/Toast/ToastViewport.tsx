"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Models
import type { ToastItem } from "../../models/Toast";
// Components
import Toast from "./Toast";
// Styles
import styles from "./Toast.module.css";



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ToastViewport(props: {
    items: ToastItem[];
    onDismiss: (id: string) => void;
    onPause: (id: string) => void;
    onResume: (id: string, durationMs: number) => void;
}) {


    const { items, onDismiss, onPause, onResume } = props;

    return (
        <div className={styles.viewport} aria-live="polite" aria-relevant="additions">
            {items.map(item => (
                <Toast
                    key={item.id}
                    item={item}
                    onDismiss={() => onDismiss(item.id)}
                    onPause={() => onPause(item.id)}
                    onResume={() => onResume(item.id, item.durationMs)}
                />
            ))}
        </div>
    );
}