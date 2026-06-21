"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
import { useRouter } from "next/navigation";

// Components
import Heading from "./components/Heading/Heading";
import AuthCard from "./components/AuthCard/AuthCard";
import MfaPanel from "./components/MfaPanel/MfaPanel";

// Provider
import { useToast } from "@/components/Toast/ToastProvider"

// Services
import { AuthService } from "../../../services/api/authService";

// Styles
import styles from "./Auth.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Auth() {

    const router = useRouter();
    const toast = useToast();

    // -------------- //
    // --- States --- //
    // -------------- //
    const [loading, setLoading] = useState(false);
    const [mfaToken, setMfaToken] = useState<string | null>(null);
    const [mfaLoading, setMfaLoading] = useState(false);



    // ------------- //
    // --- Logic --- //
    // ------------- //
    async function login(identifier: string, password: string) {

        setLoading(true);

        try {
            const dto = {
                identifier: identifier,
                password: password,
            }

            const result = await AuthService.login(dto);

            // 2FA enabled -> don't redirect yet, ask for the 6-digit code first.
            if (result?.mfaRequired) {
                setMfaToken(result.mfaToken);
                return;
            }

            router.push("/Dashboard");
            toast.success("Yuhu! Eingeloggt! Jetzt kanns losgehen.");

        } catch {
            toast.error("Ups... Da ging irgendetwas schief.");

        } finally {
            setLoading(false);
        }
    }

    async function verifyMfa(code: string) {
        if (!mfaToken) { return; }

        setMfaLoading(true);

        try {
            await AuthService.verifyTotpLogin({ mfaToken, code });

            router.push("/Dashboard");
            toast.success("Yuhu! Eingeloggt! Jetzt kanns losgehen.");

        } catch (err) {
            toast.error(err instanceof Error ? err.message : "Code ungültig.");

        } finally {
            setMfaLoading(false);
        }
    }

    function cancelMfa() {
        setMfaToken(null);
    }



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className="scrollView">
            <div className="content">

                <div className={c.container}>
                    {/* Heading */}
                    <Heading/>

                    {/* AuthCard */}
                    < AuthCard
                        onLogin={login}
                        loading={loading}
                    />
                </div>

            </div>

            {/* 2FA code step (shown after a password login when 2FA is enabled) */}
            {mfaToken && (
                <MfaPanel
                    loading={mfaLoading}
                    onVerify={verifyMfa}
                    onCancel={cancelMfa}
                />
            )}
        </div>
    );
}