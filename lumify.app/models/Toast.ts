/** The visual style of a toast notification. */
export type ToastVariant = "success" | "info" | "error";

/** An optional action button shown inside a toast. */
export type ToastAction = {
    /** The button label. */
    label: string;
    /** Click handler invoked when the button is pressed. */
    onClick: () => void;
};

/** A single toast notification managed by the toast system. */
export type ToastItem = {
    /** Unique toast ID. */
    id: string;
    /** Optional bold title. */
    title?: string;
    /** The toast message text. */
    message: string;
    /** Visual style. */
    variant: ToastVariant;
    /** How long the toast stays visible, in milliseconds. */
    durationMs: number;
    /** Creation time (epoch milliseconds). */
    createdAt: number;
    /** Optional action button. */
    action?: ToastAction;
};
