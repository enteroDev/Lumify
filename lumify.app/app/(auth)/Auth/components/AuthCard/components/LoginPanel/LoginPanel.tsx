"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";

// Styles and Icons
import LoginIcon from "../../../../../../src/svg/login.svg";
import styles from "./LoginPanel.module.css";

// Provider

// Services




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

export type LoginPanelProps = {
    onLogin: (identifier: string, password: string) => void | Promise<void>;
    loading: boolean;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function LoginPanel({
    onLogin,
    loading
}:LoginPanelProps) {


    // -------------- //
    // --- States --- //
    // -------------- //
    const [identifier, setIdentifier] = useState("");
    const [password, setPassword] = useState("");



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    async function handleLogin() {
        await onLogin(identifier, password);
    }

    // Hitting "Enter" in the input fields should trigger the login, just like clicking the button.
    function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key === "Enter" && !loading) {
            handleLogin();
        }
    }


    // --------------- //
    // --- Effects --- //
    // --------------- //



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
                    <div className={c.title}>Login</div>

                    {/* Icon */}
                    <div className={c.icon}><LoginIcon /></div>
                </div>


                {/* BODY */}
                <div className={c.body}>

                    {/* LoginForm */}
                    <div className={c.loginForm}>

                        {/* Input: Benutzername */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Benutzername / Email</div>
                            <input
                                className={c.input}
                                autoComplete="username"
                                value={identifier}
                                onChange={(e) => setIdentifier(e.target.value)}
                            />
                        </div>

                        {/* Input: Passwort */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Passwort</div>
                            <input
                                className={c.input}
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                onKeyDown={handleKeyDown}
                            />
                        </div>

                        {/* Label/Link: ForgottPassword*/}
                        <div className={c.helpLabel}>Passwort vergessen?</div>
                    </div>


                    {/* ActionArea */}
                    <div className={c.actionArea}>
                        <button className={c.button} onClick={handleLogin}>
                            {loading ? "Bitte warten..." : "Anmelden"}
                        </button>
                    </div>
                </div>

            </div>

        </div>

    );
}