"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./Tooltip.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
type TooltipProps = {
    text: string;
    x: number;
    y: number;
    isVisible: boolean;
};

const c = {
    container:      styles["container"],
    visible:        styles["visible"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Tooltip({
    text,
    x,
    y,
    isVisible,
}: TooltipProps) {
    return (
        <div
            className={`${c.container} ${isVisible ? c.visible : ""}`}
            style={{
                left: `${x}px`,
                top: `${y}px`,
            }}
        >
            {text}
        </div>
    );
}