"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef, useState } from "react";

// Styles
import styles from "./LinkLine.module.css";

// Icons
import ChevronIcon from "@/app/src/svg/arrow_up.svg";
import DeleteIcon from "@/app/src/svg/trash.svg";
import LinkIcon from "@/app/src/svg/link.svg";

// Models
import type { Note_LinkItem } from "@/models/Notes";

// Utils
import { animateCollapse } from "../../Utils/animateCollapse";
import { CONFIG } from "@/app/config/config";

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type LinkLineProps = {
    linkItems: Note_LinkItem[];
    onDelete: (linkItem: Note_LinkItem) => void;
};

const c = {
    container:          styles["container"],
    header:             styles["header"],
    headerIcon:         styles["headerIcon"],

    titleWrap:          styles["titleWrap"],
    title:              styles["title"],
    count:              styles["count"],

    actionsArea:        styles["actionsArea"],
    button:             styles["button"],
    buttonDelete:       styles["buttonDelete"],
    chevron:            styles["chevron"],
    chevronCollapsed:   styles["chevronCollapsed"],

    body:               styles["body"],
    bodyCollapsed:      styles["bodyCollapsed"],
    linkList:           styles["linkList"],
    linkItem:           styles["linkItem"],
    linkItemText:       styles["linkItemText"],
    linkItemOverlay:    styles["linkItemOverlay"],

} as const;


// -------------- //
// --- Helper --- //
// -------------- //

// Kept if naming comes in play at some point. Can be handled in here with "Links" as backfall.
function handleBlockTitle(): string {   
    return "Links";
}

// Returns either the label of a linkItem, or if not available, the trimmmed link.
function handleLinkLabel(linkItem: Note_LinkItem): string {     
    const trimmedLabel = linkItem.label?.trim();
    if (trimmedLabel) {
        return trimmedLabel;
    }

    const trimmedUrl = linkItem.url?.trim();
    if (trimmedUrl) {
        return trimmedUrl;
    }

    return "?";
}

// Takes the first initial of the avaluated linkLabel, ensures uppercase and returns it.
function handleLinkInitial(linkItem: Note_LinkItem): string {   
    const text = handleLinkLabel(linkItem);
    return text.charAt(0).toUpperCase();
}

// Ensures corrected and "sanitzed" link.
function handleHref(url: string | null | undefined): string {   
    const trimmedUrl = url?.trim() ?? "";

    if (!trimmedUrl) {
        return "#";
    }

    if (trimmedUrl.startsWith("http://") || trimmedUrl.startsWith("https://")) {
        return trimmedUrl;
    }

    return `https://${trimmedUrl}`;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function LinkLine({
    linkItems,
    onDelete
}: LinkLineProps) {

    const [isCollapsed, setIsCollapsed] = useState(false);
    
    const { showTooltip, hideTooltip } = useTooltip();

    const bodyRef = useRef<HTMLDivElement | null>(null);
    const animationRef = useRef<Animation | null>(null);


    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    
    // Open/Close Panels
    const handleToggleCollapse = () => {    
        setIsCollapsed(!isCollapsed);
    };

    // Tooltip movement-cop.
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {     
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };


    // --------------- //
    // --- Effects --- //
    // --------------- //

    // EFFEKT: Animates block collapse/expand, TRIGGER: state "isCollapsed" changes.
    useEffect(() => {
        const element = bodyRef.current;
        if (!element) { return; }

        animateCollapse(element, isCollapsed, animationRef, {   // Animates the collapse/epxand of the Element
            durationOpen: CONFIG.NOTE_MODAL.BLOCK_OPEN_DURATION,
            durationClose: CONFIG.NOTE_MODAL.BLOCK_CLOSE_DURATION,
            easing: "cubic-bezier(0.22, 1, 0.36, 1)"
        });
    }, [isCollapsed]);


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const blockTitle = handleBlockTitle();  // Set block title


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>

                {/* TitleWrap */}
                <div className={c.titleWrap}>
                    {/* Icon */}
                    <div className={c.headerIcon}>
                        <LinkIcon />
                    </div>

                    {/* Title */}
                    <div className={c.title}>
                        {blockTitle}
                    </div>

                    {/* Count */}
                    <div className={c.count}>
                        {linkItems.length}
                    </div>
                </div>

                {/* ActionsArea */}
                <div className={c.actionsArea}>
                    {/* Button: Delete */}
                    <button
                        type="button"
                        className={c.buttonDelete}
                    >
                        <DeleteIcon />
                    </button>

                    {/* Button: Collapse */}
                    <button
                        type="button"
                        className={c.button}
                        onClick={handleToggleCollapse}
                    >
                        <div className={`${c.chevron} ${isCollapsed ? c.chevronCollapsed : ""}`}>
                            <ChevronIcon />
                        </div>
                    </button>
                </div>
            </div>


            {/* BODY */}
            <div
                ref={bodyRef}
                className={`${c.body} ${isCollapsed ? c.bodyCollapsed : ""}`}
            >
                {/* LinkList */}
                <div className={c.linkList}>

                    {/* Map: LinkItems */}
                    {linkItems.map((linkItem) => (
                        
                        // LinkItem
                        <a
                            key={linkItem.id}
                            className={c.linkItem}
                            href={handleHref(linkItem.url)}
                            target="_blank"
                            rel="noopener noreferrer"
                            onMouseEnter={(e) => handleTooltipMove(e, handleLinkLabel(linkItem))}
                            onMouseLeave={hideTooltip}
                        >

                            {/* OverlayButton */}
                            <button
                                type="button"
                                className={c.linkItemOverlay}
                                onClick={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    onDelete(linkItem);
                                }}
                                onMouseEnter={(e) => {
                                    e.stopPropagation();
                                    handleTooltipMove(e, "Link löschen");
                                }}
                                onMouseLeave={(e) => handleTooltipMove(e, handleLinkLabel(linkItem))}
                            >
                                x
                            </button>

                            {/* LinkInitial */}
                            <div className={c.linkItemText}>
                                {handleLinkInitial(linkItem)}
                            </div>
                        </a>
                    ))}
                </div>
            </div>
        </div>
    );
}