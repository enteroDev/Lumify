"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Styles
import styles from "./AuthCard.module.css";
// Components
import LoginPanel from "./components/LoginPanel/LoginPanel";
import RegisterPanel from "./components/RegisterPanel/RegisterPanel";
import SwitchPanel from "./components/SwitchPanel/SwitchPanel";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
} as const;

type Mode = "login" | "register";

export type AuthCardProps = {
    onLogin: (identifier: string, password: string) => void | Promise<void>;
    loading: boolean;
    onForgotPassword: () => void;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AuthCard({
    onLogin,
    loading,
    onForgotPassword,
}:AuthCardProps) {

    const [mode, setMode] = useState<Mode>("login");


    // ---------------- //
    // --- Handlers --- //
    // ---------------- //





    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (

        <div className={c.container} data-mode={mode}>

            {/* SwitchPanel */}
            <SwitchPanel mode={mode} setMode={setMode} />

            {/* Panels: Login/Register */}
            {mode === "login"
                ? <LoginPanel
                    onLogin={onLogin}
                    loading={loading}
                    onForgotPassword={onForgotPassword}
                />
                : <RegisterPanel />}
        </div>

    );
}