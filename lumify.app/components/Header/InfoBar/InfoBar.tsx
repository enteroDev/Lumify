"use client";

import styles from "./InfoBar.module.css";

export const c = {
    container:              styles["container"],
    dateValue:              styles["date-value"],
    dateLabel:              styles["date-label"],
} as const;



export default function InfoBar() {
    return (
        <div className={c.container}>
            <div className="block-box">
                <p className={c.dateLabel}>Datum: </p>
                <p className={c.dateValue}>{new Date().toLocaleDateString()}</p>
            </div>
        </div>
    );
}
