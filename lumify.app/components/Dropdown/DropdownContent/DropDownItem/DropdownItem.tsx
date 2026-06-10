/** ITEM COMPONENT FOR DROPDOWN
 * This component is responsible for rendering a single dropdown item.
 */

"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import type { DropdownEntry } from "@/models/Dropdown";
// Styles
import styles from "./DropdownItem.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
    item: styles["item"],
} as const;

export type DropdownItemProps<TPayload> = {
    entry: DropdownEntry<TPayload>;
    selected: boolean;
    onSelect: (entry: DropdownEntry<TPayload>) => void;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function DropdownItem<TPayload>({
    entry,
    selected,
    onSelect,
}: DropdownItemProps<TPayload>) {

    const isDisabled = entry.disabled === true;

    const onClick = () => {
        if (isDisabled) return;
        onSelect(entry);
    };

    return (
        <div className={c.container}>
            <div
                className={c.item}
                role="option"
                aria-selected={selected}
                data-selected={selected ? "true" : "false"}
                data-disabled={isDisabled ? "true" : "false"}
                onClick={onClick}
            >
                {entry.text}
            </div>
        </div>
    );
}