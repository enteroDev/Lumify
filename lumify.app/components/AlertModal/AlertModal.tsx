"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Icons
import DeleteIcon from "../../app/src/svg/trash.svg";
import WarningIcon from "../../app/src/svg/warnung.svg";
import AbortIcon from "../../app/src/svg/abort.svg";
// Styles
import styles from "./AlertModal.module.css";




// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:            styles["overlay"],
    modal:              styles["modal"],
    header:             styles["header"],
    headerInfo:         styles["headerInfo"],
    headerWarning:      styles["headerWarning"],
    headerDelete:       styles["headerDelete"],
    iconWrap:           styles["iconWrap"],
    title:              styles["title"],
    body:               styles["body"],
    footer:             styles["footer"],
    button:             styles["button"],
    buttonPrimary:      styles["buttonPrimary"],
    buttonSecondary:    styles["buttonSecondary"],
} as const;

export type AlertModalProps = {
    isOpen: boolean;
    title: string;
    message: string;
    status?: AlertModalStatus;
    confirmText?: string;
    cancelText?: string;
    onConfirm: () => void;
    onCancel: () => void;
};

export type AlertModalStatus = "info" | "warning" | "delete";



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AlertModal({
    isOpen,
    title,
    message,
    status = "info",
    confirmText = "Ja",
    cancelText = "Nein",
    onConfirm,
    onCancel
}: AlertModalProps) {



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Render Header-Icon depending on alert-type
    function renderHeaderIcon(status: AlertModalStatus) {
        if (status === "warning") {
            return (
                <WarningIcon />
            );
        }

        if (status === "delete") {
            return (
                <DeleteIcon />
            );
        }

        // Info-Icon
        return (
            <svg width="26" height="26" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="2" />
                <path d="M12 10V16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                <circle cx="12" cy="7" r="1" fill="currentColor" />
            </svg>
        );
    }

    function renderConfirmIcon(status: AlertModalStatus) {
        if (status === "delete") {
            return <DeleteIcon />;
        }

        // Check-Icon
        return (
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M5 12L10 17L19 8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
        );
    }

    function renderFooterButtons() {
        if (isInfo) {
            return (
                <button
                    type="button"
                    className={`${c.button} ${c.buttonPrimary}`}
                    onClick={onConfirm}
                >
                    {renderConfirmIcon(status)}
                    {confirmText}
                </button>
            );
        }

        return (
            <>
                <button
                    type="button"
                    className={`${c.button} ${c.buttonSecondary}`}
                    onClick={onCancel}
                >
                    <AbortIcon />
                    {cancelText}
                </button>

                <button
                    type="button"
                    className={`${c.button} ${c.buttonPrimary}`}
                    onClick={onConfirm}
                >
                    {renderConfirmIcon(status)}
                    {confirmText}
                </button>
            </>
        );
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //   
    if (!isOpen) { return null; }

    const isInfo = status === "info";

    const headerClass =
        status === "delete"
            ? `${c.header} ${c.headerDelete}`
            : status === "warning"
            ? `${c.header} ${c.headerWarning}`
            : `${c.header} ${c.headerInfo}`;



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={onCancel}>
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>
                <div className={headerClass}>
                    <div className={c.iconWrap}>
                        {renderHeaderIcon(status)}
                    </div>

                    <div className={c.title}>
                        {title}
                    </div>
                </div>

                <div className={c.body}>
                    {message}
                </div>

                <div className={c.footer}>
                    {renderFooterButtons()}
                </div>
            </div>
        </div>
    );
}