"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { usePathname, useRouter  } from "next/navigation";
// Icons
import HomeIcon from "@/app/src/svg/home.svg";
import TodoIcon from "@/app/src/svg/todo.svg";
import EventIcon from "@/app/src/svg/calendar.svg";
import NoteIcon from "@/app/src/svg/folder.svg";
// Styles
import styles from "./FeatureNavigation.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],
    section:        styles["section"],
    divider:        styles["divider"],

    button:         styles["button"],
    homeButton:     styles["homeButton"],
    active:         styles["buttonActive"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FeatureNavigation() {

    const pathName = usePathname();
    const router = useRouter();


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const isActive = (path: string) => {
        return pathName.startsWith(path);
    };


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.section}>
                <div className={c.homeButton} onClick={() => router.push("/SpaceHub")}>
                    <HomeIcon />
                </div>
            </div>

            <div className={c.divider}>

            </div>

            <div className={c.section}>
                <div className={`${c.button} ${isActive("/Todos") ? c.active : ""}`} onClick={() => router.push("/Todos")}>
                    <TodoIcon />
                </div>
                <div className={`${c.button} ${isActive("/Events") ? c.active : ""}`} onClick={() => router.push("/Events")}>
                    <EventIcon />
                </div>
                <div className={`${c.button} ${isActive("/Notes") ? c.active : ""}`} onClick={() => router.push("/Notes")}>
                    <NoteIcon />
                </div>
            </div>
        </div>
    );
}