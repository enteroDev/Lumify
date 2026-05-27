"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Friendlist from "./components/Friendlist/Friendlist";
import Userlist from "./components/Userlist/Userlist";
import FriendRequests from "./components/FriendRequests/FriendRequests";
// Types
import type { FriendsPanelTab } from "../../FriendsPanel";
// Models
import type { UserPreviewDTO } from "@/models/User";
import type { FriendshipDTO } from "@/models/Friendship";
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./Content.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
} as const;

type ContentProps = {
    activeTab: FriendsPanelTab;

    friends: UserPreviewDTO[];
    users: UserPreviewDTO[];
    requestUsers: UserPreviewDTO[];

    friendIDs: Set<string>;
    outgoingFriendRequests: FriendshipDTO[];
    incomingFriendRequests: FriendshipDTO[];

    friendSearchValue: string;
    userSearchValue: string;

    onFriendSearchChange: (value: string) => void;
    onUserSearchChange: (value: string) => void;

    onSendFriendRequest: (addresseeID: string) => void;
    onAcceptFriendRequest: (friendshipID: string) => void;
    onRejectFriendRequest: (friendshipID: string) => void;
    onRemoveFriend: (friendID: string) => void;

    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Content({
    activeTab,

    friends,
    users,
    requestUsers,

    friendIDs,
    outgoingFriendRequests,
    incomingFriendRequests,

    friendSearchValue,
    userSearchValue,

    onFriendSearchChange,
    onUserSearchChange,

    onSendFriendRequest,
    onAcceptFriendRequest,
    onRejectFriendRequest,
    onRemoveFriend,

    onOpenChat,
}: ContentProps) {



    // ------------------ //
    // --- GUI-Helper --- //
    // ------------------ //
    const renderContent = () => {
        if (activeTab === "friendList") {
            return (
                <Friendlist
                    friends={friends}

                    searchValue={friendSearchValue}
                    onSearchChange={onFriendSearchChange}

                    onRemoveFriend={onRemoveFriend}
                    onOpenChat={onOpenChat}
                />
            );
        }

        if (activeTab === "requests") {
            return (
                <FriendRequests
                    users={requestUsers}
                    outgoingFriendRequests={outgoingFriendRequests}
                    incomingFriendRequests={incomingFriendRequests}

                    onAcceptFriendRequest={onAcceptFriendRequest}
                    onRejectFriendRequest={onRejectFriendRequest}
                    onOpenChat={onOpenChat}
                />
            );
        }

        return (
            <Userlist
                users={users}

                friendIDs={friendIDs}
                incomingRequestUserIDs={incomingRequestUserIDs}
                outgoingRequestUserIDs={outgoingRequestUserIDs}

                searchValue={userSearchValue}
                onSearchChange={onUserSearchChange}

                onSendFriendRequest={onSendFriendRequest}
                onOpenChat={onOpenChat}
            />
        );
    };



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const incomingRequestUserIDs = new Set(
        incomingFriendRequests
            .map(x => x.requesterID)
            .filter((id): id is string => Boolean(id))
    );

    const outgoingRequestUserIDs = new Set(
        outgoingFriendRequests
            .map(x => x.friendUserID)
            .filter((id): id is string => Boolean(id))
    );



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {renderContent()}
        </div>
    );
}