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
export type HeaderProps = {
    selectedDate: string;
    onClose: () => void;
};

const c = {
    container:      styles["container"],

    titleArea:      styles["titleArea"],
    title:          styles["title"],

    actionArea:     styles["actionArea"],
    closeButton:    styles["closeButton"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Header({
    selectedDate,
    onClose,
}: HeaderProps) {




    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const modalTitle = "Events - " + selectedDate;


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