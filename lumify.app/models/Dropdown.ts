// Typdefinitionen für die wiederverwendbare Dropdown-Komponente.
// (Aus der Verwendung in Dropdown.tsx / DropdownContent / DropdownItem / SpaceSwitcher rekonstruiert.)

// Optionales Verhalten/Konfiguration des Dropdowns.
export type DropdownConfig = {
    disabled?: boolean;
};

// Normalisierter Eintrag, mit dem das Dropdown intern arbeitet.
export type DropdownEntry<TPayload = unknown> = {
    value: string;
    text: string;
    payload?: TPayload;
    disabled?: boolean;
};

// Eingangsformat, das der Aufrufer übergeben darf: entweder ein einfacher String
// oder ein Objekt (text fällt auf value zurück; payload/disabled optional).
export type DropdownEntryRaw<TPayload = unknown> =
    | string
    | {
        value: string;
        text?: string;
        payload?: TPayload;
        disabled?: boolean;
    };
