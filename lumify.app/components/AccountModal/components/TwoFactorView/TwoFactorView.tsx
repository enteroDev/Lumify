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
    intro:          styles["intro"],

    step:           styles["step"],
    stepHead:       styles["stepHead"],
    stepNum:        styles["stepNum"],
    stepTitle:      styles["stepTitle"],
    stepBody:       styles["stepBody"],

    statusRow:      styles["statusRow"],
    statusBadge:    styles["statusBadge"],
    statusOn:       styles["statusOn"],
    statusOff:      styles["statusOff"],

    qrArea:         styles["qrArea"],
    qr:             styles["qr"],
    secretLabel:    styles["secretLabel"],
    secretRow:      styles["secretRow"],
    secret:         styles["secret"],
    copyBtn:        styles["copyBtn"],
    note:           styles["note"],

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
    async function copySecret() {
        if (!setup) { return; }
        try {
            await navigator.clipboard.writeText(setup.secret);
            toast.success("Schlüssel kopiert.");
        } catch {
            toast.error("Kopieren nicht möglich – bitte manuell markieren.");
        }
    }

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

                    <p className={c.intro}>
                        Beim Login wirst du nach dem Passwort zusätzlich nach dem 6-stelligen Code
                        aus deiner Authenticator-App gefragt.
                    </p>

                    <div className={c.hint}>
                        Zum Deaktivieren gib einen aktuellen 6-stelligen Code aus deiner Authenticator-App ein:
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
                            {busy ? "..." : "2FA deaktivieren"}
                        </button>
                    </div>
                </>
            );
        }

        // --- Setup in progress: guided 3-step flow --- //
        if (setup) {
            return (
                <>
                    <p className={c.intro}>
                        Die Zwei-Faktor-Authentifizierung (2FA) sichert deinen Login zusätzlich ab: Nach dem
                        Passwort brauchst du dann einen 6-stelligen Code aus einer Authenticator-App. So
                        richtest du sie in 3 Schritten ein:
                    </p>

                    {/* Step 1 */}
                    <div className={c.step}>
                        <div className={c.stepHead}>
                            <span className={c.stepNum}>1</span>
                            <span className={c.stepTitle}>Authenticator-App installieren</span>
                        </div>
                        <div className={c.stepBody}>
                            Eine kleine, separate App, z. B. Google Authenticator, Microsoft Authenticator
                            oder Authy – am Handy oder als Desktop-/Browser-App. Eine genügt.
                        </div>
                    </div>

                    {/* Step 2 */}
                    <div className={c.step}>
                        <div className={c.stepHead}>
                            <span className={c.stepNum}>2</span>
                            <span className={c.stepTitle}>Lumify in der App hinzufügen</span>
                        </div>
                        <div className={c.stepBody}>
                            In der App „Konto hinzufügen" wählen und den QR-Code scannen – oder den Schlüssel
                            manuell eintippen.

                            <div className={c.qrArea}>
                                <img className={c.qr} src={setup.qrCodeDataUri} alt="2FA QR-Code" />
                            </div>

                            <div className={c.secretLabel}>Schlüssel (falls du nicht scannen kannst):</div>
                            <div className={c.secretRow}>
                                <code className={c.secret}>{setup.secret}</code>
                                <button type="button" className={c.copyBtn} onClick={copySecret}>Kopieren</button>
                            </div>
                            <div className={c.note}>
                                Dieser Schlüssel gehört in die Authenticator-App – nicht in das Code-Feld unten.
                            </div>
                        </div>
                    </div>

                    {/* Step 3 */}
                    <div className={c.step}>
                        <div className={c.stepHead}>
                            <span className={c.stepNum}>3</span>
                            <span className={c.stepTitle}>Code eingeben &amp; aktivieren</span>
                        </div>
                        <div className={c.stepBody}>
                            Deine App zeigt jetzt einen 6-stelligen Code an (wechselt alle 30 Sekunden).
                            Gib den aktuellen Code hier ein:

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
                                    {busy ? "..." : "2FA aktivieren"}
                                </button>
                            </div>
                        </div>
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
