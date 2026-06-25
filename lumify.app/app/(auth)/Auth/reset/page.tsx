"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
// Services
import { AuthService } from "@/services/api/authService";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
// Styles
import styles from "./reset.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    container:  styles["container"],
    card:       styles["card"],
    title:      styles["title"],
    subtitle:   styles["subtitle"],
    inputWrap:  styles["inputWrap"],
    label:      styles["label"],
    input:      styles["input"],
    error:      styles["error"],
    actions:    styles["actions"],
    button:     styles["button"],
} as const;



// ----------------- //
// --- Form part --- //
// ----------------- //
function ResetForm() {

    const router = useRouter();
    const toast = useToast();
    const params = useSearchParams();
    const token = params.get("token") ?? "";

    const [password, setPassword] = useState("");
    const [confirm, setConfirm] = useState("");
    const [loading, setLoading] = useState(false);

    const tooShort = password.length > 0 && password.length < 8;
    const mismatch = confirm.length > 0 && password !== confirm;
    const canSubmit = token.length > 0 && password.length >= 8 && password === confirm && !loading;


    async function submit() {
        if (!canSubmit) { return; }

        setLoading(true);
        try {
            await AuthService.resetPassword(token, password);
            toast.success("Passwort geändert. Du kannst dich jetzt einloggen.");
            router.push("/Auth");
        } catch (err) {
            toast.error(err instanceof Error ? err.message : "Zurücksetzen fehlgeschlagen.");
        } finally {
            setLoading(false);
        }
    }


    // No token in the URL -> the link is broken/incomplete.
    if (!token) {
        return (
            <div className={c.card}>
                <div className={c.title}>Ungültiger Link</div>
                <div className={c.subtitle}>
                    Dieser Link ist unvollständig oder abgelaufen. Bitte fordere einen neuen Link an.
                </div>
                <div className={c.actions}>
                    <button className={c.button} onClick={() => router.push("/Auth")}>Zum Login</button>
                </div>
            </div>
        );
    }


    return (
        <div className={c.card}>
            <div className={c.title}>Neues Passwort setzen</div>
            <div className={c.subtitle}>Wähle ein neues Passwort für dein Konto.</div>

            <div className={c.inputWrap}>
                <div className={c.label}>Neues Passwort</div>
                <input
                    className={c.input}
                    type="password"
                    autoComplete="new-password"
                    autoFocus
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
            </div>

            <div className={c.inputWrap}>
                <div className={c.label}>Passwort wiederholen</div>
                <input
                    className={c.input}
                    type="password"
                    autoComplete="new-password"
                    value={confirm}
                    onChange={(e) => setConfirm(e.target.value)}
                    onKeyDown={(e) => { if (e.key === "Enter") { submit(); } }}
                />
            </div>

            {tooShort && <div className={c.error}>Mindestens 8 Zeichen.</div>}
            {mismatch && <div className={c.error}>Die Passwörter stimmen nicht überein.</div>}

            <div className={c.actions}>
                <button className={c.button} onClick={submit} disabled={!canSubmit}>
                    {loading ? "Speichert..." : "Passwort speichern"}
                </button>
            </div>
        </div>
    );
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ResetPasswordPage() {
    return (
        <div className={c.container}>
            {/* useSearchParams must be inside a Suspense boundary */}
            <Suspense fallback={<div className={c.card}><div className={c.subtitle}>Lädt...</div></div>}>
                <ResetForm />
            </Suspense>
        </div>
    );
}
