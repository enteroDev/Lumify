"use client"

// --------------- //
// --- Imports --- //
// --------------- //


// Components
import RecentItem from "./components/RecentItem/RecentItem";
// Icons
import TodoIcon from "@/app/src/svg/todo.svg";
import EventIcon from "@/app/src/svg/calendar.svg";
import NoteIcon from "@/app/src/svg/folder.svg";
// Styles
import styles from "./Recents.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    header:             styles["header"],
    body:               styles["body"],

    group:              styles["group"],
    groupHeader:        styles["groupHeader"],
    groupContent:       styles["groupContent"],
    icon:               styles["icon"],
    title:              styles["title"],

    item:               styles["item"],
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function Recents() {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>Aktuelles</div>


            {/* BODY */}
            <div className={c.body}>

                {/* Group: Todos */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader}>
                        <div className={c.icon}><TodoIcon /></div>
                        <div className={c.title}>Todos:</div>
                    </div>
                    {/* GroupContent */}
                    <div className={c.groupContent}>
                        {/* Map todos here */}
                    </div>
                </div>


                {/* Group: Events */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader}>
                        <div className={c.icon}><EventIcon /></div>
                        <div className={c.title}>Events:</div>
                    </div>
                    {/* GroupContent */}
                    <div className={c.groupContent}>
                        {/* Map events here */}
                    </div>
                </div>

                {/* Group: Notes */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader}>
                        <div className={c.icon}><NoteIcon /></div>
                        <div className={c.title}>Notes:</div>
                    </div>
                    {/* GroupContent */}
                    <div className={c.groupContent}>
                        {/* Map notes here */}
                    </div>
                </div>

            </div>
        </div>
    );
}