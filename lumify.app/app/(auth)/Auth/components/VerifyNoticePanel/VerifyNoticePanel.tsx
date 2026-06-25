"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Services
import { AuthService } from "@/services/api/authService";
// Styles
import styles from "./VerifyNoticePanel.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    overlay:      styles["overlay"],
    card:         styles["card"],
    title:        styles["title"],
    subtitle:     styles["subtitle"],
    hint:         styles["hint"],
    actions:      styles["actions"],
    sendButton:   styles["sendButton"],
    cancelButton: styles["cancelButton"],
} as const;

export type VerifyNoticePanelProps = {
    email: string;
    onClose: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function VerifyNoticePanel({ email, onClose }: VerifyNoticePanelProps) {

    const [loading, setLoading] = useState(false);
    const [resent, setResent] = useState(false);


    async function resend() {
        if (loading) { return; }

        setLoading(true);
        try {
            await AuthService.resendVerification(email);
        } catch {
            // Backend answers generically anyway - we always show the same confirmation.
        } finally {
            setLoading(false);
            setResent(true);
        }
    }


    return (
        <div className={c.overlay} onClick={onClose}>
            <div className={c.card} onClick={(e) => e.stopPropagation()}>

                <div className={c.title}>Bestätige deine E-Mail</div>
                <div className={c.subtitle}>
                    Wir haben dir eine Bestätigungsmail an <strong>{email}</strong> geschickt.
                    Klick auf den Link darin, um dein Konto zu aktivieren – schau auch im Spam-Ordner nach.
                </div>

                {resent && (
                    <div className={c.hint}>
                        Falls ein unbestätigtes Konto existiert, ist eine neue Mail unterwegs.
                    </div>
                )}

                <div className={c.actions}>
                    <button className={c.cancelButton} onClick={resend} disabled={loading}>
                        {loading ? "Senden..." : "Mail erneut senden"}
                    </button>
                    <button className={c.sendButton} onClick={onClose}>
                        Zum Login
                    </button>
                </div>

            </div>
        </div>
    );
}
