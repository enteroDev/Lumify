"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import type { ReactNode } from "react";

// Styles
import styles from "./BlockList.module.css";

// Components
import TextBlock from "./Components/Textblock/Textblock";
import LinkLine from "./Components/LinkLine/LinkLine";

// Models
import type { Note_TextBlock, Note_LinkItem } from "../../../../../../../../models/Notes";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
export type BlockListProps = {
    textBlocks: Note_TextBlock[];
    linkItems: Note_LinkItem[];
    onSaveTextBlock: (block: Note_TextBlock) => Promise<void> | void;
    onToggleTextBlockCollapse: (block: Note_TextBlock, isCollapsed: boolean) => Promise<void> | void;
    onDeleteTextBlock: (block: Note_TextBlock) => void;
    onDeleteLinkItem: (linkItem: Note_LinkItem) => void;
};

type BlockListEntry =
    | {
        type: "textBlock";
        notePos: number;
        textBlock: Note_TextBlock;
    }
    | {
        type: "linkLine";
        notePos: number;
        linkItems: Note_LinkItem[];
    };

const c = {
    container:      styles["container"],
    infoBox:        styles["infoBox"],
    text:           styles["text"],
    subText:        styles["subText"],
} as const;


// -------------- //
// --- Helper --- //
// -------------- //

// Returns all NoteBlocks, ordered by notePos or/and alphabet.
export function handleEntries(textBlocks: Note_TextBlock[], linkItems: Note_LinkItem[]): BlockListEntry[] {
    const entries: BlockListEntry[] = [];

    for (const textBlock of textBlocks) {
        entries.push({
            type: "textBlock",
            notePos: textBlock.notePos,
            textBlock: textBlock
        });
    }

    if (linkItems.length > 0) {
        const sortedLinkItems = [...linkItems].sort((a, b) => a.notePos - b.notePos);
        const firstLinkItem = sortedLinkItems[0];

        entries.push({
            type: "linkLine",
            notePos: firstLinkItem.notePos,
            linkItems: sortedLinkItems
        });
    }

    return [...entries].sort((a, b) => a.notePos - b.notePos);
}


// ----------------- //
// --- Component --- //
// ----------------- //
export default function BlockList({
    textBlocks,
    linkItems,
    onSaveTextBlock,
    onToggleTextBlockCollapse,
    onDeleteTextBlock,
    onDeleteLinkItem
}: BlockListProps) {

    const entries = handleEntries(textBlocks, linkItems);
    const isEmpty = entries.length === 0;
    let blocks: ReactNode;


    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //

    // Rendered HTML when note has no blocks
    const renderEmptyState = () => (
        <div className={c.infoBox}>
            <div className={c.text}>Diese Notiz ist noch leer.</div>
            <div className={c.subText}>(Füge einen Block hinzu um zu starten.)</div>
        </div>
    );

    // Rendered HTML when note has blocks
    const renderBlocks = () => entries.map((entry) => {
        if (entry.type === "textBlock") {
            return (
                <TextBlock
                    key={entry.textBlock.id}
                    block={entry.textBlock}
                    onSave={onSaveTextBlock}
                    onToggleCollapse={onToggleTextBlockCollapse}
                    onDelete={onDeleteTextBlock}
                />
            );
        }

        return (
            <LinkLine
                key={`link-line-${entry.notePos}`}
                linkItems={entry.linkItems}
                onDelete={onDeleteLinkItem}
            />
        );
    });


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const content = isEmpty ? renderEmptyState() : renderBlocks();



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {content}
        </div>
    );
}