"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Services
import { FriendshipService } from "@/services/api/friendshipService";
import { UserService } from "@/services/api/userService";

// Components
import TabLine from "./components/TabLine/TabLine";
// Models
import type { UserPreviewDTO } from "@/models/User";
import type { FriendshipDTO } from "@/models/Friendship";
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./FriendsPanel.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    tabLine:        styles["tabLine"],
    content:        styles["content"],
} as const;

type FriendsPanelProps = {
    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};

export type FriendsPanelTab = "friendList" | "userList" | "requests";



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FriendsPanel({
    onOpenChat,
}: FriendsPanelProps) {


    const [activeTab, setActiveTab] = useState<FriendsPanelTab>("friendList");

    const [friends, setFriends] = useState<UserPreviewDTO[]>([]);
    const [relatedUsers, setRelatedUsers] = useState<UserPreviewDTO[]>([]);

    const [outgoingFriendRequests, setOutgoingFriendRequests] = useState<FriendshipDTO[]>([]);
    const [incomingFriendRequests, setIncomingFriendRequests] = useState<FriendshipDTO[]>([]);



    // ------------- //
    // --- Logic --- //
    // ------------- //

    // ### LOAD ### //
    const loadFriends = async () => {
        try {
            const data = await FriendshipService.getFriendsOfUser();
            setFriends(data);

        } catch (error) {
            console.error("Failed to load friends", error);
            setFriends([]);
        }
    };

    const loadRelatedUsers = async () => {
        try {
            const data = await UserService.getRelatedUsers();
            setRelatedUsers(data);

        } catch (error) {
            console.error("Failed to load related users", error);
            setRelatedUsers([]);
        }
    };

    const loadOutgoingFriendRequests = async () => {
        try {
            const data = await FriendshipService.getOutgoingFriendRequests();
            setOutgoingFriendRequests(data);

        } catch (error) {
            console.error("Failed to load outgoing friend requests", error);
            setOutgoingFriendRequests([]);
        }
    };

    const loadIncomingFriendRequests = async () => {
        try {
            const data = await FriendshipService.getIncomingFriendRequests();
            setIncomingFriendRequests(data);

        } catch (error) {
            console.error("Failed to load incoming friend requests", error);
            setIncomingFriendRequests([]);
        }
    };


    // ### FRIEND-INTERACTIONS ### //
    const sendFriendRequest = async (addresseeID: string) => {
        try {
            await FriendshipService.sendFriendRequest(addresseeID);

            await loadOutgoingFriendRequests();
            await loadRelatedUsers();

        } catch (error) {
            console.error("Failed to send friend request", error);
        }
    };

    const acceptFriendRequest = async (friendshipID: string) => {
        try {
            await FriendshipService.acceptFriendRequest(friendshipID);

            await loadFriends();
            await loadIncomingFriendRequests();
            await loadOutgoingFriendRequests();
            await loadRelatedUsers();

        } catch (error) {
            console.error("Failed to accept friend request", error);
        }
    };

    const rejectFriendRequest = async (friendshipID: string) => {
        try {
            await FriendshipService.rejectFriendRequest(friendshipID);

            await loadIncomingFriendRequests();
            await loadOutgoingFriendRequests();
            await loadRelatedUsers();

        } catch (error) {
            console.error("Failed to reject friend request", error);
        }
    };

    const removeFriend = async (friendID: string) => {
        try {
            await FriendshipService.removeFriend(friendID);

            await loadFriends();
            await loadRelatedUsers();

        } catch (error) {
            console.error("Failed to remove friend", error);
        }
    };





    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* TabLine */}
            <div className={c.tabLine}>
                <TabLine activeTab={activeTab} onTabChange={setActiveTab} />
            </div>

            {/* Content */}
            <div className={c.content}>

            </div>
        </div>
    );
}