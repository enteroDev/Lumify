"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useMemo, useState } from "react";
// Models
import type { ToastItem } from "../../models/Toast";
// Styles
import styles from "./Toast.module.css";



// -------------- //
// --- Styles --- //
// -------------- //
const c = {
    toast:          styles["toast"],
    accent:         styles["accent"],
    icon:           styles["icon"],
    content:        styles["content"],
    title:          styles["title"],
    message:        styles["message"],
    action:         styles["action"],
    close:          styles["close"],
    hoverGlow:      styles["hoverGlow"],
} as const;



// -------------- //
// --- Helper --- //
// -------------- //

// Maps toast variants to icons
function iconFor(variant: ToastItem["variant"]): string {
    if (variant === "success") { return "✓"; }
    if (variant === "error") { return "!"; }
    return "i";
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Toast(props: {
    item: ToastItem;
    onDismiss: () => void;
    onPause: () => void;
    onResume: () => void;
}) {

    const { item, onDismiss, onPause, onResume } = props;
    const [hovered, setHovered] = useState(false);
    const icon = useMemo(() => iconFor(item.variant), [item.variant]);

    return (
        <div
            className={`${c.toast} ${styles[item.variant]}`}
            role="status"
            onMouseEnter={() => {
                setHovered(true);
                onPause();
            }}
            onMouseLeave={() => {
                setHovered(false);
                onResume();
            }}
        >
            <div className={c.accent} />
            <div className={c.icon} aria-hidden="true">{icon}</div>

            <div className={c.content}>
                {item.title && <div className={c.title}>{item.title}</div>}
                <div className={c.message}>{item.message}</div>

                {item.action && (
                    <button
                        className={c.action}
                        onClick={() => {
                            item.action?.onClick();
                            onDismiss();
                        }}
                    >
                        {item.action.label}
                    </button>
                )}
            </div>

            <button className={c.close} aria-label="Close" onClick={onDismiss}> x </button>

            {hovered && <div className={c.hoverGlow} aria-hidden="true" />}
        </div>
    );
}