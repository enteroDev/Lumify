"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useMemo, useState } from "react";
// Styles
import styles from "./SwitchPanel.module.css";
// Icons
import TodoIcon from "@/app/src/svg/todo.svg";
import EventIcon from "@/app/src/svg/calendar.svg";
import NoteIcon from "@/app/src/svg/folder.svg";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    header:         styles["header"],
    welcomeText:    styles["welcomeText"],

    body:           styles["body"],
    redirectLabel:  styles["redirectLabel"],
    redirect:       styles["redirect"],

    footer:         styles["footer"],
    footerItem:     styles["footerItem"],

    iconWrap:       styles["iconWrap"],
    itemLabel:      styles["itemLabel"],
    itemIcon:       styles["itemIcon"],
    glow:           styles["glow"],

    overlayBg:      styles["overlayBg"],
} as const;

type SwitchPanelProps = {
    mode: "login" | "register";
    setMode: (m: "login" | "register") => void;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function SwitchPanel({
    mode,
    setMode,
}: SwitchPanelProps) {

    const isLogin = mode === "login";

    // -------------- //
    // --- States --- //
    // -------------- //



    // ------------------ //
    // --- UI Helpers --- //
    // ------------------ //



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //



    // --------------- //
    // --- Effects --- //
    // --------------- //



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (

        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>

                {/* WelcomeText */}
                <div className={c.welcomeText}>Welcome</div>
            </div>

            {/* BODY */}
            <div className={c.body}>

                {/* RedirectLabel */}
                <div className={c.redirectLabel}>
                    {isLogin ? "Noch keinen Account?" : "Bereits registriert?"}
                </div>

                {/* RedirectLink */}
                <div
                    className={c.redirect}
                    onClick={() => setMode(isLogin ? "register" : "login")}
                >
                    {isLogin ? "REGISTER" : "LOGIN"}
                </div>
            </div>


            {/* FOOTER */}
            <div className={c.footer}>

                {/* ITEM: Todos */}
                <div className={c.footerItem}>
                    <div className={c.itemLabel}>Todos</div>
                    <div className={c.iconWrap}>
                        <div className={c.itemIcon}><TodoIcon /></div> 
                        <div className={c.glow}></div> 
                    </div>
                </div>

                {/* ITEM: Events */}
                <div className={c.footerItem}>
                    <div className={c.itemLabel}>Events</div>
                    <div className={c.iconWrap}>
                        <div className={c.itemIcon}><EventIcon /></div> 
                        <div className={c.glow}></div> 
                    </div>           
                </div>

                {/* ITEM: Notes */}
                <div className={c.footerItem}>
                    <div className={c.itemLabel}>Notes</div>
                    <div className={c.iconWrap}>
                        <div className={c.itemIcon}><NoteIcon /></div> 
                        <div className={c.glow}></div> 
                    </div>
                </div>
            </div>
        </div>

    );
}