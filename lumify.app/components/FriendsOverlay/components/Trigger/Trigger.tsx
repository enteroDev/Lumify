"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef, useState } from "react";
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
    bounce:                 styles["bounce"],
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


    // -------------- //
    // --- States --- //
    // -------------- //
    // Re-keys the bubble to replay the bounce animation, but only when the count actually goes up.
    const prevCountRef = useRef(notificationCount);
    const [bounceKey, setBounceKey] = useState(0);



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        if (notificationCount > prevCountRef.current) {
            setBounceKey(prev => prev + 1);
        }

        prevCountRef.current = notificationCount;
    }, [notificationCount]);



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
                    <div
                        key={bounceKey}
                        className={`${c.notificationCounter} ${bounceKey > 0 ? c.bounce : ""}`}
                    >
                        {notificationCount}
                    </div>
                    <NotificationIcon/>
                </div>
            </div>
        </button>
    );
}