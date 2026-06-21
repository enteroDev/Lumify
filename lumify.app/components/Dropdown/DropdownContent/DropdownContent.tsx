/** CONTENT COMPONENT FOR DROPDOWN
 * This component is responsible for rendering the dropdown panel and its entries,
 * as well as handling positioning, outside clicks, and responsive behavior.
 * It uses React Portals to render the dropdown at the body level, ensuring it appears above other content.
 */

"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
// Component
import DropdownItem from "./DropDownItem/DropdownItem";
// Models
import type { DropdownEntry } from "@/models/Dropdown";
// Styles
import styles from "./DropdownContent.module.css";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
    list: styles["list"],
} as const;


export type DropdownContentProps<TPayload> = {
    open: boolean;
    setOpen: (next: boolean) => void;
    triggerEl: HTMLElement | null;
    entries: DropdownEntry<TPayload>[];
    value: string;
    selectEntry: (entry: DropdownEntry<TPayload>) => void;
    matchTriggerWidth?: boolean;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function DropdownContent<TPayload>({
    open,
    setOpen,
    triggerEl,
    entries,
    value,
    selectEntry,
    matchTriggerWidth = true,
}: DropdownContentProps<TPayload>) {

    const panelRef = useRef<HTMLDivElement | null>(null);
    const [mounted, setMounted] = useState(false);

    useEffect(() => { setMounted(true); }, []);

    const applyPosition = () => {
        const panel = panelRef.current;
        if (!panel || !triggerEl) return;

        const r = triggerEl.getBoundingClientRect();

        panel.style.left = `${r.left}px`;
        panel.style.top = `${r.bottom}px`;
        panel.style.minWidth = matchTriggerWidth ? `${r.width}px` : "";
    };


    // Apply position when dropdown opens, and when triggerEl or matchTriggerWidth changes.
    useLayoutEffect(() => {
        if (!open) return;
        applyPosition();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open, triggerEl, matchTriggerWidth, mounted]);


    // Re-apply position on window resize or scroll, to ensure the dropdown stays correctly positioned relative to the trigger.
    useEffect(() => {
        if (!open) return;

        const onResize = () => applyPosition();
        const onScroll = () => applyPosition();

        window.addEventListener("resize", onResize);
        window.addEventListener("scroll", onScroll, true);

        return () => {
            window.removeEventListener("resize", onResize);
            window.removeEventListener("scroll", onScroll, true);
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open, triggerEl, matchTriggerWidth]);

    // Close dropdown on outside click.
    useEffect(() => {
        if (!open) return;

        const onDocClick = (e: MouseEvent) => {
            const target = e.target as Node | null;
            if (!target) return;

            const isInsidePanel = panelRef.current?.contains(target) === true;
            const isInsideTrigger = triggerEl?.contains(target) === true;

            if (isInsidePanel || isInsideTrigger) return;
            setOpen(false);
        };

        document.addEventListener("click", onDocClick);
        return () => document.removeEventListener("click", onDocClick);
    }, [open, setOpen, triggerEl]);

    if (!mounted || !open) return null;

    // We create a portal, so the dropdown panel is rendered at the body level, allowing it to appear above other content and avoid overflow issues.
    return createPortal(
        <div ref={panelRef} className={c.container} data-open="true">
            <div className={c.list}>
                {entries.map(entry => (
                    <DropdownItem<TPayload>
                        key={entry.value}
                        entry={entry}
                        selected={String(value) === String(entry.value)}
                        onSelect={selectEntry}
                    />
                ))}
            </div>
        </div>,
        document.body
    );
}