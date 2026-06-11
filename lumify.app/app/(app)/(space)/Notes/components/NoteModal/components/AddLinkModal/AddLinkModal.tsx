"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";

// Styles & Icons
import styles from "./AddLinkModal.module.css";
import AbortIcon from "@/app/src/svg/abort.svg";
import LinkIcon from "@/app/src/svg/link.svg";
import NameIcon from "@/app/src/svg/name.svg";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type AddLinkModalProps = {
    isOpen: boolean;
    initialLabel?: string | null;
    initialUrl?: string | null;
    onClose: () => void;
    onSubmit: (label: string | null, url: string) => void | Promise<void>;
};

const c = {
    overlay:            styles["overlay"],
    modal:              styles["modal"],

    header:             styles["header"],
    title:              styles["title"],
    actionArea:         styles["actionArea"],
    closeButton:        styles["closeButton"],

    body:               styles["body"],
    content:            styles["content"],

    inputRow:           styles["inputRow"],

    iconArea:           styles["iconArea"],
    icon:               styles["icon"],

    inputArea:          styles["inputArea"],
    label:              styles["label"],
    input:              styles["input"],

    footer:             styles["footer"],
    buttonPrimary:      styles["buttonPrimary"],
    buttonSecondary:    styles["buttonSecondary"],
} as const;


// -------------- //
// --- Helper --- //
// -------------- //
function handleLabelValue(value: string): string | null {
    const trimmedValue = value.trim();

    if (!trimmedValue) {
        return null;
    }

    return trimmedValue;
}

function handleUrlValue(value: string): string {
    return value.trim();
}


// ----------------- //
// --- Component --- //
// ----------------- //
export default function AddLinkModal({
    isOpen,
    initialLabel = "",
    initialUrl = "",
    onClose,
    onSubmit,
}: AddLinkModalProps) {

    const [labelValue, setLabelValue] = useState(initialLabel ?? "");
    const [urlValue, setUrlValue] = useState(initialUrl ?? "");
    const [isSaving, setIsSaving] = useState(false);

    // Ignore submit if url is empty
    const submitDisabled = isSaving || handleUrlValue(urlValue).length === 0;


    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //

    // Handle submit action
    async function handleSubmit() {
        const trimmedUrl = handleUrlValue(urlValue);
        const trimmedLabel = handleLabelValue(labelValue);

        if (!trimmedUrl) {
            return;
        }

        try {
            setIsSaving(true);
            await onSubmit(trimmedLabel, trimmedUrl);
        } finally {
            setIsSaving(false);
        }
    }

    // Handle Enter-Key-Press. Save LinkItem
    async function handleKeyEnter(event: React.KeyboardEvent<HTMLInputElement>) {
        if (event.key !== "Enter") {
            return;
        }

        event.preventDefault();

        if (submitDisabled) {
            return;
        }

        await handleSubmit();
    }


    // --------------- //
    // --- Effects --- //
    // --------------- // 

    // EFFEKT: Sets label, urlValue and bool isSaving -- TRIGGER: isOpen, label or url changes.
    useEffect(() => {
        if (!isOpen) { return; }

        setLabelValue(initialLabel ?? "");
        setUrlValue(initialUrl ?? "");
        setIsSaving(false);
    }, [isOpen, initialLabel, initialUrl]);

    if (!isOpen) { return null; }


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={onClose}>

            {/* MODAL */}
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                {/* HEADER */}
                <div className={c.header}>

                    {/* Title */}
                    <div className={c.title}>
                        Link hinzufügen
                    </div>

                    {/* ActionArea */}
                    <div className={c.actionArea}>

                        {/* Button: Close Modal */}
                        <button
                            type="button"
                            className={c.closeButton}
                            onClick={onClose}
                            aria-label="Close add link modal"
                        >
                            <AbortIcon />
                        </button>
                    </div>
                </div>


                {/* BODY */}
                <div className={c.body}>
                    {/* Content */}
                    <div className={c.content}>
                        
                        {/* InputRow */}
                        <div className={c.inputRow}>
                            
                            {/* IconArea */}
                            <div className={c.iconArea}>
                                {/* Icon */}
                                <div className={c.icon}>
                                    <NameIcon />
                                </div>
                            </div>

                            {/* InputArea */}
                            <div className={c.inputArea}>
                                {/* Label */}
                                <div className={c.label}>
                                    Name
                                </div>
                                {/* Input: LinkName */}
                                <input
                                    type="text"
                                    className={c.input}
                                    value={labelValue}
                                    onChange={(e) => setLabelValue(e.target.value)}
                                    onKeyDown={handleKeyEnter}
                                    placeholder="Notizname..."
                                />
                            </div>
                        </div>


                        {/* InputRow */}
                        <div className={c.inputRow}>

                            {/* IconArea */}
                            <div className={c.iconArea}>
                                {/* Icon */}
                                <div className={c.icon}>
                                    <LinkIcon />
                                </div>
                            </div>

                            {/* InputArea */}
                            <div className={c.inputArea}>
                                {/* Label */}
                                <div className={c.label}>
                                    Link
                                </div>

                                {/* Input: Link */}
                                <input
                                    type="text"
                                    className={c.input}
                                    value={urlValue}
                                    onChange={(e) => setUrlValue(e.target.value)}
                                    onKeyDown={handleKeyEnter}
                                    placeholder="Url://"
                                />
                            </div>
                        </div>

                    </div>
                </div>


                {/* FOOTER */}
                <div className={c.footer}>

                    {/* Button: Abort */}
                    <button
                        type="button"
                        className={c.buttonSecondary}
                        onClick={onClose}
                        disabled={isSaving}
                    >
                        Abbrechen
                    </button>

                    {/* Button: SaveLinkItem */}
                    <button
                        type="button"
                        className={c.buttonPrimary}
                        onClick={handleSubmit}
                        disabled={submitDisabled}
                    >
                        {isSaving ? "Speichert..." : "Hinzufügen"}
                    </button>
                </div>
            </div>

        </div>
    );
}