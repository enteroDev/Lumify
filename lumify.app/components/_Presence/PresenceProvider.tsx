"use client";

import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { PresenceStatus } from "@/models/User";
import { usePresence } from "@/hooks/usePresence";


// --------------------- //
// --- Types & Props --- //
// --------------------- //
type PresenceMap = Record<string, PresenceStatus>;

type PresenceContextValue = {
    getPresenceStatus: (userID: string, fallbackStatus?: PresenceStatus) => PresenceStatus;
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

    const handlePresenceChanged = useCallback((changedUserID: string, presenceStatus: PresenceStatus) => {
        setPresenceMap(prev => ({
            ...prev,
            [changedUserID]: presenceStatus,
        }));
    }, []);

    usePresence(userID, handlePresenceChanged);

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
        };
    }, [getPresenceStatus]);

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