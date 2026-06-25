"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
// Services
import { AuthService } from "@/services/api/authService";
// Styles
import styles from "./verify.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    container:  styles["container"],
    card:       styles["card"],
    title:      styles["title"],
    subtitle:   styles["subtitle"],
    actions:    styles["actions"],
    button:     styles["button"],
} as const;

type Status = "loading" | "success" | "error";



// --------------- //
// --- Confirm --- //
// --------------- //
function VerifyInner() {

    const router = useRouter();
    const params = useSearchParams();
    const token = params.get("token") ?? "";

    const [status, setStatus] = useState<Status>("loading");
    const [message, setMessage] = useState("");


    useEffect(() => {
        let cancelled = false;

        (async () => {
            if (!token) {
                setStatus("error");
                setMessage("Dieser Link ist unvollständig.");
                return;
            }

            try {
                await AuthService.verifyEmail(token);
                if (!cancelled) { setStatus("success"); }
            } catch (err) {
                if (!cancelled) {
                    setStatus("error");
                    setMessage(err instanceof Error ? err.message : "Dieser Link ist ungültig oder abgelaufen.");
                }
            }
        })();

        return () => { cancelled = true; };
    }, [token]);


    if (status === "loading") {
        return (
            <div className={c.card}>
                <div className={c.title}>Bestätige deine E-Mail…</div>
                <div className={c.subtitle}>Einen Moment bitte.</div>
            </div>
        );
    }

    if (status === "success") {
        return (
            <div className={c.card}>
                <div className={c.title}>✅ E-Mail bestätigt!</div>
                <div className={c.subtitle}>Dein Konto ist aktiviert. Du kannst dich jetzt einloggen.</div>
                <div className={c.actions}>
                    <button className={c.button} onClick={() => router.push("/Auth")}>Zum Login</button>
                </div>
            </div>
        );
    }

    return (
        <div className={c.card}>
            <div className={c.title}>Link ungültig</div>
            <div className={c.subtitle}>{message || "Dieser Link ist ungültig oder abgelaufen."}</div>
            <div className={c.actions}>
                <button className={c.button} onClick={() => router.push("/Auth")}>Zum Login</button>
            </div>
        </div>
    );
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function VerifyEmailPage() {
    return (
        <div className={c.container}>
            {/* useSearchParams must be inside a Suspense boundary */}
            <Suspense fallback={<div className={c.card}><div className={c.subtitle}>Lädt…</div></div>}>
                <VerifyInner />
            </Suspense>
        </div>
    );
}
