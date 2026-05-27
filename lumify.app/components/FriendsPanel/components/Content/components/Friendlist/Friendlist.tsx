

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import ProfileTag from "../../../../../ProfileTag/ProfileTag";
// Models
import type { UserPreviewDTO } from "@/models/User";
import { FriendshipStatus } from "@/models/Friendship";
import type { SelectedChatUserVM } from "@/models/Chat";
// Icons
import SearchIcon from "@/app/src/svg/search.svg";
// Styles
import styles from "./Friendlist.module.css";



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

export type FriendlistProps = {
    friends: UserPreviewDTO[];
    searchValue: string;

    onSearchChange: (value: string) => void;
    onRemoveFriend: (friendID: string) => void;
    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function Friendlist({
    friends,
    searchValue,

    onSearchChange,
    onRemoveFriend,
    onOpenChat,
}: FriendlistProps) {




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
                        placeholder="Freunde suchen..."

                        value={searchValue}
                        onChange={(e) => onSearchChange(e.target.value)}
                    />
                </div>
            </div>


            {/* LIST */}
            <div className={c.list}>

                {/* Friends */}
                {friends.map(friend => (
                    <ProfileTag
                        key={friend.id}
                        avatarUrl={friend.avatarUrl}
                        displayName={friend.displayName || friend.username || "Unknown"}
                        email={friend.email}
                        friendshipStatus={FriendshipStatus.Friend}
                        presenceStatus={friend.presenceStatus}

                        onRemoveFriend={() => onRemoveFriend(friend.id)}
                        onOpenChat={() => onOpenChat({
                            userID: friend.id,
                            displayName: friend.displayName || friend.username || "Unknown",
                            email: friend.email,
                            avatarUrl: friend.avatarUrl,
                            presenceStatus: friend.presenceStatus,
                        })}
                    />
                ))}
            </div>
        </div>
    );
}