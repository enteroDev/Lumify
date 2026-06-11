"use client";

import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import { PresenceStatus } from "@/models/User";
import { usePresence } from "@/hooks/usePresence";


// --------------------- //
// --- Types & Props --- //
// --------------------- //
type PresenceMap = Record<string, PresenceStatus>;

type PresenceContextValue = {
    getPresenceStatus: (userID: string, fallbackStatus?: PresenceStatus) => PresenceStatus;
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
export function usePresenceContext() {
    const context = useContext(PresenceContext);

    if (context == null) {
        throw new Error("usePresenceContext must be used inside PresenceProvider.");
    }

    return context;
}