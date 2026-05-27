"use client";

import { useEffect, useState, type ReactNode } from "react";
import { UserService } from "@/services/api/userService";
import PresenceProvider from "@/components/_Presence/PresenceProvider";

type PresenceBridgeProps = {
    children: ReactNode;
};

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