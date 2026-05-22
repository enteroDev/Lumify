"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import {useAccountModal} from "@/components/AccountModal/AccountModalProvider";
// Styles
import styles from "./AccountToggle.module.css";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:              styles["container"],
    avatar:                 styles["avatar"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AcountToggle() {


    const { showModal, avatarUrl } = useAccountModal();


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container} onClick={showModal}>
            <div
                className={c.avatar}
                style={{
                    backgroundImage: avatarUrl ? `url(${avatarUrl})` : undefined,
                }}
            />
        </div>
    );
}