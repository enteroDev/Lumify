"use client";

import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import { PresenceStatus } from "@/models/User";
import { usePresence } from "@/hooks/usePresence";


// --------------------- //
// --- Types & Props --- //
// --------------------- //
/** Maps a user ID to its latest known {@link PresenceStatus}. */
type PresenceMap = Record<string, PresenceStatus>;

/** The presence API exposed via {@link usePresenceContext}. */
export type PresenceContextValue = {
    /**
     * Returns the live presence of a user, or `fallbackStatus` (default `Offline`) if none has been
     * received yet.
     * @param userID The user to look up.
     * @param fallbackStatus Status to use when no live value is known.
     */
    getPresenceStatus: (userID: string, fallbackStatus?: PresenceStatus) => PresenceStatus;
    /**
     * Subscribes to "a friendship of the current user changed" events.
     * @param handler Called on each change.
     * @returns An unsubscribe function.
     */
    subscribeFriendshipChanged: (handler: () => void) => () => void;
};

const PresenceContext = createContext<PresenceContextValue | null>(null);

type PresenceProviderProps = {
    userID: string | null;
    children: React.ReactNode;
};



// ---------------- //
// --- Provider --- //
// ---------------- //
/**
 * Centralises live presence for the app. It opens a single presence connection (via
 * {@link usePresence}) for `userID`, keeps an in-memory map of every user's latest status, and
 * fans out friendship-changed events to subscribers — so many components share one hub connection.
 * Consumers call {@link usePresenceContext}.
 * @param props The current user ID (or `null` to stay idle) and React children.
 */
export default function PresenceProvider({ userID, children }: PresenceProviderProps) {
    const [presenceMap, setPresenceMap] = useState<PresenceMap>({});

    const friendshipSubscribersRef = useRef<Set<() => void>>(new Set());

    const handlePresenceChanged = useCallback((changedUserID: string, presenceStatus: PresenceStatus) => {
        setPresenceMap(prev => ({
            ...prev,
            [changedUserID]: presenceStatus,
        }));
    }, []);

    // Fan out a single ChatHub "FriendshipChanged" event to all subscribed components.
    const handleFriendshipChanged = useCallback(() => {
        friendshipSubscribersRef.current.forEach(handler => handler());
    }, []);

    const subscribeFriendshipChanged = useCallback((handler: () => void) => {
        friendshipSubscribersRef.current.add(handler);

        return () => {
            friendshipSubscribersRef.current.delete(handler);
        };
    }, []);

    usePresence(userID, handlePresenceChanged, handleFriendshipChanged);

    const getPresenceStatus = useCallback((userID: string, fallbackStatus?: PresenceStatus) => {
        const liveStatus = presenceMap[userID];

        if (liveStatus !== undefined) {
            return liveStatus;
        }

        return fallbackStatus ?? PresenceStatus.Offline;
    }, [presenceMap]);

    const value = useMemo(() => {
        return {
            getPresenceStatus,
            subscribeFriendshipChanged,
        };
    }, [getPresenceStatus, subscribeFriendshipChanged]);

    return (
        <PresenceContext.Provider value={value}>
            {children}
        </PresenceContext.Provider>
    );
}


// ------------ //
// --- Hook --- //
// ------------ //
/**
 * Accesses the presence {@link PresenceContextValue} (`getPresenceStatus`,
 * `subscribeFriendshipChanged`).
 * @returns The presence API.
 * @throws Error if used outside a {@link PresenceProvider}.
 */
export function usePresenceContext() {
    const context = useContext(PresenceContext);

    if (context == null) {
        throw new Error("usePresenceContext must be used inside PresenceProvider.");
    }

    return context;
}