"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Icons
import FriendsIcon from "@/app/src/svg/group.svg";
import NotificationIcon from "@/app/src/svg/bell.svg";
// Styles
import styles from "./Trigger.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:              styles["container"],

    titleArea:              styles["titleArea"],
    icon:                   styles["icon"],
    title:                  styles["title"],

    notificationArea:       styles["notificationArea"],
    notificationIcon:       styles["notificationIcon"],
    notificationCounter:    styles["notificationCounter"],
} as const;

type TriggerProps = {
    onToggle: () => void;
    notificationCount: number;
};




// ----------------- //
// --- Component --- //
// ----------------- //
export default function Trigger({
    onToggle,
    notificationCount,
}: TriggerProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <button type="button" className={c.container} onClick={onToggle}>

            {/* TitleArea */}
            <div className={c.titleArea}>
                <div className={c.icon}><FriendsIcon/></div>
                <div className={c.title}>Freunde</div>
            </div>

            {/* NotificationArea */}
            <div className={c.notificationArea}>
                <div className={c.notificationIcon}>
                    <div className={c.notificationCounter}>{notificationCount}</div>
                    <NotificationIcon/>
                </div>
            </div>
        </button>
    );
}