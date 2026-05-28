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

// Types of spaces
// - Private space: Personal space for the user, not shared with others.
// - Workspace: Shared space for a team or project, identified by workspaceID and name.
export type Space =
    | { type: "private" }
    | { type: "workspace"; workspaceID: string; name: string };


// Properties the space context provides
// - currentSpace: The currently active space (private or workspace).
// - setCurrentSpace: Function to switch spaces, updates both, state & localStorage.
// - isPrivate: Boolean indicating if the current space is private (derived from currentSpace).
// - workspaceID: ID of the workspace if in a workspace, null if in private space (derived from currentSpace).
type SpaceContextProps = {
    currentSpace: Space;
    setCurrentSpace: (next: Space) => void;
    isPrivate: boolean;
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
export function useSpace(): SpaceContextProps {
    const ctx = useContext(SpaceContext);
    if (!ctx) { throw new Error("useSpace must be used within SpaceProvider"); }
    return ctx;
}