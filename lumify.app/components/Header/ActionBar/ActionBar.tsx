"use client";

import { useMusic } from "../../_Audio/MusicProvider";
import { useTheme } from "../../_Theme/ThemeProvider";

import SoundOn from "../../../app/src/svg/soundOn.svg";
import SoundOff from "../../../app/src/svg/soundOff.svg";
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
    const { isEnabled, toggle } = useMusic();
    const { theme, toggle: toggleTheme } = useTheme();

    return (
        <div className={c.container}>
            <div className={c.actionPill}>
                <button className={c.actionButton} onClick={toggle} type="button" >
                    {isEnabled
                        ? <SoundOn className={c.iconSound} width={20} height={20} />
                        : <SoundOff className={c.iconSound} width={20} height={20} />
                    }
                </button>
                <button className={c.actionButton} onClick={toggleTheme} type="button" aria-label="Theme wechseln" >
                    {/* Icon shows the theme you'd switch INTO: chill -> sunny beach, beach -> chill night. */}
                    {theme === "chill"
                        ? <Sun className={c.iconTheme} width={20} height={20} />
                        : <Moon className={c.iconTheme} width={20} height={20} />
                    }
                </button>
            </div>
        </div>

    );
}
