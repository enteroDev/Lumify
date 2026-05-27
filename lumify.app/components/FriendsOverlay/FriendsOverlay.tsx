"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Services
import { FriendshipService } from "@/services/api/friendshipService";
// Components
import Trigger from "./components/Trigger/Trigger";
import FriendsSection from "@/components/FriendsOverlay/components/FriendsSection/FriendsSection";
import ChatSection from "@/components/FriendsOverlay/components/ChatSection/ChatSection";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./FriendsOverlay.module.css";



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
export default function FriendsOverlay() {


    const [notificationCount, setNotificationCount] = useState(0);
    const [selectedChatUser, setSelectedChatUser] = useState<SelectedChatUserVM | null>(null);

    const [isExpanded, setIsExpanded] = useState(false); // Trigger width
    const [isFriendPanelOpen, setIsFriendPanelOpen] = useState(false);
    const [isChatPanelOpen, setIsChatPanelOpen] = useState(false);



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
        setIsChatPanelOpen(true);
    }

    function handleCloseChat() {
        setIsChatPanelOpen(false);
        setSelectedChatUser(null);
    }



    // ------------- //
    // --- Logic --- //
    // ------------- //
    const loadIncomingFriendRequestsCount = async () => {
        try {
            const data = await FriendshipService.getIncomingFriendRequests();
            setNotificationCount(data.length);

        } catch (error) {
            console.error("Failed to load incoming friend requests count", error);
            setNotificationCount(0);
        }
    };



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        void loadIncomingFriendRequestsCount();
    }, []);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={`${c.container} ${isExpanded ? c.containerExpanded : ""}`}>
            <div className={c.trigger}>
                <Trigger onToggle={toggleFriends} notificationCount={notificationCount} />
            </div>

            <div className={`${c.dropClosed} ${isFriendPanelOpen ? c.dropOpened : ""}`}>
                <FriendsSection onOpenChat={handleOpenChat} />
            </div>

            <div className={`${c.chatClosed} ${isChatPanelOpen ? c.chatOpened : ""}`}>
                <ChatSection selectedChatUser={selectedChatUser} onCloseChat={handleCloseChat} />
            </div>
        </div>
    );
}