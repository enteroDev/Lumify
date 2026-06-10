/** DROPDOWN COMPONENT
 * A reusable dropdown component that supports both controlled and uncontrolled modes, 
 * with flexible entry formats and async-safe initialization.
 * Features:
 * - Accepts entries as either strings or objects with value/text/payload/disabled properties.
 * - Normalizes entries internally to ensure consistent handling.
 */


"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useMemo, useState } from "react";
// Modules
import DropdownTrigger from "./DropdownTrigger/DropdownTrigger";
import DropdownContent from "./DropdownContent/DropdownContent";
// Styles
import styles from "./Dropdown.module.css";
// Models
import type { DropdownConfig, DropdownEntry, DropdownEntryRaw } from "../../models/Dropdown";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
} as const;


export type DropdownProps<TPayload> = {
    entries: DropdownEntryRaw<TPayload>[];
    value?: string;
    defaultValue?: string;
    onChange?: (entry: DropdownEntry<TPayload>) => void;
    config?: DropdownConfig;
    placeholder?: string;
    matchTriggerWidth?: boolean;
    spaceType?: "private" | "workspace"; // For styling purpose, depending on type (Clear visuals for the user to identify his current spaceContext easily and avoid confusion)
};



// -------------- //
// --- Helper --- //
// -------------- //
// Ensure all entries have a label-text
function normalizeEntries<TPayload>(raw: DropdownEntryRaw<TPayload>[]): DropdownEntry<TPayload>[] {
    return raw.map(x => {
        if (typeof x === "string") {
            return { value: x, text: x } satisfies DropdownEntry<TPayload>;
        }

        return {
            value: x.value,
            text: x.text ?? x.value,
            payload: x.payload,
            disabled: x.disabled,
        } satisfies DropdownEntry<TPayload>;
    });
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Dropdown<TPayload>({
    entries: rawEntries,
    value,
    defaultValue,
    onChange,
    config,
    placeholder = "Auswählen...",
    matchTriggerWidth = true,
    spaceType,
}: DropdownProps<TPayload>) {

    // States
    const cfg: DropdownConfig = config ?? {};
    const isControlled = value != null;

    // Normalize entries only when inputEntries change.
    const entries = useMemo(() => normalizeEntries(rawEntries), [rawEntries]);

    // React state for open/close, internal value (when uncontrolled), and trigger element reference.
    const [open, setOpen] = useState(false);
    const [internalValue, setInternalValue] = useState<string>(defaultValue ?? "");
    const [triggerEl, setTriggerEl] = useState<HTMLElement | null>(null);

    // Determine the current value based on controlled vs uncontrolled mode.
    const currentValue = isControlled ? String(value ?? "") : internalValue;

    // Find the selected entry based on the current value of the dropDown.
    const selectedEntry = useMemo(() => {
        return entries.find(x => String(x.value) === String(currentValue)) ?? null;
    }, [entries, currentValue]);

    // Async-safe init for defaultValue when entries arrive later.
    useEffect(() => {
        if (isControlled) return;           // Check - Skip if controlled, as parent component manages the value.
        if (defaultValue == null) return;   // Check - No defaultValue provided, so keep placeholder.
        if (entries.length === 0) return;   // Check - No entries available, can't validate defaultValue.

        // Check - If the current internal value is already valid, do nothing.
        const currentIsValid = entries.some(x => String(x.value) === String(internalValue));
        if (currentIsValid) return;

        // Check - Validate defaultValue against the available entries.
        const defaultIsValid = entries.some(x => String(x.value) === String(defaultValue));
        if (!defaultIsValid) return;

        // Apply defaultValue once entries are available.
        Promise.resolve().then(() => setInternalValue(String(defaultValue)));
    }, [entries, defaultValue, internalValue, isControlled]);

    const setValue = (next: string) => {
        if (!isControlled) {
            setInternalValue(next);
        }
    };

    const selectEntry = (entry: DropdownEntry<TPayload>) => {
        const isDisabled = cfg.disabled === true || entry.disabled === true;
        if (isDisabled) return;

        setValue(entry.value);
        setOpen(false);

        if (onChange) {
            onChange(entry);
        }
    };

    const triggerText = selectedEntry?.text ?? placeholder;

    return (
        <div className={c.container}>
            <DropdownTrigger
                open={open}
                setOpen={setOpen}
                disabled={cfg.disabled === true}
                text={triggerText}
                setTriggerEl={setTriggerEl}
                spaceType={spaceType}
            />
            <DropdownContent<TPayload>
                open={open}
                setOpen={setOpen}
                triggerEl={triggerEl}
                entries={entries}
                value={currentValue}
                selectEntry={selectEntry}
                matchTriggerWidth={matchTriggerWidth}
            />
        </div>
    );
}