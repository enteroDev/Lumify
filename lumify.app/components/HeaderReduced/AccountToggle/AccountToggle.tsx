"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./AccountToggle.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container: styles["container"],
    avatar: styles["avatar"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AccountToggle() {

    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div
                className={c.avatar}
                style={{
                    backgroundImage: "url('/images/Avatar.png')",
                }}
            />
        </div>
    );
}