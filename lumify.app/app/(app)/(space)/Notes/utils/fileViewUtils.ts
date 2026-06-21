import type { TreeNode } from "../../../../../models/Notes";

export type FindResult = {
    node: TreeNode | null;
    parent: TreeNode | null;
    path: TreeNode[]; // root -> ... -> node
};

export type FileViewVM = {
    breadcrumb: string;
    currentFolderID: string | null;
    items: TreeNode[];
    isEmpty: boolean;
    notFound: boolean;
};

// Find a node by id and return its parent + full path
export function findNode(nodes: TreeNode[], targetId: string): FindResult {

    const walk = (items: TreeNode[], parent: TreeNode | null, path: TreeNode[]): FindResult => {
        for (const item of items) {
            const nextPath = [...path, item];

            if (item.id === targetId) {
                return { node: item, parent, path: nextPath };
            }

            const children = item.children;
            if (Array.isArray(children) && children.length > 0) {
                const res = walk(children, item, nextPath);
                if (res.node) { return res; }
            }
        }

        return { node: null, parent: null, path: [] };
    };

    return walk(nodes, null, []);
}

// Build breadcrumb text from a path (folders only)
export function buildBreadcrumb(path: TreeNode[]): string {
    const folderPath = path.filter((x) => x.type === "folder").map((x) => x.name);
    return folderPath.length > 0 ? folderPath.join(" / ") : "";
}

function normalize(s: string): string {
    return s.trim().toLowerCase();
}

export function filterItems(items: TreeNode[], query: string): TreeNode[] {
    const q = normalize(query);
    if (q.length === 0) {
        return items;
    }

    return items.filter((x) => normalize(x.name).includes(q));
}

// Build the FileView view-model (items, breadcrumb, current folder id, etc.)
export function buildFileViewVM(nodes: TreeNode[], selectedId: string | null, query: string): FileViewVM {
    // Root view: show top-level items
    if (!selectedId) {
        const items = filterItems(nodes, query);

        return {
            breadcrumb: "",
            currentFolderID: null,
            items,
            isEmpty: items.length === 0,
            notFound: false,
        };
    }

    const { node, parent, path } = findNode(nodes, selectedId);

    if (!node) {
        return {
            breadcrumb: "",
            currentFolderID: null,
            items: [],
            isEmpty: true,
            notFound: true,
        };
    }

    // If a file is selected, the "current folder" is its parent folder
    const currentFolder = node.type === "folder" ? node : parent;
    const currentFolderId = currentFolder ? currentFolder.id : null;

    const baseItems =
        currentFolder && currentFolder.children
            ? currentFolder.children
            : currentFolder
                ? []
                : nodes;

    const items = filterItems(baseItems, query);

    return {
        breadcrumb: buildBreadcrumb(path),
        currentFolderID: currentFolderId,
        items,
        isEmpty: items.length === 0,
        notFound: false,
    };
}

// Compute the folder id that "back" should navigate to
export function getParentFolderId(nodes: TreeNode[], selectedId: string | null): string | null {
    if (!selectedId) {
        return null;
    }

    const { node, parent } = findNode(nodes, selectedId);

    // Folder selected: go to parent (or root)
    if (node && node.type === "folder") {
        return parent ? parent.id : null;
    }

    // File selected: go to its parent folder (or root)
    return parent ? parent.id : null;
}