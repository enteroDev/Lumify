
// Type definitions for the reusable Dropdown component.


/** Optional behaviour/configuration of the dropdown. */
export type DropdownConfig = {
    /** Whether the dropdown is disabled. */
    disabled?: boolean;
};

/** Normalized entry the dropdown works with internally. */
export type DropdownEntry<TPayload = unknown> = {
    /** The entry's value (used for selection). */
    value: string;
    /** The label shown to the user. */
    text: string;
    /** Optional caller-supplied payload carried with the entry. */
    payload?: TPayload;
    /** Whether this entry is disabled. */
    disabled?: boolean;
};

/**
 * Input format the caller may pass: either a plain string or an object
 * (`text` falls back to `value`; `payload`/`disabled` are optional).
 */
export type DropdownEntryRaw<TPayload = unknown> =
    | string
    | {
        value: string;
        text?: string;
        payload?: TPayload;
        disabled?: boolean;
    };
