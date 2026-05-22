export type ToastVariant = "success" | "info" | "error";

export type ToastAction = {
    label: string;
    onClick: () => void;
};

export type ToastItem = {
    id: string;
    title?: string;
    message: string;
    variant: ToastVariant;
    durationMs: number;
    createdAt: number;
    action?: ToastAction;
};