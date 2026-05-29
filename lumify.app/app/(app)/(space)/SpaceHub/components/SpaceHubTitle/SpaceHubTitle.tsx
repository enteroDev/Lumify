"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import Link from "next/link";
// Provider
import { useSpace } from "@/components/_Space/SpaceProvider";
// Styles
import styles from "./SpaceHubTitle.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:          styles["container"],

    heading:            styles["heading"],
    hubName:            styles["spaceTag"]
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function SpaceHubTitle() {

    const { currentSpace } = useSpace();



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const isPrivate = currentSpace.type === "private";

    const spaceTagStyle =
    c.hubName + " " +
    (isPrivate ? styles["spaceTagPrivate"] : styles["spaceTagPublic"]);

    const spaceName =
    currentSpace.type === "private"
        ? "[ Privater Space ]"
        : "[ " + currentSpace.name + " ]";


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.heading}>SpaceHub</div>
            <div className={spaceTagStyle}>{spaceName}</div>
        </div>
    );
}