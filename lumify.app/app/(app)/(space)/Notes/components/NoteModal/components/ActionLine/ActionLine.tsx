"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Styles & Icons
import styles from "./ActionLine.module.css";
import TextIcon from "@/app/src/svg/text_2.svg";
import CodeIcon from "@/app/src/svg/code.svg";
import LinkIcon from "@/app/src/svg/link.svg";

// Models
import type { Note } from "@/models/Notes";

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type ActionLineProps = {
    note: Note;
    onAddTextBlock?: (note: Note) => void;
    onAddCodeBlock?: (note: Note) => void;
    onAddLinkItem?: (note: Note) => void;
};

const c = {
    container:      styles["container"],
    pill:           styles["pill"],
    button:         styles["button"],
    iconWrap:       styles["iconWrap"],
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function ActionLine({
    note,
    onAddTextBlock,
    onAddCodeBlock,
    onAddLinkItem
}: ActionLineProps) {

    const { showTooltip, hideTooltip } = useTooltip();


    // ------------------ //
    // --- GUI Helper --- //
    // ------------------ //
    const handleTooltipMove = (e: React.MouseEvent<HTMLButtonElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            
            {/* Pill */}
            <div className={c.pill}>
                
                {/* Button: Create Textblock */}
                <button
                    type="button"
                    className={c.button}
                    onClick={() => onAddTextBlock?.(note)}
                    onMouseEnter={(e) => handleTooltipMove(e, "Textblock erstellen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Textblock erstellen")}
                    onMouseLeave={hideTooltip}
                >
                    {/* Icon */}
                    <div className={c.iconWrap}>
                        <TextIcon />
                    </div>
                </button>

                {/* Button: Create Codeblock */}
                <button
                    type="button"
                    className={c.button}
                    onClick={() => onAddCodeBlock?.(note)}
                    onMouseEnter={(e) => handleTooltipMove(e, "Codeblock erstellen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Codeblock erstellen")}
                    onMouseLeave={hideTooltip}
                >
                    {/* Icon */}
                    <div className={c.iconWrap}>
                        <CodeIcon />
                    </div>
                </button>

                {/* Button: Create Link */}
                <button
                    type="button"
                    className={c.button}
                    onClick={() => onAddLinkItem?.(note)}
                    onMouseEnter={(e) => handleTooltipMove(e, "Link erstellen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Link erstellen")}
                    onMouseLeave={hideTooltip}
                >
                    {/* Icon */}
                    <div className={c.iconWrap}>
                        <LinkIcon />
                    </div>
                </button>
            </div>         
        </div>
    );
}