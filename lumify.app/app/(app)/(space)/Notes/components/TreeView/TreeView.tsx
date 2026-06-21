
// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Models
import type { TreeNode } from "@/models/notes";
// Styles & Icons
import styles from "./TreeView.module.css";
import FolderIcon from "../../../../../src/svg/folder.svg";
import DocumentIcon from "../../../../../src/svg/document.svg";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:      styles["container"],
    node:           styles["node"],
    row:            styles["row"],
    rowSelected:    styles["rowSelected"],
    indent:         styles["indent"],
    chevron:        styles["chevron"],
    label:          styles["label"],
    icon:           styles["icon"],
    children:       styles["children"],
} as const;

export type TreeViewProps = {       // Whole Tree - Carries entries/nodes, the current selectedElement if available, inormation about expanded nodes, and correpsonding Listeners
    nodes: TreeNode[];
    selectedId?: string | null;
    expandedIds: Set<string>;

    onToggle: (id: string) => void;
    onSelect: (id: string | null) => void;
    onOpen: (id: string | null) => void;
};

type TreeRowProps = {               // Tree Row - Whole row in the TreeView (Node is placed inside a row)
    node: TreeNode;
    depth: number;
    selectedId?: string | null;
    expandedIds: Set<string>;

    onToggle: (id: string) => void;
    onSelect: (id: string) => void;
    onOpen: (id: string) => void;
};


// Change this value to adjust indentation.
const INDENT_WIDTH = 13;




// ----------- //
// --- ROW --- //
// ----------- //

// Build Tree Row with node inside
function TreeRow({
    node,
    depth,
    selectedId,
    expandedIds,

    onToggle,
    onSelect,
    onOpen
}: TreeRowProps) {


    const isFolder = node.type === "folder";
    const hasChildren = isFolder && !!node.children?.length;
    const isOpen = expandedIds.has(node.id);
    const isSelected = selectedId === node.id;


    // --------------- //
    // --- Handler --- //
    // --------------- //

    // Handle click
    const handleClick = () => {
        onSelect(node.id);
        onOpen(node.id);
    };

    // Handle toggle of nodes with children
    const handleToggle = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        if (hasChildren) {
            onToggle(node.id);
        }
    };


    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // Handle shevron if node-type is folder
    const chevron = isFolder ? (isOpen ? "▼" : "▶") : "";

    // Decide wich class is used depending on base-row or selected-row
    const rowClass = isSelected ? `${c.row} ${c.rowSelected}` : c.row;


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.node}>

            {/* Row */}
            <div className={rowClass} onClick={handleClick}>

                {/* Indent */}
                <div className={c.indent} style={{ width: depth * INDENT_WIDTH }} />

                {/* Chevron - grayed out (disabled) when there is nothing to expand (files & empty folders) */}
                <button type="button" className={c.chevron} onClick={handleToggle} disabled={!hasChildren}>
                    {chevron}
                </button>

                {/* Icon */}
                <div className={c.icon}>{isFolder ? <FolderIcon /> : <DocumentIcon />}</div>

                {/* Label */}
                <div className={c.label}>{node.name}</div>
            </div>

            {/* CHILDREN */}
            {hasChildren && isOpen ? (
                <div className={c.children}>
                    {node.children!.map((child) => (
                        <TreeRow
                            key={child.id}
                            node={child}
                            depth={depth + 1}
                            selectedId={selectedId}
                            expandedIds={expandedIds}
                            onToggle={onToggle}
                            onSelect={onSelect}
                            onOpen={onOpen}
                        />
                    ))}
                </div>
            ) : null}
        </div>
    );
}





// ----------------- //
// --- Component --- //
// ----------------- //
export default function TreeView({
    nodes,
    selectedId,
    expandedIds,

    onToggle,
    onSelect,
    onOpen,
}:TreeViewProps) {


    // -------------- //
    // --- States --- //
    // -------------- //

    // Root is open by default so notes stay visible on load (collapsible via its chevron).
    const [rootOpen, setRootOpen] = useState(true);



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //

    // Handle click on root folder
    const isRootSelected = selectedId === null;
    const rootRowClass = isRootSelected ? `${c.row} ${c.rowSelected}` : c.row;
    const hasRootChildren = nodes.length > 0;

    const handleRootClick = () => {
        onSelect(null);
        onOpen(null);
    };

    // Toggle root expansion without selecting the root row
    const handleRootToggle = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        if (hasRootChildren) {
            setRootOpen((open) => !open);
        }
    };



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Rootfolder */}
            <div className={rootRowClass} onClick={handleRootClick}>
                <div className={c.indent} style={{ width: 0 }} />
                {/* Chevron - same as every other folder; grayed out (disabled) when root is empty */}
                <button type="button" className={c.chevron} onClick={handleRootToggle} disabled={!hasRootChildren}>
                    {rootOpen ? "▼" : "▶"}
                </button>
                <div className={c.icon}>
                    <FolderIcon />
                </div>
                <div className={c.label}>.root</div>
            </div>

            {/* Generated Nodes - children of root, so indented one level deeper (depth 1) */}
            {rootOpen && (
                <div className={c.children}>
                    {nodes.map((node) => (
                        <TreeRow
                            key={node.id}
                            node={node}
                            depth={1}
                            selectedId={selectedId}
                            expandedIds={expandedIds}
                            onToggle={onToggle}
                            onSelect={onSelect}
                            onOpen={onOpen}
                        />
                    ))}
                </div>
            )}

        </div>
    );
}