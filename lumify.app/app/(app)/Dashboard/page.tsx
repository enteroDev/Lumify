"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState, useEffect } from "react";
// Services
import { AuthService } from "@/services/api/authService";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
// Styles
import styles from "./Dashboard.module.css";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {

} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Dashboard() {

    const toast = useToast();
    const [isLoggingOut, setIsLoggingOut] = useState(false);

    // ### LOGOUT ### //
    async function logout() {
        try {
            setIsLoggingOut(true);

            // Clear Cookies, close modals and redirect to login-page
            await AuthService.logout();

            window.location.href = "/Auth";
        } catch (error) {
            console.error("Logout failed:", error);
            toast.error("Fehler beim Abmelden.");
        } finally {
            setIsLoggingOut(false);
        }
    }


    return (
        <div className="content">
            <button onClick={logout}>LOGOUT</button>
        </div>
    );
}
