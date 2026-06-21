"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Services
import { AuthService } from "@/services/api/authService";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
// Styles
import styles from "./TwoFactorView.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],
    header:         styles["header"],
    title:          styles["title"],

    body:           styles["body"],
    hint:           styles["hint"],

    statusRow:      styles["statusRow"],
    statusBadge:    styles["statusBadge"],
    statusOn:       styles["statusOn"],
    statusOff:      styles["statusOff"],

    qrArea:         styles["qrArea"],
    qr:             styles["qr"],
    secretLabel:    styles["secretLabel"],
    secret:         styles["secret"],

    codeInput:      styles["codeInput"],

    actions:        styles["actions"],
    button:         styles["button"],
    buttonGhost:    styles["buttonGhost"],
    buttonDanger:   styles["buttonDanger"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TwoFactorView() {

    const toast = useToast();

    const [loading, setLoading] = useState(true);
    const [enabled, setEnabled] = useState(false);
    const [setup, setSetup] = useState<{ secret: string; qrCodeDataUri: string } | null>(null);
    const [code, setCode] = useState("");
    const [busy, setBusy] = useState(false);



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        let cancelled = false;

        (async () => {
            try {
                const status = await AuthService.getMfaStatus();
                if (cancelled) { return; }

                if (status.enabled) {
                    setEnabled(true);
                } else {
                    // Open straight into the setup form - no pointless "activate" in-between screen.
                    const data = await AuthService.setupTotp();
                    if (!cancelled) { setSetup(data); }
                }
            } catch (err) {
                if (!cancelled) { toast.error(err instanceof Error ? err.message : "Fehler beim Laden."); }
            } finally {
                if (!cancelled) { setLoading(false); }
            }
        })();

        return () => { cancelled = true; };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    async function retrySetup() {
        setBusy(true);
        try {
            const data = await AuthService.setupTotp();
            setSetup(data);
            setCode("");
        } catch (err) {
            toast.error(err instanceof Error ? err.message : "Einrichtung fehlgeschlagen.");
        } finally {
            setBusy(false);
        }
    }

    async function confirmSetup() {
        if (code.length < 6) { return; }
        setBusy(true);
        try {
            await AuthService.confirmTotp(code.trim());
            setEnabled(true);
            setSetup(null);
            setCode("");
            toast.success("Zwei-Faktor-Authentifizierung ist aktiv.");
        } catch (err) {
            toast.error(err instanceof Error ? err.message : "Code ungültig.");
        } finally {
            setBusy(false);
        }
    }

    async function disable() {
        if (code.length < 6) { return; }
        setBusy(true);
        try {
            await AuthService.disableTotp(code.trim());
            setEnabled(false);
            setCode("");
            toast.success("Zwei-Faktor-Authentifizierung deaktiviert.");
        } catch (err) {
            toast.error(err instanceof Error ? err.message : "Deaktivieren fehlgeschlagen.");
        } finally {
            setBusy(false);
        }
    }



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    function renderBody() {
        if (loading) {
            return <div className={c.hint}>Lädt...</div>;
        }

        // --- Active: offer to disable (needs a current code) --- //
        if (enabled) {
            return (
                <>
                    <div className={c.statusRow}>
                        <span className={`${c.statusBadge} ${c.statusOn}`}>Aktiv</span>
                        <span className={c.hint}>Dein Login ist mit einem zweiten Faktor geschützt.</span>
                    </div>

                    <div className={c.hint}>
                        Zum Deaktivieren gib einen aktuellen Code aus deiner Authenticator-App ein.
                    </div>

                    <input
                        className={c.codeInput}
                        inputMode="numeric"
                        maxLength={6}
                        value={code}
                        placeholder="------"
                        onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
                    />

                    <div className={c.actions}>
                        <button className={c.buttonDanger} onClick={disable} disabled={busy || code.length < 6}>
                            {busy ? "..." : "Deaktivieren"}
                        </button>
                    </div>
                </>
            );
        }

        // --- Setup in progress: show QR + manual key + confirm --- //
        if (setup) {
            return (
                <>
                    <div className={c.hint}>
                        1. Scanne den QR-Code mit einer Authenticator-App (z. B. Microsoft/Google Authenticator).
                    </div>

                    <div className={c.qrArea}>
                        <img className={c.qr} src={setup.qrCodeDataUri} alt="2FA QR-Code" />
                    </div>

                    <div className={c.secretLabel}>Oder manuell eingeben:</div>
                    <div className={c.secret}>{setup.secret}</div>

                    <div className={c.hint}>2. Gib zur Bestätigung den angezeigten 6-stelligen Code ein.</div>

                    <input
                        className={c.codeInput}
                        inputMode="numeric"
                        maxLength={6}
                        autoFocus
                        value={code}
                        placeholder="------"
                        onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
                        onKeyDown={(e) => { if (e.key === "Enter") { void confirmSetup(); } }}
                    />

                    <div className={c.actions}>
                        <button className={c.button} onClick={confirmSetup} disabled={busy || code.length < 6}>
                            {busy ? "..." : "Aktivieren"}
                        </button>
                    </div>
                </>
            );
        }

        // --- Fallback: setup could not be created (e.g. network error) --- //
        return (
            <>
                <div className={c.hint}>Die Einrichtung konnte nicht gestartet werden.</div>

                <div className={c.actions}>
                    <button className={c.button} onClick={retrySetup} disabled={busy}>
                        {busy ? "..." : "Erneut versuchen"}
                    </button>
                </div>
            </>
        );
    }



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.header}>
                <div className={c.title}>Zwei-Faktor-Authentifizierung</div>
            </div>

            <div className={c.body}>
                {renderBody()}
            </div>
        </div>
    );
}
