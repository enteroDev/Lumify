
"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./CalenderNavigation.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:      styles["container"],

    date:           styles["date"],
    month:          styles["month"],
    year:           styles["year"],

    action:         styles["action"],
    actionPill:     styles["actionPill"],
    actionPillIcon: styles["actionPillIcon"],
    actionPillText: styles["actionPillText"],

    paging:         styles["paging"],
    back:           styles["back"],
    forward:        styles["forward"],
} as const;

type CalenderNavigationProps = {
    viewYear: number;
    viewMonth: number; // 0=Jan ... 11=Dec

    previousMonth: () => void;
    nextMonth: () => void;

    onAddEvent: () => void;
};

const monthNamesDE = [
    "Januar", "Februar", "März", "April", "Mai", "Juni",
    "Juli", "August", "September", "Oktober", "November", "Dezember"
];



// ----------------- //
// --- Component --- //
// ----------------- //
export default function CalendarNavigation({
    viewYear,
    viewMonth,
    previousMonth,
    nextMonth,

    onAddEvent,
}: CalenderNavigationProps) {

    const monthName = monthNamesDE[viewMonth];

    return (
        <div className={c.container}>


            {/* DateDisplay */}
            <div className={c.date}>
                <div className={c.month}>{monthName}</div>
                <div className={c.year}>{viewYear}</div>
            </div>


            {/* Action */}
            <div className={c.action}>
                <div className={c.actionPill} onClick={onAddEvent}>
                    <div className={c.actionPillIcon}>+</div>
                    <div className={c.actionPillText}>Termin erstellen</div>
                </div>
            </div>


            {/* Paging */}
            <div className={c.paging}>
                <div className={c.back} onClick={previousMonth}>{"<"}</div>
                <div className={c.forward} onClick={nextMonth}>{">"}</div>
            </div>

        </div>
    );
}