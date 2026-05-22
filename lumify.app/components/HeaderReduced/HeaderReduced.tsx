// --------------- //
// --- Imports --- //
// --------------- //

// React / Next
import Image from "next/image";
// Components
import PathBar from "./PathBar/PathBar";
import ActionBar from "./ActionBar/ActionBar";
import InfoBar from "./InfoBar/InfoBar";
import AccountToggle from "./AccountToggle/AccountToggle";
// Styles
import styles from "./HeaderReduced.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    logoArea:       styles["logoArea"],
    logo:           styles["logo"],
    titleArea:      styles["titleArea"],
    title:          styles["title"],
    navArea:        styles["navArea"],
    actionArea:     styles["actionArea"],
    pathArea:       styles["pathArea"],
    infoArea:       styles["infoArea"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function HeaderReduced() {
    return (
        <div className={c.container}>
            <div className={c.logoArea}>
                <Image className={c.logo} src="/src/logo.png" alt="Lumify Logo" width={32} height={34} />
            </div>
            <div className={c.titleArea}>
                <h1 className={c.title}>Lumify</h1>
            </div>
            <div className={c.pathArea}>
                <PathBar />
            </div>
            <div className={c.navArea}></div>
            <div className={c.actionArea}>
                <ActionBar />
            </div>
            <div className={c.infoArea}>
                <InfoBar />
                <AccountToggle />
            </div>
        </div>
    );
}