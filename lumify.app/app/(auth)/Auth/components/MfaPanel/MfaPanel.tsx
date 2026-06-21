"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Styles
import styles from "./MfaPanel.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    overlay:        styles["overlay"],
    card:           styles["card"],
    title:          styles["title"],
    subtitle:       styles["subtitle"],
    input:          styles["input"],
    actions:        styles["actions"],
    verifyButton:   styles["verifyButton"],
    cancelButton:   styles["cancelButton"],
} as const;

export type MfaPanelProps = {
    loading: boolean;
    onVerify: (code: string) => void | Promise<void>;
    onCancel: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function MfaPanel({ loading, onVerify, onCancel }: MfaPanelProps) {

    const [code, setCode] = useState("");

    function submit() {
        if (loading || code.length < 6) { return; }
        void onVerify(code.trim());
    }

    return (
        <div className={c.overlay}>
            <div className={c.card}>
                <div className={c.title}>Zwei-Faktor-Bestätigung</div>
                <div className={c.subtitle}>
                    Gib den 6-stelligen Code aus deiner Authenticator-App ein.
                </div>

                <input
                    className={c.input}
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    maxLength={6}
                    autoFocus
                    value={code}
                    placeholder="------"
                    onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
                    onKeyDown={(e) => { if (e.key === "Enter") { submit(); } }}
                />

                <div className={c.actions}>
                    <button className={c.cancelButton} onClick={onCancel} disabled={loading}>
                        Abbrechen
                    </button>
                    <button className={c.verifyButton} onClick={submit} disabled={loading || code.length < 6}>
                        {loading ? "Prüfe..." : "Bestätigen"}
                    </button>
                </div>
            </div>
        </div>
    );
}
