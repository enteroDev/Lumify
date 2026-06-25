"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";

// Styles and Icons
import RegisterIcon from "../../../../../../src/svg/account_add.svg";
import styles from "./RegisterPanel.module.css";

// Models

// Provider
import { useToast } from "@/components/Toast/ToastProvider";

// Services
import { AuthService } from "../../../../../../../services/api/authService";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    header:         styles["header"],
    title:          styles["title"],
    icon:           styles["icon"],
    body:           styles["body"],
    content:        styles["content"],
    loginForm:      styles["loginForm"],
    actionArea:     styles["actionArea"],
    button:         styles["button"],
    inputWrap:      styles["inputWrap"],
    inputGroup:     styles["inputGroup"],
    label:          styles["label"],
    input:          styles["input"],
    helpLabel:      styles["helpLabel"],
} as const;

export type RegisterPanelProps = {
    onRegistered: (email: string) => void;
}




// -------------- //
// --- Helper --- //
// -------------- //
export const validatePassword = (password: string): boolean => {
    const minLength = 8;
    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumber = /[0-9]/.test(password);

    return password.length >= minLength && hasUpperCase && hasLowerCase && hasNumber;
};

const validateRegisterRequest = (password: string, passwordRepeat: string, toast: any): boolean => {

    // 1) VALIDATE - All required inputs filled?
    const inputs = document.querySelectorAll('input[required]');
    let allFieldsFilled = true;

    inputs.forEach((input) => {
        const el = input as HTMLInputElement;
        if (!el.checkValidity()) {
            allFieldsFilled = false;
        }
    });

    if (!allFieldsFilled) {
        toast.error("Bitte fülle alle markierten Felder aus! ✨");
        return false;
    }


    // 2) VALIDATE - Password strength
    if (!validatePassword(password)) {
        toast.error("Passwort zu schwach! (8+ Zeichen, A-Z, a-z, 0-9)");
        return false;
    }

    // 3) VALIDATE - Password match ?
    if (password !== passwordRepeat) {
        toast.error("Die Passwörter stimmen nicht überein!");
        return false;
    }

    return true;
};






// ----------------- //
// --- Component --- //
// ----------------- //
export default function RegisterPanel({ onRegistered }: RegisterPanelProps) {

    const toast = useToast();

    // -------------- //
    // --- States --- //
    // -------------- //
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [email, setEmail] = useState("");
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [passwordRepeat, setPasswordRepeat] = useState("");

    const [loading, setLoading] = useState(false);


    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    async function handleRegister() {

        // 1) Validate
        if (password !== passwordRepeat) {
            toast.error("Passwörter stimmen nicht überein!");
            return;
        }

        if (!validateRegisterRequest(password, passwordRepeat, toast)) {
            toast.error("Bitte fülle alle markierten Felder korrekt aus. ✨");
            return;
        }

        // 2) Loading State
        setLoading(true);

        // 3) API Call
        try {
            await AuthService.register({
                firstName,
                lastName,
                email,
                username,
                password,
                avatarBase64: null
            });

            // No auto-login: the account must confirm its e-mail first -> show the "check your inbox" notice.
            onRegistered(email);

        } catch {
            toast.error("Registrierung fehlgeschlagen.");
        } finally {
            setLoading(false);
        }
    }


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (

        <div className={c.container}>

            {/* ContentContainer */}
            <div className={c.content}>


                {/* HEADER */}
                <div className={c.header}>

                    {/* Title */}
                    <div className={c.title}>Registrieren</div>

                    {/* Icon */}
                    <div className={c.icon}><RegisterIcon /></div>
                </div>


                {/* BODY */}
                <div className={c.body}>

                    {/* LoginForm */}
                    <div className={c.loginForm}>

                        {/* InputGroup */}
                        <div className={c.inputGroup}>

                            {/* Input: Vorname */}
                            <div className={c.inputWrap}>
                                <div className={c.label}>Vorname</div>
                                <input
                                    className={c.input}
                                    value={firstName}
                                    onChange={(e) => setFirstName(e.target.value)}
                                    required
                                />
                            </div>

                            {/* Input: Nachname */}
                            <div className={c.inputWrap}>
                                <div className={c.label}>Nachname</div>
                                <input
                                    className={c.input}
                                    value={lastName}
                                    onChange={(e) => setLastName(e.target.value)}
                                    required
                                />
                            </div>
                        </div>

                        {/* Input: Email */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Email</div>
                            <input
                                className={c.input}
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                required
                            />
                        </div>

                        {/* Input: Benutzername */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Benutzername</div>
                            <input
                                className={c.input}
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                required
                            />
                        </div>

                        {/* InputGroup */}
                        <div className={c.inputGroup}>

                            {/* Input: Passwort */}
                            <div className={c.inputWrap}>
                                <div className={c.label}>Passwort</div>
                                <input
                                    className={c.input}
                                    type="password"
                                    placeholder="z. B. Sonne2026"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    required
                                />
                            </div>

                            {/* Input: Passwort wiederholen */}
                            <div className={c.inputWrap}>
                                <div className={c.label}>Passwort wiederholen</div>
                                <input
                                    className={c.input}
                                    type="password"
                                    value={passwordRepeat}
                                    onChange={(e) => setPasswordRepeat(e.target.value)}
                                    required
                                />
                            </div>
                        </div>

                        {/* Password rules hint (same style as the "forgot password" label) */}
                        <div className={c.helpLabel}>
                            Min. 8 Zeichen, Groß- und Kleinbuchstaben, Zahl.
                        </div>
                    </div>


                    {/* ActionArea */}
                    <div className={c.actionArea}>
                        <button className={c.button} onClick={handleRegister} disabled={loading}>
                            {loading ? "Bitte warten..." : "Registrieren"}
                        </button>
                    </div>
                </div>

            </div>

        </div>

    );
}