"use client";

import { useEffect, useState, type ReactNode } from "react";
import { UserService } from "@/services/api/userService";
import PresenceProvider from "@/components/_Presence/PresenceProvider";

/** Props for {@link PresenceBridge}. */
export type PresenceBridgeProps = {
    /** The subtree that should run inside the presence context. */
    children: ReactNode;
};

/**
 * Wrapper that loads the current user's ID once on mount and feeds it into the
 * {@link PresenceProvider}, so presence tracking starts as soon as the user is known.
 * @param props The wrapped {@link PresenceBridgeProps.children}.
 */
export default function PresenceBridge({ children }: PresenceBridgeProps) {
    const [userID, setUserID] = useState<string | null>(null);

    useEffect(() => {
        const loadCurrentUser = async () => {
            try {
                const accountInfo = await UserService.getUserAccountInfo();
                setUserID(accountInfo.id);
            } catch (error) {
                console.error("Failed to load current user for presence", error);
                setUserID(null);
            }
        };

        void loadCurrentUser();
    }, []);

    return (
        <PresenceProvider userID={userID}>
            {children}
        </PresenceProvider>
    );
}