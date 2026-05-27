// --------------- //
// --- Imports --- //
// --------------- //

// Components
import ProfileTag from "../../../../../ProfileTag/ProfileTag";
// Icons
import FriendshipIncomingIcon from "@/app/src/svg/bell.svg";
import FriendshipOutgoingIcon from "@/app/src/svg/send_2.svg";
// Models
import { PresenceStatus, type UserPreviewDTO } from "@/models/User";
import { FriendshipDTO, FriendshipStatus } from "@/models/Friendship";
import { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./FriendRequest.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    section:            styles["section"],
    sectionHeader:      styles["sectionHeader"],
    sectionTitle:       styles["sectionTitle"],
    sectionIcon:        styles["sectionIcon"],

    list:               styles["list"],
} as const;

type FriendRequestProps = {
    incomingFriendRequests: FriendshipDTO[];
    outgoingFriendRequests: FriendshipDTO[];

    users: UserPreviewDTO[];

    onAcceptFriendRequest: (friendshipID: string) => void;
    onRejectFriendRequest: (friendshipID: string) => void;
    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};




// ----------------- //
// --- Component --- //
// ----------------- //
export default function FriendRequest({
    incomingFriendRequests,
    outgoingFriendRequests,
    users,
    onAcceptFriendRequest,
    onRejectFriendRequest,
    onOpenChat,
}: FriendRequestProps) {



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const incomingItems = incomingFriendRequests.map(req => {
        const requester = users.find(user => user.id === req.requesterID);

        return {
            friendshipID: req.id,
            userID: req.requesterID,
            avatarUrl: req.requesterAvatarUrl,
            displayName: req.requesterDisplayName || req.requesterUsername || "Unknown",
            email: "",
            friendshipStatus: FriendshipStatus.PendingIncoming,
            presenceStatus: requester?.presenceStatus ?? PresenceStatus.Offline,
        };
    });

    const outgoingItems = outgoingFriendRequests.map(req => {
        const addressee = users.find(user => user.id === req.addresseeID);

        return {
            friendshipID: req.id,
            userID: req.addresseeID,
            avatarUrl: req.friendAvatarUrl,
            displayName: req.friendDisplayName || req.friendUsername || "Unknown",
            email: "",
            friendshipStatus: FriendshipStatus.PendingOutgoing,
            presenceStatus: addressee?.presenceStatus ?? PresenceStatus.Offline,
        };
    });



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Section: Incoming friend-requests */}
            <div className={c.section}>

                {/* Title */}
                <div className={c.sectionHeader}>
                    <div className={c.sectionIcon}><FriendshipIncomingIcon /></div>
                    <div className={c.sectionTitle}>Eingehend</div>
                </div>

                {/* List */}
                <div className={c.list}>
                    {incomingItems.map(item => (
                        <ProfileTag
                            key={item.friendshipID}
                            avatarUrl={item.avatarUrl}
                            displayName={item.displayName}
                            email={item.email}

                            friendshipStatus={item.friendshipStatus}
                            presenceStatus={item.presenceStatus}

                            onAcceptFriendRequest={() => onAcceptFriendRequest(item.friendshipID)}
                            onRejectFriendRequest={() => onRejectFriendRequest(item.friendshipID)}
                            onOpenChat={() => onOpenChat({
                                userID: item.userID,
                                displayName: item.displayName,
                                email: item.email,
                                avatarUrl: item.avatarUrl,
                                presenceStatus: item.presenceStatus,
                            })}
                        />
                    ))}
                </div>
            </div>


            {/* Section: Outgoing friend-requests */}
            <div className={c.section}>

                {/* Title */}
                <div className={c.sectionHeader}>
                    <div className={c.sectionIcon}><FriendshipOutgoingIcon /></div>
                    <div className={c.sectionTitle}>Ausgehend</div>
                </div>

                {/* List */}
                <div className={c.list}>
                    {outgoingItems.map(item => (
                        <ProfileTag
                            key={item.friendshipID}
                            avatarUrl={item.avatarUrl}
                            displayName={item.displayName}
                            email={item.email}
                            friendshipStatus={item.friendshipStatus}
                            presenceStatus={item.presenceStatus}

                            onOpenChat={() => onOpenChat({
                                userID: item.userID,
                                displayName: item.displayName,
                                email: item.email,
                                avatarUrl: item.avatarUrl,
                                presenceStatus: item.presenceStatus,
                            })}
                        />
                    ))}
                </div>
            </div>

        </div>
    );
}