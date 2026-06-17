"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useEffect, useState } from "react";
// Services
import { FriendshipService } from "@/services/api/friendshipService";
// Provider
import { usePresenceContext } from "@/components/_Presence/PresenceProvider";
// Components
import Trigger from "./components/Trigger/Trigger";
import FriendsSection from "@/components/FriendsOverlay/components/FriendsSection/FriendsSection";
import ChatSection from "@/components/FriendsOverlay/components/ChatSection/ChatSection";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./FriendsOverlay.module.css";



// --------------- //
// --- Context --- //
// --------------- //
type FriendsOverlayContextType = {
    openChat: (user: SelectedChatUserVM) => void;
    friendshipVersion: number;
    notifyFriendshipMutated: () => void;
};

const FriendsOverlayContext = createContext<FriendsOverlayContextType | null>(null);

export function useFriendsOverlay() {
    const ctx = useContext(FriendsOverlayContext);
    if (!ctx) throw new Error("useFriendsOverlay must be used within FriendsOverlay");
    return ctx;
}



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    trigger:            styles["trigger"],

    container:          styles["container"],
    containerExpanded:  styles["containerExpanded"],

    dropClosed:         styles["dropClosed"],
    dropOpened:         styles["dropOpened"],

    chatClosed:         styles["chatClosed"],
    chatOpened:         styles["chatOpened"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FriendsOverlay({ children }: { children?: React.ReactNode }) {


    const [notificationCount, setNotificationCount] = useState(0);
    const [selectedChatUser, setSelectedChatUser] = useState<SelectedChatUserVM | null>(null);

    const [isExpanded, setIsExpanded] = useState(false); // Trigger width
    const [isFriendPanelOpen, setIsFriendPanelOpen] = useState(false);
    const [isChatPanelOpen, setIsChatPanelOpen] = useState(false);

    // Bumped on every live "FriendshipChanged" event, to trigger a refetch in the panel.
    const [friendshipVersion, setFriendshipVersion] = useState(0);

    const { subscribeFriendshipChanged } = usePresenceContext();



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    const toggleFriends = () => {
        if (!isExpanded) {
            setIsExpanded(true);

            window.setTimeout(() => {
                setIsFriendPanelOpen(true);
            }, 250);

            return;
        }

        setIsFriendPanelOpen(false);

        window.setTimeout(() => {
            setIsExpanded(false);
        }, 250);
    };



    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleOpenChat(selectedUser: SelectedChatUserVM) {
        setSelectedChatUser(selectedUser);

        if (!isExpanded) {
            setIsExpanded(true);
            window.setTimeout(() => {
                setIsFriendPanelOpen(true);
                setIsChatPanelOpen(true);
            }, 250);
        } else {
            setIsFriendPanelOpen(true);
            setIsChatPanelOpen(true);
        }
    }

    function handleCloseChat() {
        setIsChatPanelOpen(false);
        setSelectedChatUser(null);
    }



    // ------------- //
    // --- Logic --- //
    // ------------- //
    const loadIncomingFriendRequestsCount = useCallback(async () => {
        try {
            const data = await FriendshipService.getIncomingFriendRequests();
            setNotificationCount(data.length);

        } catch (error) {
            console.error("Failed to load incoming friend requests count", error);
            setNotificationCount(0);
        }
    }, []);

    // Called by a panel after a local friendship mutation (accept/reject), since the server only
    // pushes "FriendshipChanged" to the counterparty - not back to the acting user. This keeps the
    // bell count in sync without a reload.
    const notifyFriendshipMutated = useCallback(() => {
        void loadIncomingFriendRequestsCount();
    }, [loadIncomingFriendRequestsCount]);



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        void loadIncomingFriendRequestsCount();
    }, [loadIncomingFriendRequestsCount]);

    // Live-sync: refresh the bell count and signal the panel to refetch on any friendship change.
    useEffect(() => {
        const unsubscribe = subscribeFriendshipChanged(() => {
            void loadIncomingFriendRequestsCount();
            setFriendshipVersion(prev => prev + 1);
        });

        return unsubscribe;
    }, [subscribeFriendshipChanged, loadIncomingFriendRequestsCount]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <FriendsOverlayContext.Provider value={{ openChat: handleOpenChat, friendshipVersion, notifyFriendshipMutated }}>
            <div className={`${c.container} ${isExpanded ? c.containerExpanded : ""}`}>
                <div className={c.trigger}>
                    <Trigger onToggle={toggleFriends} notificationCount={notificationCount} />
                </div>

                <div className={`${c.dropClosed} ${isFriendPanelOpen ? c.dropOpened : ""}`}>
                    <FriendsSection onOpenChat={handleOpenChat} friendshipVersion={friendshipVersion} onFriendshipMutated={notifyFriendshipMutated} />
                </div>

                <div className={`${c.chatClosed} ${isChatPanelOpen ? c.chatOpened : ""}`}>
                    <ChatSection selectedChatUser={selectedChatUser} onCloseChat={handleCloseChat} />
                </div>
            </div>

            {children}
        </FriendsOverlayContext.Provider>
    );
}