"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useRef, useState, type ReactNode } from "react";

// Styles
import styles from "./Textblock.module.css";

// Icons
import ChevronIcon from "@/app/src/svg/arrow_up.svg";
import SaveIcon from "@/app/src/svg/save.svg";
import DeleteIcon from "@/app/src/svg/trash.svg";
import CodeIcon from "@/app/src/svg/code.svg";
import TextIcon from "@/app/src/svg/text_2.svg";

// Models
import type { Note_TextBlock } from "@/models/notes";

// Utils
import { animateCollapse } from "../../Utils/animateCollapse";
import { CONFIG } from "@/app/(app)/config/config";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type TextBlockProps = {
    block: Note_TextBlock;
    autoFocus?: boolean;
    onSave: (block: Note_TextBlock) => Promise<void> | void;
    onToggleCollapse: (block: Note_TextBlock, isCollapsed: boolean) => Promise<void> | void;
    onDelete: (block: Note_TextBlock) => void;
};

const c = {
    container:          styles["container"],
    containerSaved:     styles["containerSaved"],
    header:             styles["header"],
    titleWrap:          styles["headerTitle"],
    headerIcon:         styles["headerIcon"],
    actionsArea:        styles["headerActions"],
    title:              styles["title"],
    body:               styles["body"],
    bodyCollapsed:      styles["bodyCollapsed"],
    textBlock:          styles["textBlock"],
    codeBlock:          styles["codeBlock"],
    buttonSave:         styles["buttonSave"],
    buttonDelete:       styles["buttonDelete"],
    buttonChevron:      styles["buttonChevron"],
    chevron:            styles["chevron"],
    chevronCollapsed:   styles["chevronCollapsed"],
} as const;


// -------------- //
// --- Helper --- //
// -------------- //

// Return needed title
function handleBlockTitle(block: Note_TextBlock): string {      
    return block.type === 1
        ? "Code Block"
        : "Text Block";
}

