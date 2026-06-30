

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Avatar from "@/components/Avatar/Avatar";

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";

// Utils
import { maskEmail } from "@/services/utils/format";

// Icons
import ChatIcon from "@/app/src/svg/chat_round_write_3.svg";

import RemoveFriendIcon from "@/app/src/svg/user_remove_3.svg";
import AddFriendIcon from "@/app/src/svg/user_add_2.svg";
import AcceptIcon from "@/app/src/svg/accept.svg";
import RejectIcon from "@/app/src/svg/reject.svg";

import FriendshipFriendIcon from "@/app/src/svg/group.svg";
import FriendshipBlockedIcon from "@/app/src/svg/user_blocked.svg";

import FriendshipIncomingIcon from "@/app/src/svg/bell.svg";
import FriendshipOutgoingIcon from "@/app/src/svg/send_2.svg";

// Styles
import styles from "./ProfileTag.module.css";
import { FriendshipStatus } from "@/models/Friendship";
import { PresenceStatus } from "@/models/User";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    avatarArea:         styles["avatarArea"],

    infoArea:           styles["infoArea"],
    infoLine:           styles["infoLine"],
    info:               styles["info"],
    displayName:        styles["displayName"],
    email:              styles["email"],

    badges:             styles["badges"],
    friendshipStatus:   styles["friendshipStatus"],
    icon:               styles["icon"],

    actionArea:         styles["actionArea"],
    button:             styles["button"],
    accept:             styles["accept"],
    reject:             styles["reject"],
} as const;

type ProfileTagProps = {
    avatarUrl?: string | null;
    displayName: string;
    email: string;

    friendshipStatus: FriendshipStatus;
    presenceStatus: PresenceStatus;

    isUserList?: boolean;

    onSendFriendRequest?: () => void;
    onAcceptFriendRequest?: () => void;
    onRejectFriendRequest?: () => void;
    onRemoveFriend?: () => void;
    onOpenChat?: () => void;
}




// ----------------- //
// --- Component --- //
// ----------------- //
export default function ProfileTag({
    avatarUrl,
    displayName,
    email,

    friendshipStatus,
    presenceStatus,

    isUserList,

    onSendFriendRequest,
    onAcceptFriendRequest,
    onRejectFriendRequest,
    onRemoveFriend,
    onOpenChat,
}:ProfileTagProps) {


    const { showTooltip, hideTooltip } = useTooltip();


    // ------------------ //
    // --- UI-Helper --- //
    // ------------------ //
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };

    // ### Buttons ### //
    const btnOpenChat = (
        <button
            className={c.button}

            onMouseEnter={(e) => handleTooltipMove(e, "Chat öffnen")}
            onMouseMove={(e) => handleTooltipMove(e, "Chat öffnen")}
            onMouseLeave={hideTooltip}

            onClick={onOpenChat}
        >
            <ChatIcon />
        </button>
    );

    const btnAddFriend = (
        <button
            className={c.button}
            onClick={onSendFriendRequest}

            onMouseEnter={(e) => handleTooltipMove(e, "Freund hinzufügen")}
            onMouseMove={(e) => handleTooltipMove(e, "Freund hinzufügen")}
            onMouseLeave={hideTooltip}
        >
            <AddFriendIcon />
        </button>
    );

    const btnRemoveFriend = (
        <button
            className={c.button}
            onClick={onRemoveFriend}

            onMouseEnter={(e) => handleTooltipMove(e, "Freund entfernen")}
            onMouseMove={(e) => handleTooltipMove(e, "Freund entfernen")}
            onMouseLeave={hideTooltip}
        >
            <RemoveFriendIcon />
        </button>
    );

    const btnAccept = (
        <button
            className={c.button + " " + c.accept}
            onClick={onAcceptFriendRequest}

            onMouseEnter={(e) => handleTooltipMove(e, "Anfrage akzeptieren")}
            onMouseMove={(e) => handleTooltipMove(e, "Anfrage akzeptieren")}
            onMouseLeave={hideTooltip}
        >
            <AcceptIcon />
        </button>
    );

    const btnReject = (
        <button
            className={c.button + " " + c.reject}
            onClick={onRejectFriendRequest}

            onMouseEnter={(e) => handleTooltipMove(e, "Anfrage ablehnen")}
            onMouseMove={(e) => handleTooltipMove(e, "Anfrage ablehnen")}
            onMouseLeave={hideTooltip}
        >
            <RejectIcon />
        </button>
    );


    // ### Render ### //

    // Render actions -> depending on friendshipStatus
    // Not friended == AddFriendButton | IncomingFriendRequest == Accept and DenyButtons
    const renderActionArea = () => {
        switch (friendshipStatus) {

            case FriendshipStatus.Friend:
                return (
                    <>
                        {!isUserList && btnRemoveFriend}
                        {btnOpenChat}
                    </>
                );

            case FriendshipStatus.PendingIncoming:
                return (
                    <>
                        {btnAccept}
                        {btnReject}
                        {btnOpenChat}
                    </>
                );

            case FriendshipStatus.PendingOutgoing:
            case FriendshipStatus.Blocked:
                return (
                    <>
                        {btnOpenChat}
                    </>
                );

            case FriendshipStatus.None:
            default:
                return (
                    <>
                        {btnAddFriend}
                        {btnOpenChat}
                    </>
                );
        }
    };

    // Render friendship status
    const renderFriendshipStatus = () => {
        switch (friendshipStatus) {
            case FriendshipStatus.Friend:
                return <FriendshipFriendIcon />;

            case FriendshipStatus.PendingIncoming:
                return <FriendshipIncomingIcon />;

            case FriendshipStatus.PendingOutgoing:
                return <FriendshipOutgoingIcon />;

            case FriendshipStatus.Blocked:
                return <FriendshipBlockedIcon />;

            case FriendshipStatus.None:
            default:
                return <></>;
        }
    };




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* AVATAR-AREA */}
            <div className={c.avatarArea}>
                <Avatar
                    avatarUrl={avatarUrl}
                    displayName={displayName}
                    presenceStatus={presenceStatus}
                />
            </div>


            {/* INFO-AREA */}
            <div className={c.infoArea}>

                {/* InfoLine */}
                <div className={c.infoLine}>

                    {/* DisplayName */}
                    <div className={c.displayName}>{displayName}</div>

                    {/* Badges */}
                    <div className={c.badges}>

                        {/* Badge: FriendshipStatus */}
                        <div className={c.friendshipStatus}>
                            <div className={c.icon}>
                                {renderFriendshipStatus()}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Email (masked for privacy) */}
                <div className={c.email}>{maskEmail(email)}</div>
            </div>



            {/* ACTION-AREA */}
            <div className={c.actionArea}>
                {renderActionArea()}
            </div>
        </div>
    );
}