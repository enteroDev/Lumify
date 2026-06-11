"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Services
import { FriendshipService } from "@/services/api/friendshipService";
import { UserService } from "@/services/api/userService";
// Provider
import { usePresenceContext } from "@/components/_Presence/PresenceProvider";
// Components
import TabLine from "./components/TabLine/TabLine";
import Content from "./components/Content/Content";
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
    const [searchedUsers, setSearchedUsers] = useState<UserPreviewDTO[]>([]);

    const [outgoingFriendRequests, setOutgoingFriendRequests] = useState<FriendshipDTO[]>([]);
    const [incomingFriendRequests, setIncomingFriendRequests] = useState<FriendshipDTO[]>([]);

    const [friendSearchValue, setFriendSearchValue] = useState("");
    const [userSearchValue, setUserSearchValue] = useState("");
    const [debouncedUserSearchValue, setDebouncedUserSearchValue] = useState("");

    const { getPresenceStatus } = usePresenceContext();



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


    // ### SEARCH ### //
    const searchUsers = async () => {
        try {
            const data = await UserService.searchUsers(debouncedUserSearchValue);
            setSearchedUsers(data);

        } catch (error) {
            console.error("Failed to search users", error);
            setSearchedUsers([]);
        }
    };



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        void loadFriends();
        void loadRelatedUsers();
        void loadOutgoingFriendRequests();
        void loadIncomingFriendRequests();
    }, []);

    useEffect(() => {
        const timeout = window.setTimeout(() => {
            setDebouncedUserSearchValue(userSearchValue.trim().toLowerCase());
        }, 300);

        return () => window.clearTimeout(timeout);
    }, [userSearchValue]);

    useEffect(() => {
        if (!debouncedUserSearchValue) {
            void loadRelatedUsers();
            return;
        }

        void searchUsers();
    }, [debouncedUserSearchValue]);



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const friendIDs = new Set(friends.map(friend => friend.id));
    const debouncedFriendSearchValue = friendSearchValue.trim().toLowerCase();

    const filteredFriends = friends.filter(friend => {
        const effectiveName = (friend.displayName || friend.username || "").toLowerCase();

        if (!debouncedFriendSearchValue) {
            return true;
        }

        return effectiveName.includes(debouncedFriendSearchValue);
    });

    const friendsWithPresenceInfo = filteredFriends.map(friend => ({
        ...friend,
        presenceStatus: getPresenceStatus(friend.id, friend.presenceStatus),
    }));

    const relatedUsersWithPresenceInfo = relatedUsers.map(user => ({
        ...user,
        presenceStatus: getPresenceStatus(user.id, user.presenceStatus),
    }));

    const searchedUsersWithPresenceInfo = searchedUsers.map(user => ({
        ...user,
        presenceStatus: getPresenceStatus(user.id, user.presenceStatus),
    }));

    const filteredUsers = debouncedUserSearchValue
        ? searchedUsersWithPresenceInfo
        : relatedUsersWithPresenceInfo;



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* TabLine */}
            <div className={c.tabLine}>
                <TabLine
                    activeTab={activeTab}
                    onTabChange={setActiveTab}
                    incomingRequestCount={incomingFriendRequests.length}
                />
            </div>

            {/* Content */}
            <div className={c.content}>
                <Content
                    activeTab={activeTab}

                    friends={friendsWithPresenceInfo}
                    users={filteredUsers}
                    requestUsers={relatedUsersWithPresenceInfo}
                    friendIDs={friendIDs}
                    outgoingFriendRequests={outgoingFriendRequests}
                    incomingFriendRequests={incomingFriendRequests}

                    friendSearchValue={friendSearchValue}
                    onFriendSearchChange={setFriendSearchValue}
                    userSearchValue={userSearchValue}
                    onUserSearchChange={setUserSearchValue}

                    onSendFriendRequest={sendFriendRequest}
                    onAcceptFriendRequest={acceptFriendRequest}
                    onRejectFriendRequest={rejectFriendRequest}
                    onRemoveFriend={removeFriend}

                    onOpenChat={onOpenChat}
                />
            </div>
        </div>
    );
}