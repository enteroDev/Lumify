"use client";

// --------------- //
// --- Imports --- //
// --------------- //


// Components
import SpaceSwitcher from "./components/SpaceSwitcher/SpaceSwitcher";
import FeatureNavigation from "./components/FeatureNavigation/FeatureNavigation";
// Styles
import styles from "./SpaceNavigationOverlay.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function SpaceNavigationOverlay() {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <SpaceSwitcher />
            <FeatureNavigation />
        </div>
    );
}