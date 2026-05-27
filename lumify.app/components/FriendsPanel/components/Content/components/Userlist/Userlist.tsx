

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import ProfileTag from "../../../../../ProfileTag/ProfileTag";
// Models
import type { UserPreviewDTO } from "@/models/User";
import { FriendshipStatus } from "@/models/Friendship";
import { SelectedChatUserVM } from "@/models/Chat";
// Icons
import SearchIcon from "@/app/src/svg/search.svg";
// Styles
import styles from "./Userlist.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    actionBar:          styles["actionBar"],
    searchBar:          styles["searchBar"],
    searchIcon:         styles["searchIcon"],
    input:              styles["input"],

    list:               styles["list"],
} as const;

export type UserlistProps = {
    users: UserPreviewDTO[];
    friendIDs: Set<string>;

    incomingRequestUserIDs: Set<string>;
    outgoingRequestUserIDs: Set<string>;

    searchValue: string;
    onSearchChange: (value: string) => void;

    onSendFriendRequest: (addresseeID: string) => void;
    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Userlist({
    users,
    friendIDs,

    incomingRequestUserIDs,
    outgoingRequestUserIDs,

    searchValue,
    onSearchChange,

    onSendFriendRequest,
    onOpenChat,
}:UserlistProps) {




    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const getFriendshipStatus = (userID: string): FriendshipStatus => {
        if (friendIDs.has(userID)) {
            return FriendshipStatus.Friend;
        }

        if (incomingRequestUserIDs.has(userID)) {
            return FriendshipStatus.PendingIncoming;
        }

        if (outgoingRequestUserIDs.has(userID)) {
            return FriendshipStatus.PendingOutgoing;
        }

        return FriendshipStatus.None;
    };



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* ACTION-BAR */}
            <div className={c.actionBar}>

                {/* SearchBar */}
                <div className={c.searchBar}>
                    <div className={c.searchIcon}><SearchIcon /></div>
                    <input
                        className={c.input}
                        type="text"
                        placeholder="User suchen..."

                        value={searchValue}
                        onChange={(e) => onSearchChange(e.target.value)}
                    />
                </div>
            </div>

            {/* LIST */}
            <div className={c.list}>

                {/* Users */}
                {users.map(user => (
                    <ProfileTag
                        key={user.id}
                        avatarUrl={user.avatarUrl}
                        displayName={user.displayName || user.username || "Unknown"}
                        email={user.email}

                        friendshipStatus={getFriendshipStatus(user.id)}
                        presenceStatus={user.presenceStatus}

                        isUserList={true}

                        onSendFriendRequest={() => onSendFriendRequest(user.id)}
                        onOpenChat={() => onOpenChat({
                            userID: user.id,
                            displayName: user.displayName || user.username || "Unknown",
                            email: user.email,
                            avatarUrl: user.avatarUrl,
                            presenceStatus: user.presenceStatus,
                        })}
                    />
                ))}

            </div>
        </div>
    );
}