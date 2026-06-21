"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import type { MouseEvent } from "react";
// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Models
import { CalendarEventDTO, EventEntryType, EventEntryVariant } from "@/models/Events";
// Icons
import TimedEventIcon from "@/app/src/svg/clock.svg";
import MultiDayEventIcon from "@/app/src/svg/layers_2.svg";
import FullDayEventIcon from "@/app/src/svg/calendar_day.svg";
// Styles
import styles from "./EventEntry.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    compact:            styles["compact"],
    detailed:           styles["detailed"],

    infoArea:           styles["infoArea"],
    meta:               styles["meta"],
    title:              styles["title"],

    indicatorArea:      styles["indicatorArea"],
    icon:               styles["icon"],
    indicator:          styles["indicator"],

    fullDay:            styles["fullDay"],
    timed:              styles["timed"],
    multiDay:           styles["multiDay"],
} as const;

export type EventEntryProps = {
    event:      CalendarEventDTO;
    variant:    EventEntryVariant;
    type:       EventEntryType;
};





// ----------------- //
// --- Component --- //
// ----------------- //
export default function EventEntry({
    event,
    variant,
    type,
}: EventEntryProps) {


    const { showTooltip, hideTooltip } = useTooltip();


    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Show the event's description in a tooltip while hovering (only if it has one).
    const showDescriptionTooltip = (e: MouseEvent<HTMLElement>) => {
        if (!event.description) { return; }
        showTooltip({ text: event.description, x: e.clientX, y: e.clientY });
    };

    function renderMeta() {

        const start = event.startTime.slice(11, 16);
        const end = event.endTime.slice(11, 16);

        // Compact: show name for fullDay + multiDay
        if (variant === "compact") {

            if (type === "fullDay" || type === "multiDay") {
                return event.name;
            }

            // timed
            return `${start} - ${end}`;
        }


        // Detailed: show type & time
        if (variant === "detailed") {

            if (type === "fullDay") {
                return "TagesEvent";
            }

            if (type === "multiDay") {
                return "MehrtagesEvent";
            }

            // timed
            return `${start} - ${end}`;
        }

        return "";
    }

    function renderIcon() {
        if (type === "fullDay") {
            return <FullDayEventIcon />;
        }

        if (type === "timed") {
            return <TimedEventIcon />;
        }

        if (type === "multiDay") {
            return <MultiDayEventIcon />;
        }

        return null;
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    let containerStyle = c.container;
    containerStyle += variant === "compact" ? " " + c.compact : " " + c.detailed;
    containerStyle += " " + c[type];

    const metaText = renderMeta();
    const icon = renderIcon();




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div
            className={containerStyle}
            onMouseEnter={showDescriptionTooltip}
            onMouseMove={showDescriptionTooltip}
            onMouseLeave={hideTooltip}
        >

            {/* InfoArea */}
            <div className={c.infoArea}>
                <div className={c.meta}>{metaText}</div>

                {variant === "detailed" && (
                    <div className={c.title}>{event.name}</div>
                )}
            </div>

            {/* IndicatorArea */}
            <div className={c.indicatorArea}>
                <div className={c.indicator}>
                    {variant === "detailed" && (
                        <div className={c.icon}>{icon}</div>
                    )}
                </div>
            </div>

        </div>
    );
}