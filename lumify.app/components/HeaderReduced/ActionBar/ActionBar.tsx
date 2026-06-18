"use client";

import { useTheme } from "../../_Theme/ThemeProvider";

import SoundOn from "../../../app/src/svg/soundOn.svg";
import Sun from "../../../app/src/svg/sun.svg";
import Moon from "../../../app/src/svg/moon.svg";

import styles from "./ActionBar.module.css";

export const c = {
    container:              styles["container"],
    actionPill:             styles["action-pill"],
    actionButton:           styles["action-button"],

    iconSound:              styles["icon-sound"],
    iconTheme:              styles["icon-theme"],
    dateValue:              styles["date-value"],
    dateLabel:              styles["date-label"],
} as const;



export default function ActionBar() {
    const { theme, toggle: toggleTheme } = useTheme();

    return (
        <div className={c.container}>
            <div className={c.actionPill}>
                <button className={c.actionButton} type="button" >
                    <SoundOn className={c.iconSound} width={20} height={20} />
                </button>
                <button className={c.actionButton} onClick={toggleTheme} type="button" aria-label="Theme wechseln" >
                    {theme === "chill"
                        ? <Sun className={c.iconTheme} width={20} height={20} />
                        : <Moon className={c.iconTheme} width={20} height={20} />
                    }
                </button>
            </div>
        </div>

    );
}
