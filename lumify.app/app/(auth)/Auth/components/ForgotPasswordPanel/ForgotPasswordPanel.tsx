"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Services
import { AuthService } from "@/services/api/authService";
// Styles
import styles from "./ForgotPasswordPanel.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    overlay:      styles["overlay"],
    card:         styles["card"],
    title:        styles["title"],
    subtitle:     styles["subtitle"],
    input:        styles["input"],
    actions:      styles["actions"],
    sendButton:   styles["sendButton"],
    cancelButton: styles["cancelButton"],
} as const;

export type ForgotPasswordPanelProps = {
    onClose: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ForgotPasswordPanel({ onClose }: ForgotPasswordPanelProps) {

    const [identifier, setIdentifier] = useState("");
    const [loading, setLoading] = useState(false);
    const [sent, setSent] = useState(false);


    async function submit() {
        if (loading || identifier.trim().length === 0) { return; }

        setLoading(true);
        try {
            await AuthService.requestPasswordReset(identifier.trim());
        } catch {
            // Intentionally ignored: we always show the same generic confirmation,
            // so we never reveal whether an account exists.
        } finally {
            setLoading(false);
            setSent(true);
        }
    }


    return (
        <div className={c.overlay} onClick={onClose}>
            <div className={c.card} onClick={(e) => e.stopPropagation()}>

                {sent ? (
                    <>
                        <div className={c.title}>E-Mail unterwegs</div>
                        <div className={c.subtitle}>
                            Falls diese Adresse bei uns registriert ist, haben wir dir gerade einen Link zum
                            Zurücksetzen geschickt. Schau auch im Spam-Ordner nach.
                        </div>

                        <div className={c.actions}>
                            <button className={c.sendButton} onClick={onClose}>
                                Zurück zum Login
                            </button>
                        </div>
                    </>
                ) : (
                    <>
                        <div className={c.title}>Passwort vergessen</div>
                        <div className={c.subtitle}>
                            Gib deine E-Mail oder deinen Benutzernamen ein – wir senden dir einen Link zum
                            Zurücksetzen.
                        </div>

                        <input
                            className={c.input}
                            autoFocus
                            value={identifier}
                            placeholder="E-Mail oder Benutzername"
                            onChange={(e) => setIdentifier(e.target.value)}
                            onKeyDown={(e) => { if (e.key === "Enter") { submit(); } }}
                        />

                        <div className={c.actions}>
                            <button className={c.cancelButton} onClick={onClose} disabled={loading}>
                                Abbrechen
                            </button>
                            <button className={c.sendButton} onClick={submit} disabled={loading || identifier.trim().length === 0}>
                                {loading ? "Senden..." : "Link senden"}
                            </button>
                        </div>
                    </>
                )}

            </div>
        </div>
    );
}