// Return needed icon
function handleBlockIcon(block: Note_TextBlock): ReactNode {    
    return block.type === 1
        ? <CodeIcon />
        : <TextIcon />;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TextBlock({
    block,
    autoFocus = false,
    onSave,
    onToggleCollapse,
    onDelete,
}: TextBlockProps) {

    const [isCollapsed, setIsCollapsed] = useState(block.isCollapsed);
    const [content, setContent] = useState(block.content ?? "");
    const [isSaving, setIsSaving] = useState(false);

    const textareaRef = useRef<HTMLTextAreaElement | null>(null);
    const bodyRef = useRef<HTMLDivElement | null>(null);
    const animationRef = useRef<Animation | null>(null);


    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //

    // Triggers flash when saving the Textblock.
    function triggerSavedFlash() {      
        const textarea = textareaRef.current;
        if (!textarea) { return; }

        textarea.classList.remove(c.containerSaved);
        void textarea.offsetWidth;
        textarea.classList.add(c.containerSaved);

        window.setTimeout(() => {
            textarea.classList.remove(c.containerSaved);
        }, 700);
    }

    // Controls resizing of Textarea. Keeps it growing till maxHeight is hit. Collapses it if last row is deleted till minHeight is hit.
    const autoResizeTextarea = () => {      
        const textarea = textareaRef.current;
        if (!textarea) { return; }

        const minHeight = CONFIG.NOTE_MODAL.TEXTAREA_MIN_HEIGHT;
        const maxHeight = CONFIG.NOTE_MODAL.TEXTAREA_MAX_HEIGHT;

        textarea.style.height = "auto";

        const nextHeight = Math.max(
            minHeight,
            Math.min(textarea.scrollHeight, maxHeight)
        );

        textarea.style.height = `${nextHeight}px`;
        textarea.style.maxHeight = `${maxHeight}px`;
        textarea.style.overflowY = textarea.scrollHeight > maxHeight ? "auto" : "hidden";
    };



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    // CLICK: Creates Object. Trys to save TextBlock to database via function "onSave". Triggers flash on success.
    const handleSaveClick = async () => {       
        
        // Create Object.
        const updatedBlock: Note_TextBlock = {  
            ...block,
            content: content,
            isCollapsed: isCollapsed,
        };

        // Trys to save TextBlock to database via function "onSave". Triggers flash on success.
        try {
            setIsSaving(true);
            await onSave(updatedBlock);
            triggerSavedFlash();
        } finally {
            setIsSaving(false);
        }
    };

    // Handle open/closed - Also persist collapsed state immediately when user toggles the block
    const handleToggleCollapse = async () => {      
        const nextIsCollapsed = !isCollapsed;
        setIsCollapsed(nextIsCollapsed);
        await onToggleCollapse(block, nextIsCollapsed);
    };

    // Handle short-key "strg+s". Save on short-key.
    const handleSaveShortcut = async (e: React.KeyboardEvent<HTMLTextAreaElement>) => {     
        if (isSaving) { return; }

        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") {
            e.preventDefault();
            await handleSaveClick();
        }
    };



    // --------------- //
    // --- Effects --- //
    // --------------- //

    // EFFEKT: Auto-focuses textarea and jumps cursor to end of text, TRIGGER: prop "autoFocus"
    useEffect(() => {       
        if (autoFocus) {
            textareaRef.current?.focus();

            const length = textareaRef.current?.value.length ?? 0;
            textareaRef.current?.setSelectionRange(length, length);
        }
    }, [autoFocus]);

    // EFFEKT: Animates block collapse/expand, TRIGGER: state "isCollapsed"
    useEffect(() => {   
        const element = bodyRef.current;
        if (!element) { return; }

        animateCollapse(element, isCollapsed, animationRef, {   // Animates the collapse/epxand of the Element
            durationOpen: CONFIG.NOTE_MODAL.BLOCK_OPEN_DURATION,
            durationClose: CONFIG.NOTE_MODAL.BLOCK_CLOSE_DURATION,
            easing: "cubic-bezier(0.22, 1, 0.36, 1)"
        });
    }, [isCollapsed]);

    // EFFEKT: Syncs local state with incoming block props, TRIGGER: block.content or block.isCollapsed changes
    useEffect(() => {   
        setContent(block.content ?? "");
        setIsCollapsed(block.isCollapsed);
    }, [block.content, block.isCollapsed]);

    // EFFEKT: Adjusts textarea height to fit content, TRIGGER: local state "content" changes
    useEffect(() => {
        autoResizeTextarea();
    }, [content]);



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const blockTitle = handleBlockTitle(block);
    const isCodeBlock = block.type === 1;
    const blockIcon = handleBlockIcon(block);


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
                        {blockIcon}
                    </div>

                    {/* Title */}
                    <div className={c.title}>
                        {blockTitle}
                    </div>
                </div>

                {/* ActionsArea */}
                <div className={c.actionsArea}>

                    {/* Button: Save */}
                    <button
                        type="button"
                        className={c.buttonSave}
                        onClick={handleSaveClick}
                        disabled={isSaving}
                    >
                        <SaveIcon />
                    </button>

                    {/* Button: Delete */}
                    <button
                        type="button"
                        className={c.buttonDelete}
                        onClick={() => onDelete(block)}
                    >
                        <DeleteIcon />
                    </button>

                    {/* Button: Collapse */}
                    <button
                        type="button"
                        className={c.buttonChevron}
                        onClick={handleToggleCollapse}
                    >
                        {/* ChevronIcon */}
                        <span className={`${c.chevron} ${isCollapsed ? c.chevronCollapsed : ""}`}>
                            <ChevronIcon />
                        </span>
                    </button>

                </div>
            </div>

            {/* BODY */}
            <div
                ref={bodyRef}
                className={`${c.body} ${isCollapsed ? c.bodyCollapsed : ""}`}
            >
                {/* TextArea */}
                <textarea
                    ref={textareaRef}
                    className={isCodeBlock ? c.codeBlock : c.textBlock}
                    value={content}
                    onChange={(e) => {
                        setContent(e.target.value);
                        autoResizeTextarea();
                    }}
                    onKeyDown={handleSaveShortcut}
                    spellCheck={!isCodeBlock}
                />
            </div>

        </div>
    );
}