"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Components
import AvatarBubble from "@/components/Avatar/Avatar";
// Models
import type { PresenceStatus } from "@/models/User";
// Styles
import styles from "./UserListItem.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],

    avatarArea:         styles["avatarArea"],

    infoArea:           styles["infoArea"],
    name:               styles["name"],
    info:               styles["info"],

    actionArea:         styles["actionArea"],
    button:             styles["button"],
} as const;

export type UserListItemProps = {
    userID: string;
    avatarUrl?: string | null;
    displayName: string;
    username: string;
    email: string;
    presenceStatus?: PresenceStatus;
    onAddMember: (userID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function UserListItem({
    userID,
    avatarUrl,
    displayName,
    username,
    email,
    presenceStatus,
    onAddMember,
}: UserListItemProps) {


    const { showTooltip, hideTooltip } = useTooltip();


    // ------------------ //
    // --- UI-Helpers --- //
    // ------------------ //
    const renderName = () => {
        if (!displayName) {
            return username;
        }

        return displayName;
    };


    // --------------- //
    // --- Handler --- //
    // --------------- //
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };

    const handleAddMember = () => {
        onAddMember(userID);
    };


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.avatarArea}>
                <AvatarBubble
                    avatarUrl={avatarUrl}
                    displayName={displayName}
                    presenceStatus={presenceStatus}
                />
            </div>

            <div className={c.infoArea}>
                <div className={c.name}>{renderName()}</div>
                <div className={c.info}>{email}</div>
            </div>

            <div className={c.actionArea}>
                <button
                    className={c.button}

                    onMouseEnter={(e) => handleTooltipMove(e, "Member hinzufügen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Member hinzufügen")}
                    onMouseLeave={hideTooltip}

                    onClick={handleAddMember}
                >
                    +
                </button>
            </div>
        </div>
    );
}