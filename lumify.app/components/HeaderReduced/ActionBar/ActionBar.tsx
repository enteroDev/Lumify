"use client";

import SoundOn from "../../../app/src/svg/soundOn.svg";
import SoundOff from "../../../app/src/svg/soundOff.svg";
import Sun from "../../../app/src/svg/sun.svg";
import Star from "../../../app/src/svg/star.svg";

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


    return (
        <div className={c.container}>
            <div className={c.actionPill}>
                <button className={c.actionButton} type="button" >
                    <SoundOn className={c.iconSound} width={20} height={20} />
                </button>
                <button className={c.actionButton}>
                    <Sun className={c.iconTheme} width={20} height={20} />
                </button>
            </div>
        </div>

    );
}
