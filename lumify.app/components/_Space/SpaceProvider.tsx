"use client";


// --------------- //
// --- Imports --- //
// --------------- //
import React, {
    createContext,
    useCallback,
    useContext,
    useMemo,
    useState,
} from "react";



// ------------------- //
// --- Types/Props --- //
// ------------------- //

/**
 * The space the user is currently working in. Either their private personal space, or a shared
 * workspace identified by its ID and name.
 */
export type Space =
    | { type: "private" }
    | { type: "workspace"; workspaceID: string; name: string };


/** The space API exposed via {@link useSpace}. */
export type SpaceContextProps = {
    /** The currently active space (private or a workspace). */
    currentSpace: Space;
    /** Switches the active space (updates both state and `localStorage`). */
    setCurrentSpace: (next: Space) => void;
    /** `true` while the private space is active (derived from {@link SpaceContextProps.currentSpace}). */
    isPrivate: boolean;
    /** The active workspace ID, or `null` in the private space (derived). */
    workspaceID: string | null;
};

const SpaceContext = createContext<SpaceContextProps | null>(null);
const SPACE_STORAGE = "lumify.currentSpace.v1";



// -------------- //
// --- Helper --- //
// -------------- //

// Type guard for Space
// Checks if a value conforms to the Space type structure
// "value is Space" tells Typescript that if this function returns true, the value can be treated as a Space type in the calling code.
function isSpace(value: unknown): value is Space {
    if (typeof value !== "object" || value === null) { return false; }

    const v = value as Record<string, unknown>;

    if (v.type === "private") { return true; }

    return v.type === "workspace"
        && typeof v.workspaceID === "string"
        && typeof v.name === "string";
}

// Read initial space from localStorage, with fallback to private space if no storage or invalid data
// Tries to parse stored space data, validates it, and returns it. If any step fails, defaults to private space.
function readInitialSpace(): Space {
    if (typeof window === "undefined") { return { type: "private" }; }

    try {
        const raw = window.localStorage.getItem(SPACE_STORAGE);
        if (!raw) { return { type: "private" }; }

        const parsed: unknown = JSON.parse(raw);
        if (!isSpace(parsed)) { return { type: "private" }; }

        return parsed.type === "private"
            ? { type: "private" }
            : { type: "workspace", workspaceID: parsed.workspaceID, name: parsed.name };
    } catch {
        return { type: "private" };
    }
}



// ----------------- //
// --- Component --- //
// ----------------- //
/**
 * Tracks which {@link Space} the user is working in (private vs. a workspace) and persists the
 * choice to `localStorage`. The active space drives whether other features query personal or
 * workspace data. To keep server and client markup identical, the first render is always `private`.
 * Wrap the app once; consumers call {@link useSpace}.
 * @param props Standard React children.
 */
export default function SpaceProvider({ children }: { children: React.ReactNode }) {

    // IMPORTANT: deterministic initial render
    // Server == Client -> Means server always sends space "private" as initial.
    // Frontend ist NOT allowed to display anything else on initial load
    const [currentSpace, setCurrentSpaceState] = useState<Space>({ type: "private" });

    const setCurrentSpace = useCallback((next: Space) => {
        setCurrentSpaceState(next);

        try {
            window.localStorage.setItem(SPACE_STORAGE, JSON.stringify(next));
        } catch {
            // ignore
        }
    }, []);

    const spaceContextProps = useMemo<SpaceContextProps>(() => {
        const isPrivate = currentSpace.type === "private";
        const workspaceID = currentSpace.type === "workspace" ? currentSpace.workspaceID : null;

        return { currentSpace, setCurrentSpace, isPrivate, workspaceID };
    }, [currentSpace, setCurrentSpace]);

    return (
        <SpaceContext.Provider value={spaceContextProps}>
            {children}
        </SpaceContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * Accesses the space {@link SpaceContextProps} (`currentSpace`, `setCurrentSpace`, `isPrivate`,
 * `workspaceID`).
 * @returns The space API.
 * @throws Error if used outside a {@link SpaceProvider}.
 */
export function useSpace(): SpaceContextProps {
    const ctx = useContext(SpaceContext);
    if (!ctx) { throw new Error("useSpace must be used within SpaceProvider"); }
    return ctx;
}