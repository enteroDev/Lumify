

import styles from "./DayHeader.module.css";

export const c = {
    container:      styles["container"],
    label:          styles["label"],
} as const;


export default function DayHeader() {
    return (
        <div className={c.container}>
            <div className={c.label}>Montag</div>
            <div className={c.label}>Dienstag</div>
            <div className={c.label}>Mittwoch</div>
            <div className={c.label}>Donnerstag</div>
            <div className={c.label}>Freitag</div>
            <div className={c.label}>Samstag</div>
            <div className={c.label}>Sonntag</div>
        </div>
    );
}