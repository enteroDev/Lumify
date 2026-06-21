"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import type { MouseEvent } from "react";
// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Models
import { CalendarEventDTO, EventEntryType } from "@/models/Events";
// Icons
import TimedEventIcon from "@/app/src/svg/clock.svg";
import MultiDayEventIcon from "@/app/src/svg/layers_2.svg";
import FullDayEventIcon from "@/app/src/svg/calendar_day.svg";
// Styles
import styles from "./EventItem.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],
    selected:           styles["selected"],

    timeArea:           styles["timeArea"],
    time:               styles["time"],

    infoArea:           styles["infoArea"],
    title:              styles["title"],
    description:        styles["description"],

    indicatorArea:      styles["indicatorArea"],
    icon:               styles["icon"],

    fullDay:            styles["fullDay"],
    timed:              styles["timed"],
    multiDay:           styles["multiDay"],
} as const;

export type EventItemProps = {
    // The event to display.
    event: CalendarEventDTO;
    // The type of the event.
    type: EventEntryType;

    isSelected: boolean;
    onClick: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function EventItem({
    event,
    type,
    isSelected,

    onClick,
}: EventItemProps) {



    const { showTooltip, hideTooltip } = useTooltip();


    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Show the event's description in a tooltip while hovering (only if it has one).
    const showDescriptionTooltip = (e: MouseEvent<HTMLElement>) => {
        if (!event.description) { return; }
        showTooltip({ text: event.description, x: e.clientX, y: e.clientY });
    };

    function renderTimeText() {
        if (type === "fullDay") {
            return "Ganztag";
        }

        if (type === "multiDay") {
            const startDate = formatDate(event.startTime);
            const endDate = formatDate(event.endTime);

            return `${startDate} -
            ${endDate}`;
        }

        const start = event.startTime.slice(11, 16);
        const end = event.endTime.slice(11, 16);

        return `${start} -
        ${end}`;
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

    function formatDate(value: string) {
        const day = value.slice(8, 10);
        const month = value.slice(5, 7);

        return `${day}.${month}.`;
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // ContainerStyle depending on type and if selected
    let containerStyle = c.container;
    containerStyle += " " + c[type];

    if (isSelected) {
        containerStyle += " " + c.selected;
    }

    // Render correct stuff depending on type
    const timeText = renderTimeText();
    const icon = renderIcon();



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div
            className={containerStyle}
            onClick={onClick}
            onMouseEnter={showDescriptionTooltip}
            onMouseMove={showDescriptionTooltip}
            onMouseLeave={hideTooltip}
        >

            {/* TimeArea */}
            <div className={c.timeArea}>
                <div className={c.time}>
                    {timeText}
                </div>
            </div>

            {/* InfoArea */}
            <div className={c.infoArea}>
                <div className={c.title}>{event.name}</div>

                {event.description && (
                    <div className={c.description}>{event.description}</div>
                )}
            </div>

            {/* IndicatorArea */}
            <div className={c.indicatorArea}>
                <div className={c.icon}>
                    {icon}
                </div>
            </div>

        </div>
    );
}