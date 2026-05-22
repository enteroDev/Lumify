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

            await AuthService.login(dto);

            router.push("/Dashboard");
            toast.success("Yuhu! Eingeloggt! Jetzt kanns losgehen.");

        } catch {
            toast.error("Ups... Da ging irgendetwas schief.");

        } finally {
            setLoading(false);
        }
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
        </div>
    );
}