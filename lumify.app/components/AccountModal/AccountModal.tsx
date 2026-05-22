"use client";

// --------------- //
// --- Imports --- //
// --------------- //


// Styles
import styles from "./AccountModal.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:        styles["overlay"],
    modal:          styles["modal"],
    sidePanel:      styles["sidePanel"],
    content:        styles["content"],
    close:          styles["close"],
} as const;

type TabView = "account" | "profile" | "workspaces";


// ----------------- //
// --- Component --- //
// ----------------- //
export default function AccountModal() {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay}>
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>
                {/* Place content here */}
            </div>
        </div>
    );
}