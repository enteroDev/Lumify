/** TRIGGER COMPONENT FOR DROPDOWN
 * This component is responsible for rendering the dropdown trigger button and handling its click events.
 * It also manages the reference to the trigger element, which is used for positioning the dropdown content.
 * The trigger displays the currently selected value or a placeholder when no value is selected.
 */

"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef } from "react";
// Styles
import styles from "./DropdownTrigger.module.css";
import ChevronIcon from "../../../app/src/svg/arrow_right.svg";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
    trigger: styles["trigger"],
    label: styles["label"],
    chevron: styles["chevron"],
} as const;

export type DropdownTriggerProps = {
    open: boolean;
    setOpen: (next: boolean) => void;
    disabled: boolean;
    text: string;
    setTriggerEl: (el: HTMLElement | null) => void;
    spaceType?: "private" | "workspace";
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function DropdownTrigger({
    open,
    setOpen,
    disabled,
    text,
    setTriggerEl,
    spaceType,
}: DropdownTriggerProps) {

    const ref = useRef<HTMLButtonElement | null>(null);

    // Set triggerEl in parent on mount, and clear on unmount.
    useEffect(() => {
        setTriggerEl(ref.current);
        return () => setTriggerEl(null);
    }, [setTriggerEl]);

    // Toggle dropdown on trigger click, unless disabled.
    const onClick = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (disabled) return;

        // make sure triggerEl is available immediately on first open
        setTriggerEl(ref.current);

        setOpen(!open);
    };

    return (
        <div className={c.container}>
            <button
                ref={ref}
                type="button"
                className={c.trigger}
                onClick={onClick}
                disabled={disabled}
                data-open={open ? "true" : "false"}
                data-space={spaceType}
            >
                <span className={c.label}>{text}</span>

                <span className={c.chevron} aria-hidden="true">
                    <ChevronIcon />
                </span>
            </button>
        </div>
    );
}