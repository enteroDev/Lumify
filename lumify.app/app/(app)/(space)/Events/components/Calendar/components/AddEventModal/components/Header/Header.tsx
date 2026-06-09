"use client";

// --------------- //
// --- Imports --- //
// --------------- //


// Styles & Icons
import styles from "./Header.module.css";
import CloseIcon from "@/app/src/svg/close.svg";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],

    titleArea:      styles["titleArea"],
    title:          styles["title"],

    actionArea:     styles["actionArea"],
    closeButton:    styles["closeButton"],
} as const;

export type HeaderProps = {
    onClose: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Header({
    onClose,
}: HeaderProps) {




    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const modalTitle = "Neuer Termin";


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Title */}
            <div className={c.title}>
                {modalTitle}
            </div>

            {/* Button: Close Modal */}
            <button
                type="button"
                className={c.closeButton}
                onClick={onClose}
            >
                <CloseIcon />
            </button>
        </div>
    );
}